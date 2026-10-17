using System.ComponentModel;
using System.IO.Abstractions;

using wsl_squad.Core.Abstractions.Build;
using wsl_squad.Core.Abstractions.Process;
using wsl_squad.Core.Abstractions.Time;
using wsl_squad.Core.Abstractions.Windows;
using wsl_squad.Core.Application.Common;
using wsl_squad.Core.Application.Fleet;
using wsl_squad.Core.Domain.Build;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Fleet;
using wsl_squad.Core.Infrastructure.Docker;

namespace wsl_squad.Core.Application.Build;

/// <summary>Builds a Docker image, imports it as a WSL instance and records its metadata.</summary>
public sealed class BuildInstanceHandler
{
    private readonly IFileSystem _fileSystem;
    private readonly IWslGateway _wsl;
    private readonly IDockerGateway _docker;
    private readonly FleetMarkerStore _markers;
    private readonly InstanceMetadataStore _metadata;
    private readonly BuildPlanFactory _plans;
    private readonly IClock _clock;

    public BuildInstanceHandler(
        IFileSystem fileSystem,
        IWslGateway wsl,
        IDockerGateway docker,
        FleetMarkerStore markers,
        InstanceMetadataStore metadata,
        BuildPlanFactory plans,
        IClock clock)
    {
        _fileSystem = fileSystem;
        _wsl = wsl;
        _docker = docker;
        _markers = markers;
        _metadata = metadata;
        _plans = plans;
        _clock = clock;
    }

    public async Task<OperationResult<BuildSummary>> BuildAsync(
        string fleetRoot,
        string catalogRoot,
        BuildRequest request,
        CancellationToken cancellationToken = default)
    {
        OperationResult<BuildPlan> planResult = _plans.Create(fleetRoot, request);

        if (!planResult.IsSuccess)
        {
            return Failure(planResult.Error!);
        }

        BuildPlan plan = planResult.Value;

        if (!await _docker.IsAvailableAsync(cancellationToken).ConfigureAwait(false))
        {
            return OperationResult<BuildSummary>.Fail(
                ErrorCode.DockerUnavailable,
                "Docker is not available. Start Docker Desktop and retry.");
        }

        OperationTransaction transaction = new();
        List<string> warnings = [];
        bool onboarded = false;
        string? savedImagePath = null;
        string rootfsTar = _fileSystem.Path.Combine(plan.StagingDirectory, "rootfs.tar");

        try
        {
            _ = _fileSystem.Directory.CreateDirectory(plan.StagingDirectory);

            string imageReference = await PrepareImageAsync(plan, transaction, cancellationToken)
                .ConfigureAwait(false);

            await _docker.ExportRootfsAsync(imageReference, rootfsTar, cancellationToken)
                .ConfigureAwait(false);

            _ = _fileSystem.Directory.CreateDirectory(plan.InstallPath);
            transaction.OnRollback(() =>
            {
                DeleteDirectoryStrict(plan.InstallPath);
                return Task.CompletedTask;
            });

            try
            {
                await _wsl.ImportDistributionAsync(plan.Name, plan.InstallPath, rootfsTar, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception exception) when (IsExpectedBuildFailure(exception))
            {
                await TryUnregisterAsync(plan.Name).ConfigureAwait(false);
                IReadOnlyList<string> rollbackFailures = await transaction.RollbackAsync().ConfigureAwait(false);

                return FailureFromException(exception, rollbackFailures);
            }

            transaction.OnRollback(() => _wsl.UnregisterDistributionAsync(plan.Name, CancellationToken.None));

            string id = Guid.NewGuid().ToString("N");
            DateTimeOffset now = _clock.UtcNow;

            OperationResult markerResult = _markers.Write(plan.InstallPath, new FleetMarker
            {
                Id = id,
                Name = plan.Name,
                CreatedUtc = now,
                CreatedBy = "build",
            });

            if (!markerResult.IsSuccess)
            {
                return await FailAfterRollbackAsync(transaction, markerResult.Error!).ConfigureAwait(false);
            }

            OperationResult metadataResult = _metadata.Save(plan.InstallPath, new InstanceMetadata
            {
                Id = id,
                Name = plan.Name,
                CreatedUtc = now,
                DefaultUser = plan.User,
                DockerDesktop = InstanceMetadata.DockerUnknown,
                Source = new InstanceSource
                {
                    Kind = plan.SourceKind.ToString(),
                    Reference = SourceReference(plan),
                    ImageTag = plan.ImageTag,
                },
            });

            if (!metadataResult.IsSuccess)
            {
                return await FailAfterRollbackAsync(transaction, metadataResult.Error!).ConfigureAwait(false);
            }

            if (plan.OnboardingPath is not null)
            {
                try
                {
                    await _wsl.CopyFileIntoDistributionAsync(
                        plan.Name,
                        plan.OnboardingPath,
                        "/root/onboarding.sh",
                        cancellationToken).ConfigureAwait(false);

                    await _wsl.RunOnboardingAsync(
                        plan.Name,
                        "/root/onboarding.sh",
                        plan.User == InstanceMetadata.DefaultUserName ? null : plan.User,
                        cancellationToken).ConfigureAwait(false);

                    await _wsl.TerminateDistributionAsync(plan.Name, CancellationToken.None).ConfigureAwait(false);
                    onboarded = true;
                }
                catch (Exception exception) when (IsExpectedBuildFailure(exception))
                {
                    warnings.Add($"onboarding: {exception.Message}");
                }
            }

            if (plan.ImageTag is not null)
            {
                savedImagePath = await RetainImageAsync(plan, catalogRoot, request.KeepImage, warnings, cancellationToken)
                    .ConfigureAwait(false);
            }

            BuildSummary summary = new(plan.Name, plan.InstallPath, plan.ImageTag, savedImagePath, onboarded);

            return warnings.Count == 0
                ? OperationResult<BuildSummary>.Ok(summary)
                : OperationResult<BuildSummary>.Warn(summary, [.. warnings]);
        }
        catch (OperationCanceledException)
        {
            IReadOnlyList<string> rollbackFailures = await transaction.RollbackAsync().ConfigureAwait(false);

            return OperationResult<BuildSummary>.Fail(
                ErrorCode.Cancelled,
                "Build cancelled.",
                JoinRollbackFailures(rollbackFailures));
        }
        catch (Exception exception) when (IsExpectedBuildFailure(exception))
        {
            IReadOnlyList<string> rollbackFailures = await transaction.RollbackAsync().ConfigureAwait(false);

            return FailureFromException(exception, rollbackFailures);
        }
        finally
        {
            TryDeleteDirectory(plan.StagingDirectory);
        }
    }

    private async Task<string> PrepareImageAsync(
        BuildPlan plan,
        OperationTransaction transaction,
        CancellationToken cancellationToken)
    {
        switch (plan.SourceKind)
        {
            case BuildSourceKind.Dockerfile:
            case BuildSourceKind.Recipe:
                string imageTag = plan.ImageTag!;

                await _docker.BuildImageAsync(
                    imageTag,
                    plan.DockerfilePath!,
                    plan.ContextPath!,
                    cancellationToken).ConfigureAwait(false);

                transaction.OnRollback(() => _docker.RemoveImageAsync(imageTag, CancellationToken.None));

                return imageTag;

            case BuildSourceKind.Image:
                return await _docker.LoadImageAsync(plan.ImagePath!, cancellationToken).ConfigureAwait(false);

            case BuildSourceKind.Registry:
                return plan.RegistryReference!;

            default:
                throw new InvalidOperationException($"Unsupported build source '{plan.SourceKind}'.");
        }
    }

    private async Task<string?> RetainImageAsync(
        BuildPlan plan,
        string catalogRoot,
        bool keepImage,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        string? savedImagePath = null;

        if (keepImage)
        {
            string directory = _fileSystem.Path.Combine(catalogRoot, "images", DockerTag.Slugify(plan.Name));
            savedImagePath = _fileSystem.Path.Combine(directory, plan.BuildId + ".tar");

            try
            {
                _ = _fileSystem.Directory.CreateDirectory(directory);

                await _docker.SaveImageAsync(plan.ImageTag!, savedImagePath, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception exception) when (IsExpectedBuildFailure(exception))
            {
                warnings.Add($"image save: {exception.Message}");
                savedImagePath = null;
            }
        }

        try
        {
            await _docker.RemoveImageAsync(plan.ImageTag!, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception) when (IsExpectedBuildFailure(exception))
        {
            warnings.Add($"image cleanup: {exception.Message}");
        }

        return savedImagePath;
    }

    private static async Task<OperationResult<BuildSummary>> FailAfterRollbackAsync(
        OperationTransaction transaction,
        OperationError error)
    {
        IReadOnlyList<string> rollbackFailures = await transaction.RollbackAsync().ConfigureAwait(false);

        return OperationResult<BuildSummary>.Fail(
            error.Code,
            error.Message,
            CombineDetail(error.Detail, rollbackFailures),
            error.ExternalExitCode);
    }

    private void DeleteDirectoryStrict(string path)
    {
        if (_fileSystem.Directory.Exists(path))
        {
            _fileSystem.Directory.Delete(path, recursive: true);
        }
    }

    private void TryDeleteDirectory(string path)
    {
        try
        {
            if (_fileSystem.Directory.Exists(path))
            {
                _fileSystem.Directory.Delete(path, recursive: true);
            }
        }
        catch (IOException)
        {
            // Staging cleanup is best-effort.
        }
        catch (UnauthorizedAccessException)
        {
            // Staging cleanup is best-effort.
        }
    }

    private async Task TryUnregisterAsync(string name)
    {
        try
        {
            await _wsl.UnregisterDistributionAsync(name, CancellationToken.None).ConfigureAwait(false);
        }
        catch (ProcessExecutionException)
        {
            // The distribution may not have been registered at all.
        }
        catch (Win32Exception)
        {
            // WSL is unavailable; nothing to unregister.
        }
    }

    private static string SourceReference(BuildPlan plan)
    {
        return plan.SourceKind switch
        {
            BuildSourceKind.Dockerfile => plan.DockerfilePath ?? string.Empty,
            BuildSourceKind.Recipe => plan.ContextPath ?? string.Empty,
            BuildSourceKind.Image => plan.ImagePath ?? string.Empty,
            BuildSourceKind.Registry => plan.RegistryReference ?? string.Empty,
            _ => string.Empty,
        };
    }

    private static bool IsExpectedBuildFailure(Exception exception)
    {
        return exception is ProcessExecutionException
            or IOException
            or UnauthorizedAccessException
            or Win32Exception
            or InvalidDataException;
    }

    private static OperationResult<BuildSummary> Failure(OperationError error)
    {
        return OperationResult<BuildSummary>.Fail(
            error.Code,
            error.Message,
            error.Detail,
            error.ExternalExitCode);
    }

    private static OperationResult<BuildSummary> FailureFromException(
        Exception exception,
        IReadOnlyList<string> rollbackFailures)
    {
        ErrorCode code;
        string message;
        string? detail;
        int? exitCode;

        if (exception is ProcessExecutionException process)
        {
            code = ErrorCode.ExternalTool;
            message = "An external tool failed.";
            detail = string.IsNullOrWhiteSpace(process.Result.StandardError)
                ? process.Message
                : process.Result.StandardError;
            exitCode = process.Result.ExitCode;
        }
        else if (exception is Win32Exception)
        {
            code = ErrorCode.ExternalTool;
            message = "An external tool could not be started.";
            detail = exception.Message;
            exitCode = null;
        }
        else if (exception is InvalidDataException)
        {
            code = ErrorCode.CorruptState;
            message = "The Docker image archive could not be read.";
            detail = exception.Message;
            exitCode = null;
        }
        else if (exception is IOException)
        {
            code = ErrorCode.IoError;
            message = "A file operation failed.";
            detail = exception.Message;
            exitCode = null;
        }
        else if (exception is UnauthorizedAccessException)
        {
            code = ErrorCode.PermissionDenied;
            message = "A file operation was denied.";
            detail = exception.Message;
            exitCode = null;
        }
        else
        {
            code = ErrorCode.Internal;
            message = "The build failed.";
            detail = exception.Message;
            exitCode = null;
        }

        return OperationResult<BuildSummary>.Fail(
            code,
            message,
            CombineDetail(detail, rollbackFailures),
            exitCode);
    }

    private static string? CombineDetail(string? detail, IReadOnlyList<string> rollbackFailures)
    {
        string? rollback = JoinRollbackFailures(rollbackFailures);

        if (string.IsNullOrWhiteSpace(detail))
        {
            return rollback;
        }

        return string.IsNullOrWhiteSpace(rollback) ? detail : $"{detail}{Environment.NewLine}{rollback}";
    }

    private static string? JoinRollbackFailures(IReadOnlyList<string> rollbackFailures)
    {
        return rollbackFailures.Count == 0
            ? null
            : $"Rollback issues: {string.Join(" | ", rollbackFailures)}";
    }
}

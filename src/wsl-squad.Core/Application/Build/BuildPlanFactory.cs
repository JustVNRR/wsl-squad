using System.IO.Abstractions;
using System.Text.RegularExpressions;

using wsl_squad.Core.Abstractions.Build;
using wsl_squad.Core.Abstractions.Windows;
using wsl_squad.Core.Domain.Build;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Fleet;
using wsl_squad.Core.Infrastructure.Docker;
using wsl_squad.Core.Infrastructure.Storage;

namespace wsl_squad.Core.Application.Build;

/// <summary>Validates a build request and turns it into a normalized plan.</summary>
public sealed partial class BuildPlanFactory
{
    private readonly IFileSystem _fileSystem;
    private readonly IWslRegistryGateway _registry;
    private readonly IBuildIdGenerator _buildIds;

    public BuildPlanFactory(IFileSystem fileSystem, IWslRegistryGateway registry, IBuildIdGenerator buildIds)
    {
        _fileSystem = fileSystem;
        _registry = registry;
        _buildIds = buildIds;
    }

    public OperationResult<BuildPlan> Create(string fleetRoot, BuildRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!PathHelper.IsFullyQualified(fleetRoot))
        {
            return OperationResult<BuildPlan>.Fail(
                ErrorCode.Validation,
                $"Fleet root '{fleetRoot}' is not an absolute path.");
        }

        if (!InstanceName.IsValid(request.Name))
        {
            return OperationResult<BuildPlan>.Fail(
                ErrorCode.InvalidName,
                $"Instance name '{request.Name}' is invalid.");
        }

        string user = string.IsNullOrWhiteSpace(request.User)
            ? InstanceMetadata.DefaultUserName
            : request.User;

        if (!UserPattern().IsMatch(user))
        {
            return OperationResult<BuildPlan>.Fail(
                ErrorCode.Validation,
                $"User name '{user}' is invalid.");
        }

        if (IsRegisteredAlready(request.Name))
        {
            return OperationResult<BuildPlan>.Fail(
                ErrorCode.AlreadyExists,
                $"A WSL distribution named '{request.Name}' already exists.");
        }

        string installPath = _fileSystem.Path.Combine(fleetRoot, request.Name);

        if (HasContent(installPath))
        {
            return OperationResult<BuildPlan>.Fail(
                ErrorCode.AlreadyExists,
                $"Fleet folder '{installPath}' already exists and is not empty.");
        }

        OperationResult<BuildSourcePaths> source = ValidateSource(request);

        if (!source.IsSuccess)
        {
            return OperationResult<BuildPlan>.Fail(
                source.Error!.Code,
                source.Error.Message,
                source.Error.Detail);
        }

        OperationResult<string?> onboarding = ValidateOnboarding(request.OnboardingPath);

        if (!onboarding.IsSuccess)
        {
            return OperationResult<BuildPlan>.Fail(
                onboarding.Error!.Code,
                onboarding.Error.Message,
                onboarding.Error.Detail);
        }

        string buildId = _buildIds.NextBuildId();
        string? imageTag = request.SourceKind is BuildSourceKind.Dockerfile or BuildSourceKind.Recipe
            ? $"wsl-squad/{DockerTag.Slugify(request.Name)}:{buildId}"
            : null;

        BuildPlan plan = new(
            request.Name,
            user,
            request.SourceKind,
            buildId,
            installPath,
            _fileSystem.Path.Combine(fleetRoot, ".staging", buildId),
            imageTag,
            source.Value.DockerfilePath,
            source.Value.ContextPath,
            source.Value.ImagePath,
            source.Value.RegistryReference,
            onboarding.Value);

        return OperationResult<BuildPlan>.Ok(plan);
    }

    private bool IsRegisteredAlready(string name)
    {
        if (!_registry.IsSupported)
        {
            return false;
        }

        try
        {
            return _registry.GetDistributions()
                .Any(entry => string.Equals(entry.Name, name, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception exception) when (exception is PlatformNotSupportedException or IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private bool HasContent(string directory)
    {
        try
        {
            return _fileSystem.Directory.Exists(directory) &&
                   _fileSystem.Directory.EnumerateFileSystemEntries(directory).Any();
        }
        catch (IOException)
        {
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            return true;
        }
    }

    private OperationResult<BuildSourcePaths> ValidateSource(BuildRequest request)
    {
        try
        {
            return request.SourceKind switch
            {
                BuildSourceKind.Dockerfile => ValidateDockerfile(request),
                BuildSourceKind.Recipe => ValidateRecipe(request),
                BuildSourceKind.Image => _fileSystem.File.Exists(request.SourcePath)
                    ? OperationResult<BuildSourcePaths>.Ok(new BuildSourcePaths(null, null, request.SourcePath, null))
                    : InvalidSource($"Image archive '{request.SourcePath}' does not exist."),
                BuildSourceKind.Registry => ValidateRegistryReference(request.SourcePath),
                _ => InvalidSource($"Build source '{request.SourceKind}' is not supported."),
            };
        }
        catch (IOException exception)
        {
            return OperationResult<BuildSourcePaths>.Fail(ErrorCode.IoError, "The build source could not be read.", exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            return OperationResult<BuildSourcePaths>.Fail(ErrorCode.PermissionDenied, "The build source could not be read.", exception.Message);
        }
    }

    private OperationResult<BuildSourcePaths> ValidateDockerfile(BuildRequest request)
    {
        if (!_fileSystem.File.Exists(request.SourcePath))
        {
            return InvalidSource($"Dockerfile '{request.SourcePath}' does not exist.");
        }

        string? context = string.IsNullOrWhiteSpace(request.Context)
            ? _fileSystem.Path.GetDirectoryName(request.SourcePath)
            : request.Context;

        if (context is null || !_fileSystem.Directory.Exists(context))
        {
            return InvalidSource($"Build context '{context}' does not exist.");
        }

        return OperationResult<BuildSourcePaths>.Ok(
            new BuildSourcePaths(request.SourcePath, context, null, null));
    }

    private OperationResult<BuildSourcePaths> ValidateRecipe(BuildRequest request)
    {
        if (!_fileSystem.Directory.Exists(request.SourcePath))
        {
            return InvalidSource($"Recipe folder '{request.SourcePath}' does not exist.");
        }

        string dockerfile = _fileSystem.Path.Combine(request.SourcePath, "Dockerfile");

        return _fileSystem.File.Exists(dockerfile)
            ? OperationResult<BuildSourcePaths>.Ok(new BuildSourcePaths(dockerfile, request.SourcePath, null, null))
            : InvalidSource($"Recipe '{request.SourcePath}' has no Dockerfile.");
    }

    private static OperationResult<BuildSourcePaths> ValidateRegistryReference(string reference)
    {
        if (string.IsNullOrWhiteSpace(reference) || reference.Any(char.IsWhiteSpace))
        {
            return InvalidSource($"Registry reference '{reference}' is invalid.");
        }

        return OperationResult<BuildSourcePaths>.Ok(
            new BuildSourcePaths(null, null, null, reference.Trim()));
    }

    private OperationResult<string?> ValidateOnboarding(string? onboardingPath)
    {
        if (string.IsNullOrWhiteSpace(onboardingPath))
        {
            return OperationResult<string?>.Ok(null);
        }

        if (!_fileSystem.File.Exists(onboardingPath))
        {
            return OperationResult<string?>.Fail(
                ErrorCode.Validation,
                $"Onboarding script '{onboardingPath}' does not exist.");
        }

        if (PathHelper.ToWslMountPath(onboardingPath) is null)
        {
            return OperationResult<string?>.Fail(
                ErrorCode.Validation,
                $"Onboarding script '{onboardingPath}' must be on a Windows drive.");
        }

        return OperationResult<string?>.Ok(onboardingPath);
    }

    private static OperationResult<BuildSourcePaths> InvalidSource(string message)
    {
        return OperationResult<BuildSourcePaths>.Fail(ErrorCode.Validation, message);
    }

    [GeneratedRegex("^[a-z][a-z0-9_-]{0,31}$", RegexOptions.CultureInvariant)]
    private static partial Regex UserPattern();

    private sealed record BuildSourcePaths(
        string? DockerfilePath,
        string? ContextPath,
        string? ImagePath,
        string? RegistryReference);
}

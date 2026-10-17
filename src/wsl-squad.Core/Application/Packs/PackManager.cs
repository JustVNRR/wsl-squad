using System.IO.Abstractions;

using wsl_squad.Core.Abstractions.Process;
using wsl_squad.Core.Abstractions.Windows;
using wsl_squad.Core.Application.Common;
using wsl_squad.Core.Application.Fleet;
using wsl_squad.Core.Application.Lifecycle;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Fleet;
using wsl_squad.Core.Domain.Packs;
using wsl_squad.Core.Infrastructure.Storage;

namespace wsl_squad.Core.Application.Packs;

/// <summary>Reports and manages the packs installed in an instance.</summary>
public sealed class PackManager
{
    private readonly IFileSystem _fileSystem;
    private readonly IWslPackGateway _gateway;
    private readonly InstanceStateService _instances;
    private readonly InstanceMetadataStore _metadata;
    private readonly PackCatalogRepository _catalogRepository;

    public PackManager(
        IFileSystem fileSystem,
        IWslPackGateway gateway,
        InstanceStateService instances,
        InstanceMetadataStore metadata,
        PackCatalogRepository catalogRepository)
    {
        _fileSystem = fileSystem;
        _gateway = gateway;
        _instances = instances;
        _metadata = metadata;
        _catalogRepository = catalogRepository;
    }

    public async Task<OperationResult<PackStatus>> GetStatusAsync(
        string fleetRoot,
        string catalogRoot,
        string instanceName,
        CancellationToken cancellationToken = default)
    {
        OperationResult<string> managed = _instances.GetManagedPath(fleetRoot, instanceName);

        if (!managed.IsSuccess)
        {
            return OperationFailure.From<PackStatus>(managed.Error!);
        }

        OperationResult<PackCatalogLoadResult> catalog = _catalogRepository.Load(catalogRoot);

        if (!catalog.IsSuccess)
        {
            return OperationFailure.From<PackStatus>(catalog.Error!);
        }

        try
        {
            IReadOnlyList<string> installed = await _gateway
                .GetInstalledPackNamesAsync(instanceName, cancellationToken)
                .ConfigureAwait(false);
            IReadOnlyList<string> family = await _gateway
                .GetDistributionFamilyTokensAsync(instanceName, cancellationToken)
                .ConfigureAwait(false);

            List<WslPack> candidates =
            [
                .. catalog.Value.Packs.Where(pack =>
                    pack.Visible &&
                    PackDependencyResolver.MatchesFamily(pack, family) &&
                    !installed.Contains(pack.Name, StringComparer.OrdinalIgnoreCase)),
            ];

            List<string> removable =
            [
                .. installed.Where(name => catalog.Value.Packs.Any(pack =>
                    pack.Visible && string.Equals(pack.Name, name, StringComparison.OrdinalIgnoreCase))),
            ];

            PackStatus status = new(instanceName, installed, candidates, removable, catalog.Value.Packs);

            return catalog.Value.Warnings.Count == 0
                ? OperationResult<PackStatus>.Ok(status)
                : OperationResult<PackStatus>.Warn(status, [.. catalog.Value.Warnings]);
        }
        catch (Exception exception) when (OperationFailure.IsExpected(exception))
        {
            return OperationFailure.From<PackStatus>(exception);
        }
    }

    public async Task<OperationResult<PackManageOutcome>> ManageAsync(
        string fleetRoot,
        string catalogRoot,
        string instanceName,
        IReadOnlyList<string> add,
        IReadOnlyList<string> remove,
        CancellationToken cancellationToken = default)
    {
        OperationResult<string> managed = _instances.GetManagedPath(fleetRoot, instanceName);

        if (!managed.IsSuccess)
        {
            return OperationFailure.From<PackManageOutcome>(managed.Error!);
        }

        OperationResult<PackCatalogLoadResult> catalog = _catalogRepository.Load(catalogRoot);

        if (!catalog.IsSuccess)
        {
            return OperationFailure.From<PackManageOutcome>(catalog.Error!);
        }

        try
        {
            IReadOnlyList<string> installed = await _gateway
                .GetInstalledPackNamesAsync(instanceName, cancellationToken)
                .ConfigureAwait(false);
            IReadOnlyList<string> family = await _gateway
                .GetDistributionFamilyTokensAsync(instanceName, cancellationToken)
                .ConfigureAwait(false);

            OperationResult<IReadOnlyList<WslPack>> installPlan =
                PackDependencyResolver.ResolveInstall(catalog.Value.Packs, add);

            if (!installPlan.IsSuccess)
            {
                return OperationFailure.From<PackManageOutcome>(installPlan.Error!);
            }

            List<string> incompatible =
            [
                .. installPlan.Value
                    .Where(pack => !PackDependencyResolver.MatchesFamily(pack, family))
                    .Select(pack => pack.Name),
            ];

            if (incompatible.Count > 0)
            {
                return OperationResult<PackManageOutcome>.Fail(
                    ErrorCode.Validation,
                    $"Packs incompatible with this distribution: {string.Join(", ", incompatible)}.");
            }

            OperationResult<PackRemovalPlan> removalPlan =
                PackDependencyResolver.ResolveRemoval(catalog.Value.Packs, installed, remove);

            if (!removalPlan.IsSuccess)
            {
                return OperationFailure.From<PackManageOutcome>(removalPlan.Error!);
            }

            string user = GetUser(managed.Value);
            string home = await _gateway.GetUserHomeAsync(instanceName, user, cancellationToken)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(home))
            {
                return OperationResult<PackManageOutcome>.Fail(
                    ErrorCode.ExternalTool,
                    $"The home folder of '{user}' could not be resolved.");
            }

            List<string> added = [];
            List<string> removed = [];
            List<PackOperationFailure> failures = [];

            foreach (WslPack pack in removalPlan.Value.RemovalOrder)
            {
                try
                {
                    await RemovePackAsync(instanceName, home, pack, cancellationToken)
                        .ConfigureAwait(false);
                    removed.Add(pack.Name);
                }
                catch (Exception exception) when (OperationFailure.IsExpected(exception))
                {
                    failures.Add(ToFailure(pack.Name, "remove", exception));
                }
            }

            if (removed.Count > 0)
            {
                await RunCleanupAsync(catalogRoot, instanceName, failures, cancellationToken)
                    .ConfigureAwait(false);
            }

            foreach (WslPack pack in installPlan.Value)
            {
                if (installed.Contains(pack.Name, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    await InstallPackAsync(instanceName, user, home, pack, cancellationToken)
                        .ConfigureAwait(false);
                    added.Add(pack.Name);
                }
                catch (Exception exception) when (OperationFailure.IsExpected(exception))
                {
                    failures.Add(ToFailure(pack.Name, "install", exception));
                    await TryRemovePackFolderAsync(instanceName, home, pack.Name)
                        .ConfigureAwait(false);
                }
            }

            PackManageOutcome outcome = new(instanceName, added, removed, failures);

            return failures.Count == 0
                ? OperationResult<PackManageOutcome>.Ok(outcome)
                : OperationResult<PackManageOutcome>.Partial(
                    outcome,
                    new OperationError(ErrorCode.ExternalTool, $"{failures.Count} pack step(s) failed."),
                    [.. failures.Select(failure => $"{failure.PackName}: {failure.Step}: {failure.Message}")]);
        }
        catch (Exception exception) when (OperationFailure.IsExpected(exception))
        {
            return OperationFailure.From<PackManageOutcome>(exception);
        }
    }

    private async Task InstallPackAsync(
        string instanceName,
        string user,
        string home,
        WslPack pack,
        CancellationToken cancellationToken)
    {
        await _gateway.CopyPackIntoDistributionAsync(
            instanceName,
            user,
            pack.DirectoryPath,
            pack.Name,
            cancellationToken).ConfigureAwait(false);

        string installedFolder = $"{home}/.config/packs/{pack.Name}";

        if (_fileSystem.File.Exists(_fileSystem.Path.Combine(pack.DirectoryPath, "install_root.sh")))
        {
            await _gateway.RunScriptAsync(
                instanceName,
                "root",
                home,
                $"{installedFolder}/install_root.sh",
                [],
                cancellationToken).ConfigureAwait(false);
        }

        if (!_fileSystem.File.Exists(_fileSystem.Path.Combine(pack.DirectoryPath, "install.sh")))
        {
            return;
        }

        bool sudoOpened = false;

        try
        {
            await _gateway.OpenPackSudoAsync(instanceName, user, cancellationToken).ConfigureAwait(false);
            sudoOpened = true;

            await _gateway.RunScriptAsync(
                instanceName,
                user,
                home,
                $"{installedFolder}/install.sh",
                [],
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (sudoOpened)
            {
                await TryCloseSudoAsync(instanceName).ConfigureAwait(false);
            }
        }
    }

    private async Task RemovePackAsync(
        string instanceName,
        string home,
        WslPack pack,
        CancellationToken cancellationToken)
    {
        string installedFolder = $"{home}/.config/packs/{pack.Name}";

        if (_fileSystem.File.Exists(_fileSystem.Path.Combine(pack.DirectoryPath, "remove.sh")))
        {
            await _gateway.RunScriptAsync(
                instanceName,
                "root",
                home,
                $"{installedFolder}/remove.sh",
                [],
                cancellationToken).ConfigureAwait(false);
        }

        if (_fileSystem.File.Exists(_fileSystem.Path.Combine(pack.DirectoryPath, "remove_root.sh")))
        {
            await _gateway.RunScriptAsync(
                instanceName,
                "root",
                home,
                $"{installedFolder}/remove_root.sh",
                [],
                cancellationToken).ConfigureAwait(false);
        }

        await _gateway.RemovePackFolderAsync(instanceName, home, pack.Name, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task RunCleanupAsync(
        string catalogRoot,
        string instanceName,
        List<PackOperationFailure> failures,
        CancellationToken cancellationToken)
    {
        string windowsScript = _fileSystem.Path.Combine(catalogRoot, "packs", "cleanup_orphans.sh");

        if (!_fileSystem.File.Exists(windowsScript))
        {
            return;
        }

        string? linuxScript = PathHelper.ToWslMountPath(windowsScript);

        if (linuxScript is null)
        {
            return;
        }

        try
        {
            await _gateway.RunCleanupOrphansAsync(instanceName, linuxScript, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (OperationFailure.IsExpected(exception))
        {
            failures.Add(ToFailure("cleanup", "cleanup_orphans.sh", exception));
        }
    }

    private async Task TryCloseSudoAsync(string instanceName)
    {
        try
        {
            await _gateway.ClosePackSudoAsync(instanceName, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception) when (OperationFailure.IsExpected(exception))
        {
            // Removing the temporary sudo rule is best-effort; the instance remains usable.
        }
    }

    private async Task TryRemovePackFolderAsync(string instanceName, string home, string packName)
    {
        try
        {
            await _gateway.RemovePackFolderAsync(instanceName, home, packName, CancellationToken.None)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (OperationFailure.IsExpected(exception))
        {
            // Cleanup after a failed install is best-effort.
        }
    }

    private string GetUser(string installPath)
    {
        OperationResult<InstanceMetadata> metadata = _metadata.Read(installPath);

        return metadata.IsSuccess ? metadata.Value.DefaultUser : InstanceMetadata.DefaultUserName;
    }

    private static PackOperationFailure ToFailure(string packName, string step, Exception exception)
    {
        int? exitCode = exception is ProcessExecutionException process ? process.Result.ExitCode : null;

        return new PackOperationFailure(packName, step, exception.Message, exitCode);
    }
}

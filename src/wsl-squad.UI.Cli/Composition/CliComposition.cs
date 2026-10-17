using System.IO.Abstractions;

using wsl_squad.Core.Application.Appearance;
using wsl_squad.Core.Application.Build;
using wsl_squad.Core.Application.Catalog;
using wsl_squad.Core.Application.Docker;
using wsl_squad.Core.Application.Common;
using wsl_squad.Core.Application.Fleet;
using wsl_squad.Core.Application.Lifecycle;
using wsl_squad.Core.Application.Migration;
using wsl_squad.Core.Application.Packs;
using wsl_squad.Core.Domain.Appearance;
using wsl_squad.Core.Domain.Build;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Fleet;
using wsl_squad.Core.Domain.Lifecycle;
using wsl_squad.Core.Domain.Migration;
using wsl_squad.Core.Domain.Packs;
using wsl_squad.Core.Infrastructure.Build;
using wsl_squad.Core.Infrastructure.Docker;
using wsl_squad.Core.Infrastructure.Environment;
using wsl_squad.Core.Infrastructure.Process;
using wsl_squad.Core.Infrastructure.Resources;
using wsl_squad.Core.Infrastructure.Storage;
using wsl_squad.Core.Infrastructure.Time;
using wsl_squad.Core.Infrastructure.Windows;
using wsl_squad.Presentation.Dtos.Fleet;
using wsl_squad.Presentation.Presenters.Fleet;

namespace wsl_squad.UI.Cli.Composition;

/// <summary>Builds the CLI object graph.</summary>
internal static class CliComposition
{
    public static async Task<OperationResult<FleetSnapshotDto>> FleetSnapshotAsync(
        bool includeArchives,
        CancellationToken cancellationToken)
    {
        FleetListPresenter presenter = CreateListPresenter();

        return await presenter.LoadAsync(includeArchives, cancellationToken).ConfigureAwait(false);
    }

    public static OperationResult<string> EnsureWslConfig()
    {
        CliContext context = CliContext.Create();
        string path = Path.Combine(context.Platform.UserProfile, ".wslconfig");

        try
        {
            if (!context.FileSystem.File.Exists(path))
            {
                AtomicFileWriter.WriteAllText(
                    context.FileSystem,
                    path,
                    "# WSL global configuration.\n# See https://learn.microsoft.com/windows/wsl/wsl-config\n");
            }

            return OperationResult<string>.Ok(path);
        }
        catch (IOException exception)
        {
            return OperationResult<string>.Fail(
                ErrorCode.IoError,
                $"'{path}' could not be created.",
                exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            return OperationResult<string>.Fail(
                ErrorCode.PermissionDenied,
                $"'{path}' could not be created.",
                exception.Message);
        }
    }

    public static FleetListPresenter CreateListPresenter()
    {
        FileSystem fileSystem = new();
        SystemPlatformEnvironment platform = new();
        FleetConfigStore configStore = new(fileSystem, AppPaths.GetConfigFilePath(platform));
        FleetMarkerStore markers = new(fileSystem);
        WindowsRegistryGateway registry = new();
        WslCliGateway wsl = new(new ProcessRunner());
        InstanceMetadataStore metadataStore = new(fileSystem, new SystemClock());
        ArchiveStore archiveStore = new(fileSystem);
        FleetDiscoveryService discovery = new(
            fileSystem,
            registry,
            wsl,
            markers,
            metadataStore,
            archiveStore);
        FleetRootResolver resolver = new(platform, configStore, registry, markers);

        return new FleetListPresenter(resolver, discovery);
    }

    public static async Task<OperationResult<BuildSummary>> BuildInstanceAsync(
        BuildRequest request,
        CancellationToken cancellationToken)
    {
        FileSystem fileSystem = new();
        SystemPlatformEnvironment platform = new();
        SystemClock clock = new();
        FleetConfigStore configStore = new(fileSystem, AppPaths.GetConfigFilePath(platform));
        FleetMarkerStore markers = new(fileSystem);
        WindowsRegistryGateway registry = new();
        ProcessRunner processRunner = new();
        FleetRootResolver resolver = new(platform, configStore, registry, markers);

        OperationResult<string> root = resolver.Resolve();

        if (!root.IsSuccess)
        {
            return FailFrom(root.Error!);
        }

        OperationResult<FleetConfig?> config = configStore.Load();

        if (!config.IsSuccess)
        {
            return FailFrom(config.Error!);
        }

        string catalogRoot = config.Value?.CatalogRoot
            ?? Path.Combine(AppPaths.GetConfigDirectory(platform), "catalog");

        CatalogSeeder seeder = new(
            fileSystem,
            new AssemblyEmbeddedResourceCatalog(typeof(FleetConfig).Assembly));

        OperationResult<CatalogSeedResult> seed = seeder.Seed(catalogRoot);

        if (!seed.IsSuccess)
        {
            return FailFrom(seed.Error!);
        }

        if (request.SourceKind == BuildSourceKind.Recipe &&
            !PathHelper.IsFullyQualified(request.SourcePath))
        {
            request = request with
            {
                SourcePath = Path.Combine(catalogRoot, "recipes", request.SourcePath),
            };
        }

        WslCliGateway wsl = new(processRunner);
        InstanceMetadataStore metadataStore = new(fileSystem, clock);
        BuildInstanceHandler handler = new(
            fileSystem,
            wsl,
            new DockerCliGateway(processRunner, fileSystem),
            markers,
            metadataStore,
            new BuildPlanFactory(fileSystem, registry, new TimestampBuildIdGenerator(clock)),
            clock);

        OperationResult<BuildSummary> built = await handler
            .BuildAsync(root.Value, catalogRoot, request, cancellationToken)
            .ConfigureAwait(false);

        if (!built.IsSuccess)
        {
            return built;
        }

        List<string> warnings = [.. built.Warnings];

        if (request.Packs.Count > 0)
        {
            PackManager packManager = new(
                fileSystem,
                wsl,
                new InstanceStateService(fileSystem, markers, wsl),
                metadataStore,
                new PackCatalogRepository(fileSystem));

            OperationResult<PackManageOutcome> packs = await packManager
                .ManageAsync(root.Value, catalogRoot, built.Value.Name, request.Packs, [], cancellationToken)
                .ConfigureAwait(false);

            if (!packs.IsSuccess)
            {
                warnings.Add($"packs: {packs.Error!.Message}");
            }
            else
            {
                foreach (string warning in packs.Warnings)
                {
                    warnings.Add(warning);
                }

                foreach (PackOperationFailure failure in packs.Value.Failed)
                {
                    warnings.Add($"pack {failure.PackName} ({failure.Step}): {failure.Message}");
                }
            }
        }

        if (request.RegisterDocker)
        {
            DockerDesktopService dockerService = new(
                new DockerDesktopStore(fileSystem, platform),
                new InstanceStateService(fileSystem, markers, wsl),
                processRunner);

            OperationResult<DockerIntegrationOutcome> integration = await dockerService
                .RegisterAsync(root.Value, built.Value.Name, restart: true, cancellationToken)
                .ConfigureAwait(false);

            if (!integration.IsSuccess)
            {
                warnings.Add($"docker: {integration.Error!.Message}");
            }
            else
            {
                foreach (string warning in integration.Warnings)
                {
                    warnings.Add(warning);
                }
            }
        }

        return warnings.Count == 0
            ? built
            : OperationResult<BuildSummary>.Warn(built.Value, [.. warnings]);
    }

    public static async Task<OperationResult<RootMoveOutcome>> MoveRootAsync(
        string targetRoot,
        CancellationToken cancellationToken)
    {
        CliContext context = CliContext.Create();
        OperationResult<string> root = context.ResolveRoot();

        if (!root.IsSuccess)
        {
            return OperationFailure.From<RootMoveOutcome>(root.Error!);
        }

        RemoveInstanceHandler remove = new(
            context.FileSystem,
            context.Instances,
            context.Wsl,
            context.Registry,
            context.ArchiveService);
        RootMoveService service = new(
            context.FileSystem,
            context.Markers,
            context.ArchiveService,
            remove,
            context.Archives,
            context.ConfigStore);

        return await service.MoveAsync(root.Value, targetRoot, cancellationToken).ConfigureAwait(false);
    }

    public static OperationResult<MigrationPlan> CreateMigrationPlan(string? sourceRoot)
    {
        CliContext context = CliContext.Create();

        return CreateMigrator(context).CreatePlan(sourceRoot);
    }

    public static OperationResult<MigrationOutcome> MigrateLegacy(string? sourceRoot)
    {
        CliContext context = CliContext.Create();

        return CreateMigrator(context).Migrate(sourceRoot);
    }

    public static OperationResult MigrateRollback(string backupId)
    {
        CliContext context = CliContext.Create();

        return CreateMigrator(context).Rollback(backupId);
    }

    private static LegacyMigrator CreateMigrator(CliContext context)
    {
        return new LegacyMigrator(
            context.FileSystem,
            context.Platform,
            context.Registry,
            context.Markers,
            context.Metadata,
            context.Archives,
            context.ConfigStore,
            context.Clock);
    }

    public static OperationResult<AppearanceOutcome> AppearanceGet(string instanceName)
    {
        CliContext context = CliContext.Create();
        OperationResult<string> root = context.ResolveRoot();

        return root.IsSuccess
            ? CreateAppearanceService(context).Get(root.Value, instanceName)
            : OperationFailure.From<AppearanceOutcome>(root.Error!);
    }

    public static OperationResult<AppearanceOutcome> AppearanceSet(
        string instanceName,
        AppearanceRequest request)
    {
        CliContext context = CliContext.Create();
        OperationResult<string> root = context.ResolveRoot();

        return root.IsSuccess
            ? CreateAppearanceService(context).Set(root.Value, instanceName, request)
            : OperationFailure.From<AppearanceOutcome>(root.Error!);
    }

    private static AppearanceService CreateAppearanceService(CliContext context)
    {
        return new AppearanceService(
            context.FileSystem,
            context.Instances,
            context.Metadata,
            new WindowsTerminalStore(context.FileSystem, context.Platform),
            new GdiIconRenderer(),
            context.Clock);
    }

    public static async Task<OperationResult<DockerIntegrationOutcome>> DockerSetAsync(
        string instanceName,
        bool integrated,
        bool restart,
        CancellationToken cancellationToken)
    {
        CliContext context = CliContext.Create();
        OperationResult<string> root = context.ResolveRoot();

        if (!root.IsSuccess)
        {
            return OperationFailure.From<DockerIntegrationOutcome>(root.Error!);
        }

        DockerDesktopService service = new(
            new DockerDesktopStore(context.FileSystem, context.Platform),
            context.Instances,
            context.ProcessRunner);

        return integrated
            ? await service.RegisterAsync(root.Value, instanceName, restart, cancellationToken).ConfigureAwait(false)
            : await service.UnregisterAsync(root.Value, instanceName, restart, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<OperationResult<PackStatus>> PackStatusAsync(
        string instanceName,
        CancellationToken cancellationToken)
    {
        CliContext context = CliContext.Create();
        OperationResult<CliRoots> roots = ResolveRoots(context);

        if (!roots.IsSuccess)
        {
            return OperationFailure.From<PackStatus>(roots.Error!);
        }

        return await CreatePackManager(context)
            .GetStatusAsync(roots.Value.FleetRoot, roots.Value.CatalogRoot, instanceName, cancellationToken)
            .ConfigureAwait(false);
    }

    public static async Task<OperationResult<PackManageOutcome>> PackManageAsync(
        string instanceName,
        IReadOnlyList<string> add,
        IReadOnlyList<string> remove,
        CancellationToken cancellationToken)
    {
        CliContext context = CliContext.Create();
        OperationResult<CliRoots> roots = ResolveRoots(context);

        if (!roots.IsSuccess)
        {
            return OperationFailure.From<PackManageOutcome>(roots.Error!);
        }

        return await CreatePackManager(context)
            .ManageAsync(roots.Value.FleetRoot, roots.Value.CatalogRoot, instanceName, add, remove, cancellationToken)
            .ConfigureAwait(false);
    }

    public static OperationResult<IReadOnlyList<WslPack>> PackCatalogList()
    {
        CliContext context = CliContext.Create();
        OperationResult<CliRoots> roots = ResolveRoots(context);

        return roots.IsSuccess
            ? CreatePackCatalogService(context).List(roots.Value.CatalogRoot)
            : OperationFailure.From<IReadOnlyList<WslPack>>(roots.Error!);
    }

    public static OperationResult<string> PackCatalogDirectory(string packName)
    {
        CliContext context = CliContext.Create();
        OperationResult<CliRoots> roots = ResolveRoots(context);

        return roots.IsSuccess
            ? CreatePackCatalogService(context).GetDirectory(roots.Value.CatalogRoot, packName)
            : OperationFailure.From<string>(roots.Error!);
    }

    public static OperationResult<string> PackCatalogDuplicate(string sourceName, string targetName)
    {
        CliContext context = CliContext.Create();
        OperationResult<CliRoots> roots = ResolveRoots(context);

        return roots.IsSuccess
            ? CreatePackCatalogService(context).Duplicate(roots.Value.CatalogRoot, sourceName, targetName)
            : OperationFailure.From<string>(roots.Error!);
    }

    public static OperationResult PackCatalogDelete(string packName)
    {
        CliContext context = CliContext.Create();
        OperationResult<CliRoots> roots = ResolveRoots(context);

        if (!roots.IsSuccess)
        {
            OperationError error = roots.Error!;

            return OperationResult.Fail(error.Code, error.Message, error.Detail);
        }

        return CreatePackCatalogService(context).Delete(roots.Value.CatalogRoot, packName);
    }

    private static PackManager CreatePackManager(CliContext context)
    {
        return new PackManager(
            context.FileSystem,
            context.Wsl,
            context.Instances,
            context.Metadata,
            new PackCatalogRepository(context.FileSystem));
    }

    private static PackCatalogService CreatePackCatalogService(CliContext context)
    {
        return new PackCatalogService(context.FileSystem, new PackCatalogRepository(context.FileSystem));
    }

    private static OperationResult<CliRoots> ResolveRoots(CliContext context)
    {
        OperationResult<string> root = context.ResolveRoot();

        if (!root.IsSuccess)
        {
            return OperationFailure.From<CliRoots>(root.Error!);
        }

        OperationResult<FleetConfig?> config = context.ConfigStore.Load();

        if (!config.IsSuccess)
        {
            return OperationFailure.From<CliRoots>(config.Error!);
        }

        string catalogRoot = config.Value?.CatalogRoot
            ?? Path.Combine(AppPaths.GetConfigDirectory(context.Platform), "catalog");

        CatalogSeeder seeder = new(
            context.FileSystem,
            new AssemblyEmbeddedResourceCatalog(typeof(FleetConfig).Assembly));
        OperationResult<CatalogSeedResult> seed = seeder.Seed(catalogRoot);

        return seed.IsSuccess
            ? OperationResult<CliRoots>.Ok(new CliRoots(root.Value, catalogRoot))
            : OperationFailure.From<CliRoots>(seed.Error!);
    }

    private sealed record CliRoots(string FleetRoot, string CatalogRoot);

    public static OperationResult<string> InitializeFleetRoot(string? requestedRoot)
    {
        FileSystem fileSystem = new();
        SystemPlatformEnvironment platform = new();
        FleetConfigStore configStore = new(fileSystem, AppPaths.GetConfigFilePath(platform));
        FleetMarkerStore markers = new(fileSystem);
        WindowsRegistryGateway registry = new();
        FleetRootResolver resolver = new(platform, configStore, registry, markers);

        OperationResult<string> root = ResolveRequestedRoot(requestedRoot, resolver);

        if (!root.IsSuccess)
        {
            return root;
        }

        try
        {
            _ = fileSystem.Directory.CreateDirectory(root.Value);
        }
        catch (IOException exception)
        {
            return OperationResult<string>.Fail(
                ErrorCode.IoError,
                $"Fleet root '{root.Value}' could not be created.",
                exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            return OperationResult<string>.Fail(
                ErrorCode.PermissionDenied,
                $"Fleet root '{root.Value}' could not be created.",
                exception.Message);
        }

        OperationResult saved = configStore.Save(new FleetConfig { FleetRoot = root.Value });

        if (!saved.IsSuccess)
        {
            return OperationResult<string>.Fail(
                saved.Error!.Code,
                saved.Error.Message,
                saved.Error.Detail,
                saved.Error.ExternalExitCode);
        }

        string catalogRoot = Path.Combine(AppPaths.GetConfigDirectory(platform), "catalog");
        CatalogSeeder seeder = new(
            fileSystem,
            new AssemblyEmbeddedResourceCatalog(typeof(FleetConfig).Assembly));
        OperationResult<CatalogSeedResult> seed = seeder.Seed(catalogRoot);

        return seed.IsSuccess
            ? root
            : OperationResult<string>.Warn(root.Value, $"catalog: {seed.Error!.Message}");
    }

    private static OperationResult<string> ResolveRequestedRoot(string? requestedRoot, FleetRootResolver resolver)
    {
        if (string.IsNullOrWhiteSpace(requestedRoot))
        {
            return resolver.Resolve();
        }

        if (!PathHelper.IsFullyQualified(requestedRoot))
        {
            return OperationResult<string>.Fail(
                ErrorCode.Validation,
                $"Fleet root '{requestedRoot}' is not an absolute path.");
        }

        return OperationResult<string>.Ok(PathHelper.NormalizeSeparators(requestedRoot).TrimEnd('/'));
    }

    public static OperationResult<OpenOutcome> Open(string instanceName)
    {
        CliContext context = CliContext.Create();
        OperationResult<string> root = context.ResolveRoot();

        return root.IsSuccess
            ? new OpenHandler(context.Instances, context.Terminal).Open(root.Value, instanceName)
            : OperationFailure.From<OpenOutcome>(root.Error!);
    }

    public static async Task<OperationResult<RestartOutcome>> RestartAsync(
        string instanceName,
        CancellationToken cancellationToken)
    {
        CliContext context = CliContext.Create();
        OperationResult<string> root = context.ResolveRoot();

        return root.IsSuccess
            ? await new RestartHandler(context.Instances, context.Wsl, context.Terminal)
                .RestartAsync(root.Value, instanceName, cancellationToken)
                .ConfigureAwait(false)
            : OperationFailure.From<RestartOutcome>(root.Error!);
    }

    public static async Task<OperationResult<ArchiveOutcome>> ArchiveAsync(
        ArchiveRequest request,
        CancellationToken cancellationToken)
    {
        CliContext context = CliContext.Create();
        OperationResult<string> root = context.ResolveRoot();

        return root.IsSuccess
            ? await context.ArchiveService.ArchiveAsync(root.Value, request, cancellationToken)
                .ConfigureAwait(false)
            : OperationFailure.From<ArchiveOutcome>(root.Error!);
    }

    public static OperationResult<ArchiveListing> ListArchives()
    {
        CliContext context = CliContext.Create();
        OperationResult<string> root = context.ResolveRoot();

        return root.IsSuccess
            ? context.Archives.List(root.Value)
            : OperationFailure.From<ArchiveListing>(root.Error!);
    }

    public static OperationResult<DeleteArchiveOutcome> DeleteArchive(string archiveName)
    {
        CliContext context = CliContext.Create();
        OperationResult<string> root = context.ResolveRoot();

        return root.IsSuccess
            ? new DeleteArchiveHandler(context.FileSystem, context.Archives).Delete(root.Value, archiveName)
            : OperationFailure.From<DeleteArchiveOutcome>(root.Error!);
    }

    public static async Task<OperationResult<RestoreOutcome>> RestoreAsync(
        RestoreRequest request,
        CancellationToken cancellationToken)
    {
        CliContext context = CliContext.Create();
        OperationResult<string> root = context.ResolveRoot();

        if (!root.IsSuccess)
        {
            return OperationFailure.From<RestoreOutcome>(root.Error!);
        }

        RestoreHandler handler = new(
            context.FileSystem,
            context.Wsl,
            context.Registry,
            context.Archives,
            context.Markers,
            context.Metadata,
            context.DiskSpace,
            context.Clock);

        return await handler.RestoreAsync(root.Value, request, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<OperationResult<DuplicateOutcome>> DuplicateAsync(
        DuplicateRequest request,
        CancellationToken cancellationToken)
    {
        CliContext context = CliContext.Create();
        OperationResult<string> root = context.ResolveRoot();

        if (!root.IsSuccess)
        {
            return OperationFailure.From<DuplicateOutcome>(root.Error!);
        }

        DuplicateHandler handler = new(
            context.FileSystem,
            context.Instances,
            context.Registry,
            context.Wsl,
            context.Markers,
            context.Metadata,
            context.Archives,
            context.DiskSpace,
            context.Clock);

        return await handler.DuplicateAsync(root.Value, request, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<OperationResult<RemoveOutcome>> RemoveAsync(
        RemoveRequest request,
        CancellationToken cancellationToken)
    {
        CliContext context = CliContext.Create();
        OperationResult<string> root = context.ResolveRoot();

        if (!root.IsSuccess)
        {
            return OperationFailure.From<RemoveOutcome>(root.Error!);
        }

        RemoveInstanceHandler handler = new(
            context.FileSystem,
            context.Instances,
            context.Wsl,
            context.Registry,
            context.ArchiveService);

        return await handler.RemoveAsync(root.Value, request, cancellationToken).ConfigureAwait(false);
    }

    public static async Task<OperationResult<CompactOutcome>> CompactAsync(
        CompactRequest request,
        CancellationToken cancellationToken)
    {
        CliContext context = CliContext.Create();
        OperationResult<string> root = context.ResolveRoot();

        if (!root.IsSuccess)
        {
            return OperationFailure.From<CompactOutcome>(root.Error!);
        }

        CompactHandler handler = new(context.Instances, context.Wsl, context.ArchiveService);

        return await handler.CompactAsync(root.Value, request, cancellationToken).ConfigureAwait(false);
    }

    private static OperationResult<BuildSummary> FailFrom(OperationError error)
    {
        return OperationResult<BuildSummary>.Fail(
            error.Code,
            error.Message,
            error.Detail,
            error.ExternalExitCode);
    }
}

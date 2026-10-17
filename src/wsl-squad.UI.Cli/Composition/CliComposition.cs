using System.IO.Abstractions;

using wsl_squad.Core.Application.Build;
using wsl_squad.Core.Application.Catalog;
using wsl_squad.Core.Application.Common;
using wsl_squad.Core.Application.Fleet;
using wsl_squad.Core.Application.Lifecycle;
using wsl_squad.Core.Domain.Build;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Fleet;
using wsl_squad.Core.Domain.Lifecycle;
using wsl_squad.Core.Infrastructure.Build;
using wsl_squad.Core.Infrastructure.Docker;
using wsl_squad.Core.Infrastructure.Environment;
using wsl_squad.Core.Infrastructure.Process;
using wsl_squad.Core.Infrastructure.Resources;
using wsl_squad.Core.Infrastructure.Storage;
using wsl_squad.Core.Infrastructure.Time;
using wsl_squad.Core.Infrastructure.Windows;
using wsl_squad.Presentation.Presenters.Fleet;

namespace wsl_squad.UI.Cli.Composition;

/// <summary>Builds the CLI object graph.</summary>
internal static class CliComposition
{
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

        BuildInstanceHandler handler = new(
            fileSystem,
            new WslCliGateway(processRunner),
            new DockerCliGateway(processRunner, fileSystem),
            markers,
            new InstanceMetadataStore(fileSystem, clock),
            new BuildPlanFactory(fileSystem, registry, new TimestampBuildIdGenerator(clock)),
            clock);

        return await handler.BuildAsync(root.Value, catalogRoot, request, cancellationToken)
            .ConfigureAwait(false);
    }

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

using wsl_squad.Core.Abstractions.Platform;
using wsl_squad.Core.Abstractions.Windows;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Fleet;
using wsl_squad.Core.Infrastructure.Storage;

namespace wsl_squad.Core.Application.Fleet;

/// <summary>Resolves the active fleet root from overrides, configuration, discovery or defaults.</summary>
public sealed class FleetRootResolver
{
    private readonly IPlatformEnvironment _platform;
    private readonly FleetConfigStore _configStore;
    private readonly IWslRegistryGateway _registry;
    private readonly FleetMarkerStore _markers;

    public FleetRootResolver(
        IPlatformEnvironment platform,
        FleetConfigStore configStore,
        IWslRegistryGateway registry,
        FleetMarkerStore markers)
    {
        _platform = platform;
        _configStore = configStore;
        _registry = registry;
        _markers = markers;
    }

    public OperationResult<string> Resolve()
    {
        string? overridden = AppPaths.GetFleetRootOverride(_platform);
        if (overridden is not null)
        {
            return Validate(overridden, AppPaths.FleetRootVariable);
        }

        OperationResult<FleetConfig?> configResult = _configStore.Load();
        if (!configResult.IsSuccess)
        {
            OperationError error = configResult.Error!;

            return OperationResult<string>.Fail(error.Code, error.Message, error.Detail, error.ExternalExitCode);
        }

        FleetConfig? config = configResult.Value;
        if (config is not null)
        {
            return Validate(config.FleetRoot, _configStore.ConfigPath);
        }

        if (_registry.IsSupported)
        {
            IReadOnlyList<string> candidates = FindMarkedRoots();

            if (candidates.Count == 1)
            {
                return OperationResult<string>.Ok(candidates[0]);
            }

            if (candidates.Count > 1)
            {
                return OperationResult<string>.Fail(
                    ErrorCode.Conflict,
                    "Several marked fleets were found. Set WSL_SQUAD_ROOT or create config.json to choose one.",
                    string.Join(", ", candidates));
            }
        }

        return OperationResult<string>.Ok(AppPaths.GetDefaultFleetRoot(_platform));
    }

    private IReadOnlyList<string> FindMarkedRoots()
    {
        Dictionary<string, string> roots = new(StringComparer.Ordinal);

        IEnumerable<string> parents = _registry.GetDistributions()
            .Where(entry => PathHelper.IsFullyQualified(entry.BasePath))
            .Where(entry => _markers.Exists(entry.BasePath))
            .Select(entry => PathHelper.GetParent(entry.BasePath))
            .OfType<string>();

        foreach (string parent in parents)
        {
            _ = roots.TryAdd(PathHelper.NormalizeForComparison(parent), parent);
        }

        return [.. roots.Values.OrderBy(static value => value, StringComparer.OrdinalIgnoreCase)];
    }

    private static OperationResult<string> Validate(string root, string source)
    {
        if (!PathHelper.IsFullyQualified(root))
        {
            return OperationResult<string>.Fail(
                ErrorCode.Validation,
                $"Fleet root '{root}' from {source} is not an absolute path.");
        }

        return OperationResult<string>.Ok(PathHelper.NormalizeSeparators(root).TrimEnd('/'));
    }
}

using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Packs;

namespace wsl_squad.Core.Application.Packs;

/// <summary>Orders installs and removals according to pack dependencies.</summary>
public static class PackDependencyResolver
{
    public static OperationResult<IReadOnlyList<WslPack>> ResolveInstall(
        IReadOnlyList<WslPack> catalog,
        IReadOnlyList<string> requested)
    {
        Dictionary<string, WslPack> byName = ToDictionary(catalog);
        List<WslPack> order = [];
        HashSet<string> visited = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> visiting = new(StringComparer.OrdinalIgnoreCase);

        foreach (string name in requested)
        {
            OperationResult<bool> visit = Visit(name, byName, order, visited, visiting);

            if (!visit.IsSuccess)
            {
                return OperationResult<IReadOnlyList<WslPack>>.Fail(
                    visit.Error!.Code,
                    visit.Error.Message,
                    visit.Error.Detail);
            }
        }

        return OperationResult<IReadOnlyList<WslPack>>.Ok(order);
    }

    public static OperationResult<PackRemovalPlan> ResolveRemoval(
        IReadOnlyList<WslPack> catalog,
        IReadOnlyList<string> installed,
        IReadOnlyList<string> requested)
    {
        Dictionary<string, WslPack> byName = ToDictionary(catalog);
        HashSet<string> installedSet = new(installed, StringComparer.OrdinalIgnoreCase);
        HashSet<string> requestedSet = new(requested, StringComparer.OrdinalIgnoreCase);

        foreach (string name in requestedSet)
        {
            if (!installedSet.Contains(name))
            {
                return OperationResult<PackRemovalPlan>.Fail(
                    ErrorCode.NotFound,
                    $"Pack '{name}' is not installed.");
            }
        }

        List<string> blockers = FindBlockers(byName, installedSet, requestedSet);

        if (blockers.Count > 0)
        {
            return OperationResult<PackRemovalPlan>.Fail(
                ErrorCode.PackConflict,
                $"Packs needed by remaining packs cannot be removed: {string.Join(", ", blockers)}.",
                string.Join(", ", blockers));
        }

        HashSet<string> removal = new(requestedSet, StringComparer.OrdinalIgnoreCase);
        bool changed;

        do
        {
            changed = false;
            HashSet<string> required = RequiredByRemaining(byName, installedSet, removal);

            foreach (string installedName in installedSet)
            {
                if (removal.Contains(installedName) || required.Contains(installedName))
                {
                    continue;
                }

                if (byName.TryGetValue(installedName, out WslPack? pack) && !pack.Visible)
                {
                    _ = removal.Add(installedName);
                    changed = true;
                }
            }
        }
        while (changed);

        OperationResult<IReadOnlyList<WslPack>> ordered = ResolveInstall(catalog, [.. removal]);

        if (!ordered.IsSuccess)
        {
            return OperationResult<PackRemovalPlan>.Fail(
                ordered.Error!.Code,
                ordered.Error.Message,
                ordered.Error.Detail);
        }

        List<WslPack> removalOrder = [.. ordered.Value.Where(pack => removal.Contains(pack.Name))];
        removalOrder.Reverse();

        return OperationResult<PackRemovalPlan>.Ok(new PackRemovalPlan(removalOrder, blockers));
    }

    public static bool MatchesFamily(WslPack pack, IReadOnlyList<string> distributionTokens)
    {
        return string.IsNullOrWhiteSpace(pack.Family) ||
               distributionTokens.Contains(pack.Family, StringComparer.OrdinalIgnoreCase);
    }

    private static OperationResult<bool> Visit(
        string name,
        IReadOnlyDictionary<string, WslPack> byName,
        List<WslPack> order,
        HashSet<string> visited,
        HashSet<string> visiting)
    {
        if (visited.Contains(name))
        {
            return OperationResult<bool>.Ok(true);
        }

        if (!visiting.Add(name))
        {
            return OperationResult<bool>.Fail(
                ErrorCode.Validation,
                $"Pack dependency cycle detected at '{name}'.");
        }

        if (!byName.TryGetValue(name, out WslPack? pack))
        {
            return OperationResult<bool>.Fail(ErrorCode.NotFound, $"Pack '{name}' does not exist.");
        }

        foreach (string dependency in pack.Requires)
        {
            OperationResult<bool> result = Visit(dependency, byName, order, visited, visiting);

            if (!result.IsSuccess)
            {
                return result;
            }
        }

        _ = visiting.Remove(name);
        _ = visited.Add(name);
        order.Add(pack);

        return OperationResult<bool>.Ok(true);
    }

    private static List<string> FindBlockers(
        IReadOnlyDictionary<string, WslPack> byName,
        HashSet<string> installed,
        HashSet<string> requested)
    {
        List<string> blockers = [];

        foreach (string installedName in installed)
        {
            if (requested.Contains(installedName))
            {
                continue;
            }

            OperationResult<IReadOnlyList<WslPack>> closure = ResolveInstall([.. byName.Values], [installedName]);

            if (closure.IsSuccess &&
                closure.Value.Any(pack => requested.Contains(pack.Name)))
            {
                blockers.Add(installedName);
            }
        }

        return blockers;
    }

    private static HashSet<string> RequiredByRemaining(
        IReadOnlyDictionary<string, WslPack> byName,
        HashSet<string> installed,
        HashSet<string> removal)
    {
        HashSet<string> required = new(StringComparer.OrdinalIgnoreCase);

        foreach (string installedName in installed)
        {
            if (removal.Contains(installedName))
            {
                continue;
            }

            if (byName.TryGetValue(installedName, out WslPack? installedPack) && !installedPack.Visible)
            {
                continue;
            }

            OperationResult<IReadOnlyList<WslPack>> closure = ResolveInstall([.. byName.Values], [installedName]);

            if (!closure.IsSuccess)
            {
                continue;
            }

            foreach (WslPack pack in closure.Value)
            {
                _ = required.Add(pack.Name);
            }
        }

        return required;
    }

    private static Dictionary<string, WslPack> ToDictionary(IReadOnlyList<WslPack> catalog)
    {
        Dictionary<string, WslPack> byName = new(StringComparer.OrdinalIgnoreCase);

        foreach (WslPack pack in catalog)
        {
            byName[pack.Name] = pack;
        }

        return byName;
    }
}

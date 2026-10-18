namespace wsl_squad.Core.Entities;

/// <summary>The pack catalogue and its dependency rules (reference <c>WslPackCatalog</c>).</summary>
public sealed class WslPackCatalog
{
    public IReadOnlyList<WslPack> Packs { get; set; } = [];

    public WslPack? GetPack(string name)
    {
        return Packs.FirstOrDefault(pack => pack.Name == name);
    }

    public IReadOnlyList<string> DependentsOf(string packName)
    {
        List<string> dependents = [];
        List<string> frontier = [packName];

        while (frontier.Count > 0)
        {
            List<string> next = [];

            foreach (WslPack pack in Packs)
            {
                string name = pack.Name;

                if (name == packName || dependents.Contains(name))
                {
                    continue;
                }

                if (pack.Requires.Any(frontier.Contains))
                {
                    dependents.Add(name);
                    next.Add(name);
                }
            }

            frontier = next;
        }

        return dependents;
    }

    public IReadOnlyList<WslPack> OfferedFor(string? family)
    {
        return
        [
            .. Packs.Where(pack =>
                pack.Offered &&
                (string.IsNullOrEmpty(family) || pack.Family == family)),
        ];
    }

    public IReadOnlyList<string> ResolveSelection(IReadOnlyList<string> names, IReadOnlyList<string> installed)
    {
        HashSet<string> seen = new(StringComparer.Ordinal);
        List<string> ordered = [];

        foreach (string name in names)
        {
            AddRequires(name, installed, seen, ordered);
        }

        return ordered;
    }

    public IReadOnlyList<string> ResolveRemoval(
        IReadOnlyList<string> installed,
        IReadOnlyList<string> leaving,
        IReadOnlyList<string> arriving)
    {
        List<string> gone = [.. leaving];

        int added;

        do
        {
            added = 0;
            List<string> remaining = [.. installed.Where(name => !gone.Contains(name)), .. arriving];

            foreach (string name in installed)
            {
                if (gone.Contains(name))
                {
                    continue;
                }

                WslPack? pack = GetPack(name);

                if (pack is null || pack.Offered)
                {
                    continue;
                }

                bool claimed = remaining.Exists(other => GetPack(other)?.Requires.Contains(name) == true);

                if (!claimed)
                {
                    gone.Add(name);
                    added++;
                }
            }
        }
        while (added > 0);

        return gone;
    }

    public IReadOnlyList<string> GetBlockers(
        string name,
        IReadOnlyList<string> installed,
        IReadOnlyList<string> leaving,
        IReadOnlyList<string> arriving)
    {
        List<string> standing = [.. installed.Where(item => !leaving.Contains(item) && item != name), .. arriving];

        return
        [
            .. standing
                .Where(other => GetPack(other)?.Requires.Contains(name) == true)
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal),
        ];
    }

    public IReadOnlyList<PackRemovalConflict> GetRemovalConflicts(
        IReadOnlyList<string> installed,
        IReadOnlyList<string> leaving,
        IReadOnlyList<string> arriving)
    {
        List<PackRemovalConflict> conflicts = [];

        foreach (string name in leaving)
        {
            IReadOnlyList<string> blockers = GetBlockers(name, installed, leaving, arriving);

            if (blockers.Count > 0)
            {
                conflicts.Add(new PackRemovalConflict(name, blockers));
            }
        }

        return conflicts;
    }

    private void AddRequires(
        string name,
        IReadOnlyList<string> installed,
        HashSet<string> seen,
        List<string> ordered)
    {
        if (!seen.Add(name))
        {
            return;
        }

        WslPack? pack = GetPack(name);

        if (pack is null)
        {
            return;
        }

        foreach (string need in pack.Requires)
        {
            AddRequires(need, installed, seen, ordered);
        }

        if (!installed.Contains(name))
        {
            ordered.Add(name);
        }
    }
}

/// <summary>A pack that cannot be removed because another pack requires it.</summary>
public sealed record PackRemovalConflict(string Name, IReadOnlyList<string> Blockers);

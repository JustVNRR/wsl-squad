namespace WslSquad.Core.Entities;

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
}

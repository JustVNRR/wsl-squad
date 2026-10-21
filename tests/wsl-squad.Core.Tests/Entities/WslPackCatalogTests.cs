using FluentAssertions;

using wsl_squad.Core.Entities;

namespace wsl_squad.Core.Tests.Entities;

public class WslPackCatalogTests
{
    private static WslPackCatalog Catalog()
    {
        WslPack a = new() { Name = "a" };
        WslPack b = new() { Name = "b", Requires = ["a"] };
        WslPack c = new() { Name = "c", Requires = ["b"] };

        return new WslPackCatalog { Packs = [a, b, c] };
    }

    [Fact]
    public void Finds_transitive_dependents()
    {
        IReadOnlyList<string> dependents = Catalog().DependentsOf("a");

        dependents.Should().BeEquivalentTo("b", "c");
    }

}

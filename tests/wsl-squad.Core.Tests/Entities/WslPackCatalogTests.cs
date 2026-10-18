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

    [Fact]
    public void Selection_closes_over_requirements_in_install_order()
    {
        IReadOnlyList<string> selected = Catalog().ResolveSelection(["c"], []);

        selected.Should().Equal("a", "b", "c");
    }

    [Fact]
    public void Selection_skips_installed_packs()
    {
        IReadOnlyList<string> selected = Catalog().ResolveSelection(["c"], ["a"]);

        selected.Should().Equal("b", "c");
    }

    [Fact]
    public void Removal_claims_unoffered_orphans_only()
    {
        WslPackCatalog catalog = new()
        {
            Packs =
            [
                new WslPack { Name = "base", Offered = false },
                new WslPack { Name = "child", Offered = false, Requires = ["base"] },
            ],
        };

        IReadOnlyList<string> removed = catalog.ResolveRemoval(["base", "child"], ["child"], []);

        removed.Should().Equal("child", "base");
    }

    [Fact]
    public void Reports_the_pack_blocking_a_removal()
    {
        IReadOnlyList<PackRemovalConflict> conflicts = Catalog().GetRemovalConflicts(["a", "b"], ["a"], []);

        conflicts.Should().ContainSingle();
        conflicts[0].Name.Should().Be("a");
        conflicts[0].Blockers.Should().Equal("b");
    }

    [Fact]
    public void Offered_for_filters_on_family()
    {
        WslPackCatalog catalog = new()
        {
            Packs =
            [
                new WslPack { Name = "one", Family = "debian" },
                new WslPack { Name = "two", Family = "arch" },
                new WslPack { Name = "three", Family = "debian", Offered = false },
            ],
        };

        catalog.OfferedFor("debian").Select(pack => pack.Name).Should().Equal("one");
    }
}

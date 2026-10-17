using FluentAssertions;

using wsl_squad.Core.Application.Packs;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Packs;

namespace wsl_squad.Tests.Core.Packs;

public class PackDependencyResolverTests
{
    private static IReadOnlyList<WslPack> CreateCatalog()
    {
        return
        [
            new WslPack("shell", "/packs/shell", "Shell", "debian", [], true, ""),
            new WslPack("ops", "/packs/ops", "Ops", "debian", [], false, ""),
            new WslPack("scaff", "/packs/scaff", "Scaffold", "debian", [], false, ""),
            new WslPack("py", "/packs/py", "Python", "debian", ["shell", "ops", "scaff"], true, ""),
            new WslPack("web", "/packs/web", "Web", "debian", ["shell"], true, ""),
        ];
    }

    [Fact]
    public void Installs_dependencies_before_dependents()
    {
        OperationResult<IReadOnlyList<WslPack>> result =
            PackDependencyResolver.ResolveInstall(CreateCatalog(), ["py"]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(pack => pack.Name).Should().Equal("shell", "ops", "scaff", "py");
    }

    [Fact]
    public void Rejects_an_unknown_pack()
    {
        OperationResult<IReadOnlyList<WslPack>> result =
            PackDependencyResolver.ResolveInstall(CreateCatalog(), ["missing"]);

        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    [Fact]
    public void Rejects_a_missing_dependency()
    {
        IReadOnlyList<WslPack> catalog =
        [
            new WslPack("py", "/packs/py", "Python", "debian", ["missing"], true, ""),
        ];

        OperationResult<IReadOnlyList<WslPack>> result =
            PackDependencyResolver.ResolveInstall(catalog, ["py"]);

        result.Error!.Code.Should().Be(ErrorCode.NotFound);
        result.Error.Message.Should().Contain("missing");
    }

    [Fact]
    public void Detects_dependency_cycles()
    {
        IReadOnlyList<WslPack> catalog =
        [
            new WslPack("a", "/packs/a", "A", "debian", ["b"], true, ""),
            new WslPack("b", "/packs/b", "B", "debian", ["a"], true, ""),
        ];

        OperationResult<IReadOnlyList<WslPack>> result =
            PackDependencyResolver.ResolveInstall(catalog, ["a"]);

        result.Error!.Code.Should().Be(ErrorCode.Validation);
    }

    [Fact]
    public void Removal_includes_hidden_packs_that_become_unused()
    {
        OperationResult<PackRemovalPlan> result = PackDependencyResolver.ResolveRemoval(
            CreateCatalog(),
            ["shell", "ops", "scaff", "py"],
            ["py"]);

        result.IsSuccess.Should().BeTrue();
        result.Value.RemovalOrder.Select(pack => pack.Name)
            .Should().BeEquivalentTo("py", "ops", "scaff");
        result.Value.RemovalOrder[0].Name.Should().Be("py");
        result.Value.Blockers.Should().BeEmpty();
    }

    [Fact]
    public void Removal_is_blocked_by_a_remaining_dependent()
    {
        OperationResult<PackRemovalPlan> result = PackDependencyResolver.ResolveRemoval(
            CreateCatalog(),
            ["shell", "py"],
            ["shell"]);

        result.Error!.Code.Should().Be(ErrorCode.PackConflict);
        result.Error.Detail.Should().Contain("py");
    }

    [Fact]
    public void Removal_rejects_a_pack_that_is_not_installed()
    {
        OperationResult<PackRemovalPlan> result = PackDependencyResolver.ResolveRemoval(
            CreateCatalog(),
            ["shell"],
            ["py"]);

        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    [Fact]
    public void Matches_pack_families_against_distribution_tokens()
    {
        WslPack pack = CreateCatalog()[0];

        PackDependencyResolver.MatchesFamily(pack, ["ubuntu", "debian"]).Should().BeTrue();
        PackDependencyResolver.MatchesFamily(pack, ["fedora"]).Should().BeFalse();
    }
}

using FluentAssertions;

using WslSquad.Core.Tests.Support;
using WslSquad.Core.Validation;

namespace WslSquad.Core.Tests.Validation;

public class FleetTargetRulesTests
{
    [Fact]
    public void Rejects_missing_relative_and_occupied_targets()
    {
        using TemporaryDirectory temp = new();
        Directory.CreateDirectory(temp.Combine("archives"));

        FleetTargetRules.Test(null).Should().Be(FleetTargetProblem.Empty);
        FleetTargetRules.Test("relative").Should().Be(FleetTargetProblem.NotAbsolutePath);
        FleetTargetRules.Test(temp.Path).Should().Be(FleetTargetProblem.ArchivesFolderAlreadyExists);
    }

    [Fact]
    public void Accepts_an_absolute_empty_target()
    {
        using TemporaryDirectory temp = new();

        FleetTargetRules.Test(temp.Combine("fleet")).Should().Be(FleetTargetProblem.None);
    }
}

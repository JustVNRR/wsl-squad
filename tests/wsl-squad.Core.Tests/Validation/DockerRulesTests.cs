using FluentAssertions;

using WslSquad.Core.Dtos;
using WslSquad.Core.Validation;

namespace WslSquad.Core.Tests.Validation;

public class DockerRulesTests
{
    [Theory]
    [InlineData("yes", true, DockerTargetStatus.AlreadyRegistered)]
    [InlineData("yes", false, DockerTargetStatus.Eligible)]
    [InlineData("no", true, DockerTargetStatus.Eligible)]
    [InlineData("no", false, DockerTargetStatus.NotRegistered)]
    [InlineData("unknown", true, DockerTargetStatus.Unknown)]
    [InlineData("", false, DockerTargetStatus.Unknown)]
    public void The_docker_state_decides_the_available_action(
        string state,
        bool register,
        DockerTargetStatus expected)
    {
        DockerRules.Status(state, register).Should().Be(expected);
    }

    [Theory]
    [InlineData("no", true, true)]
    [InlineData("yes", false, true)]
    [InlineData("yes", true, false)]
    [InlineData("no", false, false)]
    public void A_candidate_matches_the_requested_action(string state, bool register, bool expected)
    {
        WslIndexDto row = new() { Name = "alpha", Docker = state };

        DockerRules.IsCandidate(row, register).Should().Be(expected);
    }

    [Fact]
    public void An_archive_is_never_a_candidate()
    {
        WslIndexDto row = new() { Name = "alpha_archive", IsArchive = true, Docker = "no" };

        DockerRules.IsCandidate(row, register: true).Should().BeFalse();
    }
}

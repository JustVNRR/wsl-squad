using FluentAssertions;

using WslSquad.Core.Validation;

namespace WslSquad.Core.Tests.Validation;

public class NameRulesTests
{
    [Theory]
    [InlineData("alpha", true)]
    [InlineData("1alpha", true)]
    [InlineData("a.b_c-d", true)]
    [InlineData(".alpha", false)]
    [InlineData("alpha space", false)]
    [InlineData("", false)]
    public void Instance_names_follow_the_allowed_shape(string value, bool expected)
    {
        NameRules.IsValid(value).Should().Be(expected);
    }

    [Theory]
    [InlineData(null, NameProblem.Empty)]
    [InlineData("", NameProblem.Empty)]
    [InlineData("   ", NameProblem.Empty)]
    [InlineData(".alpha", NameProblem.InvalidFormat)]
    [InlineData("alpha space", NameProblem.InvalidFormat)]
    [InlineData("alpha", NameProblem.None)]
    public void Instance_name_validation_reports_the_reason(string? name, NameProblem expected)
    {
        NameRules.ValidateInstanceName(name, []).Should().Be(expected);
    }

    [Fact]
    public void A_taken_instance_name_is_reported_case_insensitively()
    {
        NameRules.ValidateInstanceName("Alpha", ["alpha"]).Should().Be(NameProblem.AlreadyTaken);
    }

    [Theory]
    [InlineData("alpha", true)]
    [InlineData("a_b-c1", true)]
    [InlineData("1alpha", false)]
    [InlineData("Alpha", false)]
    [InlineData("", false)]
    public void User_names_follow_the_allowed_shape(string value, bool expected)
    {
        NameRules.IsValidUser(value).Should().Be(expected);
    }

    [Fact]
    public void User_names_stop_at_thirty_two_characters()
    {
        NameRules.IsValidUser(new string('a', 32)).Should().BeTrue();
        NameRules.IsValidUser(new string('a', 33)).Should().BeFalse();
    }

    [Theory]
    [InlineData("Jean-Sébastien", "jean-sebastien")]
    [InlineData("123_Admin", "admin")]
    [InlineData("Élodie Dupond", "elodie-dupond")]
    [InlineData("01_Jean Éric", "jean-eric")]
    [InlineData("!!!", "")]
    public void A_user_name_is_proposed_from_the_windows_name(string raw, string expected)
    {
        NameRules.ProposeUserName(raw).Should().Be(expected);
    }

    [Fact]
    public void A_proposed_user_name_is_truncated_to_the_limit()
    {
        string proposed = NameRules.ProposeUserName(new string('a', 40));

        proposed.Should().HaveLength(32);
        NameRules.IsValidUser(proposed).Should().BeTrue();
    }
}

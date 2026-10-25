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

}

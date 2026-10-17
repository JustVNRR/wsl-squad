using FluentAssertions;

using wsl_squad.Core.Domain.Fleet;

namespace wsl_squad.Tests.Core.Fleet;

public class InstanceNameTests
{
    [Theory]
    [InlineData("demo")]
    [InlineData("Demo-1")]
    [InlineData("a.b_c")]
    [InlineData("0abc")]
    public void Accepts_valid_names(string name)
    {
        InstanceName.IsValid(name).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("-abc")]
    [InlineData(".abc")]
    [InlineData("_abc")]
    [InlineData("a b")]
    [InlineData("a/b")]
    public void Rejects_invalid_names(string? name)
    {
        InstanceName.IsValid(name).Should().BeFalse();
    }
}

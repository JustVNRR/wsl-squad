using FluentAssertions;

using WslSquad.Core.Entities;

namespace WslSquad.Core.Tests.Entities;

public class WslInstanceTests
{
    [Theory]
    [InlineData("alpha")]
    [InlineData("alpha-2")]
    [InlineData("a.b_c")]
    [InlineData("0abc")]
    public void Accepts_reference_names(string name)
    {
        WslInstance instance = new() { Name = name };

        instance.Name.Should().Be(name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("-alpha")]
    [InlineData("alpha copy")]
    [InlineData("alpha/copy")]
    public void Rejects_invalid_names(string name)
    {
        Action act = () => _ = new WslInstance { Name = name };

        act.Should().Throw<ArgumentException>();
    }
}

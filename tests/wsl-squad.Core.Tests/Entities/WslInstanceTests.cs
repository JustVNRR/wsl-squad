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
        instance.DefaultUser.Should().Be("root");
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

    [Fact]
    public void ToString_lists_the_reference_fields()
    {
        WslInstance instance = new()
        {
            Name = "alpha",
            DefaultUser = "alice",
            Packs = [new WslPack { Name = "tools" }],
        };

        string text = instance.ToString();

        text.Should().Contain("Distribution Name : alpha");
        text.Should().Contain("Default User      : alice");
        text.Should().Contain("Packs             : tools");
    }
}

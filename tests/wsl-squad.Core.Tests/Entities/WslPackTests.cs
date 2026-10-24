using FluentAssertions;

using WslSquad.Core.Entities;

namespace WslSquad.Core.Tests.Entities;

public class WslPackTests
{
    [Fact]
    public void Defaults_follow_the_reference()
    {
        WslPack pack = new() { Name = "tools" };

        pack.Family.Should().Be("debian");
        pack.Offered.Should().BeTrue();
        pack.Requires.Should().BeEmpty();
    }

    [Theory]
    [InlineData("tools")]
    [InlineData("_template")]
    [InlineData("oh-my")]
    public void Accepts_reference_pack_names(string name)
    {
        WslPack pack = new() { Name = name };

        pack.Name.Should().Be(name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("__template")]
    [InlineData("-tools")]
    public void Rejects_invalid_pack_names(string name)
    {
        Action act = () => _ = new WslPack { Name = name };

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Validates_family_and_requires()
    {
        Action family = () => _ = new WslPack { Name = "tools", Family = "Debian" };
        Action requires = () => _ = new WslPack { Name = "tools", Requires = ["bad name"] };

        family.Should().Throw<ArgumentException>();
        requires.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void With_methods_only_replace_given_values()
    {
        WslPack pack = new()
        {
            Name = "tools",
            Description = "Tools",
            Family = "debian",
        };

        _ = pack.WithName("other").WithDescription("").WithFamily("");

        pack.Name.Should().Be("other");
        pack.Description.Should().Be("Tools");
        pack.Family.Should().Be("debian");
    }
}

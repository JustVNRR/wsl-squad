using FluentAssertions;

using wsl_squad.Core.Infrastructure.Docker;

namespace wsl_squad.Tests.Core.Docker;

public class DockerTagTests
{
    [Theory]
    [InlineData("demo", "demo")]
    [InlineData("Demo-1", "demo-1")]
    [InlineData("Demo 1", "demo-1")]
    [InlineData("_demo_", "demo")]
    [InlineData("...", "instance")]
    public void Slugifies_instance_names(string name, string expected)
    {
        DockerTag.Slugify(name).Should().Be(expected);
    }

    [Fact]
    public void Rejects_a_blank_name()
    {
        Action act = () => DockerTag.Slugify(" ");

        act.Should().Throw<ArgumentException>();
    }
}

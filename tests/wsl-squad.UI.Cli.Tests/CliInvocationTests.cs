using FluentAssertions;

namespace WslSquad.UI.Cli.Tests;

public class CliInvocationTests
{
    [Fact]
    public void Parses_an_interactive_build()
    {
        CliInvocation invocation = CliInvocation.Parse(["build"]);

        invocation.Command.Should().Be("build");
        invocation.Name.Should().BeNull();
    }

    [Fact]
    public void Keeps_the_positional_name_for_other_commands()
    {
        CliInvocation invocation = CliInvocation.Parse(["shell", "alpha"]);

        invocation.Command.Should().Be("shell");
        invocation.Name.Should().Be("alpha");
    }
}

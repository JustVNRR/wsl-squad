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
        invocation.BuildRequest.Should().BeNull();
    }

    [Fact]
    public void Parses_a_build_request()
    {
        CliInvocation invocation = CliInvocation.Parse(["build", "--request", "/tmp/build.json"]);

        invocation.Command.Should().Be("build");
        invocation.Name.Should().BeNull();
        invocation.BuildRequest.Should().Be("/tmp/build.json");
    }

    [Fact]
    public void Refuses_a_request_without_a_path()
    {
        Action act = () => CliInvocation.Parse(["build", "--request"]);

        act.Should().Throw<ArgumentException>().WithMessage("*path*");
    }

    [Fact]
    public void Refuses_a_request_on_another_command()
    {
        Action act = () => CliInvocation.Parse(["shell", "alpha", "--request", "/tmp/build.json"]);

        act.Should().Throw<ArgumentException>().WithMessage("*build*");
    }

    [Fact]
    public void Keeps_the_positional_name_for_other_commands()
    {
        CliInvocation invocation = CliInvocation.Parse(["shell", "alpha"]);

        invocation.Command.Should().Be("shell");
        invocation.Name.Should().Be("alpha");
        invocation.BuildRequest.Should().BeNull();
    }
}

using FluentAssertions;

using wsl_squad.Core.Abstractions.Process;

namespace wsl_squad.Tests.Core.Process;

public class ProcessInvocationTests
{
    [Fact]
    public void Preserves_argument_order()
    {
        ProcessInvocation invocation = new("docker", ["build", "-t", "wsl-squad/demo:1", "."]);

        invocation.Arguments.Should().Equal("build", "-t", "wsl-squad/demo:1", ".");
    }

    [Fact]
    public void Copies_arguments_and_environment()
    {
        List<string> arguments = ["a"];
        Dictionary<string, string> environment = new() { ["KEY"] = "value" };
        ProcessInvocation invocation = new("tool", arguments, environment: environment);

        arguments.Add("b");
        environment["KEY"] = "changed";

        invocation.Arguments.Should().Equal("a");
        invocation.Environment["KEY"].Should().Be("value");
    }

    [Fact]
    public void Rejects_a_blank_file_name()
    {
        Action act = () => _ = new ProcessInvocation(" ");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Defaults_to_no_arguments_environment_or_working_directory()
    {
        ProcessInvocation invocation = new("tool");

        invocation.Arguments.Should().BeEmpty();
        invocation.Environment.Should().BeEmpty();
        invocation.WorkingDirectory.Should().BeNull();
    }
}

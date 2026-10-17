using FluentAssertions;

using wsl_squad.Core.Abstractions.Process;
using wsl_squad.Core.Infrastructure.Windows;

namespace wsl_squad.Tests.Core.Windows;

public class TerminalCommandBuilderTests
{
    [Fact]
    public void Profile_targets_the_windows_terminal_profile()
    {
        ProcessInvocation invocation = TerminalCommandBuilder.Profile("demo");

        invocation.FileName.Should().Be("wt.exe");
        invocation.Arguments.Should().Equal("-p", "demo");
    }

    [Fact]
    public void Shell_falls_back_to_wsl_with_utf8_output()
    {
        ProcessInvocation invocation = TerminalCommandBuilder.Shell("demo");

        invocation.FileName.Should().Be("wsl.exe");
        invocation.Arguments.Should().Equal("-d", "demo", "--cd", "~");
        invocation.Environment["WSL_UTF8"].Should().Be("1");
    }
}

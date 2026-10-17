using FluentAssertions;

using wsl_squad.Core.Abstractions.Process;
using wsl_squad.Core.Infrastructure.Windows;

namespace wsl_squad.Tests.Core.Windows;

public class WslCommandBuilderTests
{
    [Fact]
    public void List_running_uses_exact_arguments_and_utf8_output()
    {
        ProcessInvocation invocation = WslCommandBuilder.ListRunning();

        invocation.FileName.Should().Be("wsl.exe");
        invocation.Arguments.Should().Equal("--list", "--running", "--quiet");
        invocation.Environment.Should().ContainKey("WSL_UTF8").WhoseValue.Should().Be("1");
        invocation.WorkingDirectory.Should().BeNull();
    }
}

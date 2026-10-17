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

    [Fact]
    public void Import_uses_version_two()
    {
        WslCommandBuilder.Import("demo", "C:\\fleet\\demo", "C:\\staging\\rootfs.tar").Arguments
            .Should().Equal("--import", "demo", "C:\\fleet\\demo", "C:\\staging\\rootfs.tar", "--version", "2");
    }

    [Fact]
    public void Unregister_and_terminate_target_the_distribution()
    {
        WslCommandBuilder.Unregister("demo").Arguments.Should().Equal("--unregister", "demo");
        WslCommandBuilder.Terminate("demo").Arguments.Should().Equal("--terminate", "demo");
    }

    [Fact]
    public void Copy_file_runs_cp_as_root_in_the_distribution()
    {
        WslCommandBuilder.CopyFile("demo", "/mnt/c/onboarding.sh", "/root/onboarding.sh").Arguments
            .Should().Equal("-d", "demo", "-u", "root", "--", "cp", "/mnt/c/onboarding.sh", "/root/onboarding.sh");
    }

    [Fact]
    public void Onboarding_runs_bash_with_lang_c_and_optional_user()
    {
        WslCommandBuilder.RunOnboarding("demo", "/root/onboarding.sh", "jane").Arguments
            .Should().Equal(
                "-d", "demo", "-u", "root", "--", "env", "LANG=C", "/bin/bash", "/root/onboarding.sh", "jane");

        WslCommandBuilder.RunOnboarding("demo", "/root/onboarding.sh", null).Arguments
            .Should().Equal("-d", "demo", "-u", "root", "--", "env", "LANG=C", "/bin/bash", "/root/onboarding.sh");
    }

    [Fact]
    public void Export_and_compact_use_expected_arguments()
    {
        WslCommandBuilder.Export("demo", "C:\\tar\\demo.tar.gz", "tar.gz").Arguments
            .Should().Equal("--export", "demo", "C:\\tar\\demo.tar.gz", "--format", "tar.gz");

        WslCommandBuilder.Compact("demo").Arguments
            .Should().Equal("--manage", "demo", "--compact");
    }

    [Fact]
    public void Start_boots_the_distribution_without_a_shell()
    {
        WslCommandBuilder.Start("demo").Arguments
            .Should().Equal("-d", "demo", "--exec", "/bin/true");
    }
}

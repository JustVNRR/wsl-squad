using FluentAssertions;

using wsl_squad.Core.Abstractions.Process;
using wsl_squad.Core.Infrastructure.Windows;

namespace wsl_squad.Tests.Core.Windows;

public class PackCommandBuilderTests
{
    [Fact]
    public void InstalledPacks_finds_pack_conf_directories()
    {
        ProcessInvocation invocation = PackCommandBuilder.InstalledPacks("demo");

        invocation.Arguments.Take(5).Should().Equal("-d", "demo", "--", "bash", "-lc");
        invocation.Arguments[5].Should().Contain("find").And.Contain("pack.conf");
        invocation.Environment["WSL_UTF8"].Should().Be("1");
    }

    [Fact]
    public void OsRelease_and_user_home_read_the_distribution()
    {
        PackCommandBuilder.OsRelease("demo").Arguments
            .Should().Equal("-d", "demo", "--", "cat", "/etc/os-release");

        PackCommandBuilder.UserHome("demo", "jane").Arguments
            .Should().Equal("-d", "demo", "-u", "jane", "--", "printenv", "HOME");
    }

    [Fact]
    public void CopyPack_copies_through_the_wsl_mount()
    {
        ProcessInvocation invocation = PackCommandBuilder.CopyPack(
            "demo",
            "jane",
            "/mnt/c/catalog/packs/shell",
            "shell");

        invocation.Arguments.Take(6).Should().Equal("-d", "demo", "-u", "jane", "--", "bash");
        string script = invocation.Arguments[7];
        script.Should().Contain("mkdir -p \"$HOME/.config/packs/shell\"");
        script.Should().Contain("cp -r '/mnt/c/catalog/packs/shell'/.");
        script.Should().Contain("chmod +x");
    }

    [Fact]
    public void RunScript_sets_lang_home_and_arguments()
    {
        ProcessInvocation invocation = PackCommandBuilder.RunScript(
            "demo",
            "jane",
            "/home/jane",
            "/home/jane/.config/packs/shell/install.sh",
            ["arg1"]);

        invocation.Arguments.Should().Equal(
            "-d", "demo", "-u", "jane", "--", "env", "LANG=C", "HOME=/home/jane",
            "/bin/bash", "/home/jane/.config/packs/shell/install.sh", "arg1");
    }

    [Fact]
    public void Sudo_window_opens_and_closes()
    {
        ProcessInvocation open = PackCommandBuilder.OpenSudo("demo", "jane");
        open.Arguments.Take(6).Should().Equal("-d", "demo", "-u", "root", "--", "bash");
        open.Arguments[7].Should().Contain("jane ALL=(ALL) NOPASSWD:ALL");
        open.Arguments[7].Should().Contain(PackCommandBuilder.SudoersPath);

        PackCommandBuilder.CloseSudo("demo").Arguments
            .Should().Equal("-d", "demo", "-u", "root", "--", "rm", "-f", PackCommandBuilder.SudoersPath);
    }

    [Fact]
    public void Remove_and_cleanup_use_expected_arguments()
    {
        PackCommandBuilder.RemovePackFolder("demo", "/home/jane", "shell").Arguments
            .Should().Equal("-d", "demo", "-u", "root", "--", "rm", "-rf", "/home/jane/.config/packs/shell");

        PackCommandBuilder.CleanupOrphans("demo", "/mnt/c/catalog/packs/cleanup_orphans.sh").Arguments
            .Should().Equal(
                "-d", "demo", "-u", "root", "--cd", "/tmp", "--", "env", "LANG=C",
                "/bin/bash", "/mnt/c/catalog/packs/cleanup_orphans.sh");
    }

    [Fact]
    public void Quotes_shell_single_quotes()
    {
        PackCommandBuilder.Quote("O'Brien").Should().Be("'O'\\''Brien'");
    }
}

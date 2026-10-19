using FluentAssertions;

using wsl_squad.Core.Repositories;

namespace wsl_squad.Core.Tests.Repositories;

public class WslRepositoryTests
{
    [Fact]
    public void Reads_only_pack_conf_lines_from_find_output()
    {
        IReadOnlyList<string> lines =
        [
            "/home/alice/.config/packs/shell/pack.conf",
            "/home/alice/.config/packs/tools/pack.conf",
            "/home/alice/.config/packs/my pack/pack.conf",
            "/home/alice/.config/packs/notes.txt",
            "/home/alice/.config/packs/old/pack.conf.bak",
        ];

        IReadOnlyList<string> names = WslRepository.ParseInstalledPacks(lines);

        names.Should().Equal("my pack", "shell", "tools");
    }

    [Fact]
    public void Returns_no_pack_when_the_packs_folder_is_missing()
    {
        IReadOnlyList<string> lines =
        [
            "find: ‘/home/alice/.config/packs’: No such file or directory",
        ];

        WslRepository.ParseInstalledPacks(lines).Should().BeEmpty();
    }

    [Fact]
    public void Returns_no_pack_when_find_found_nothing()
    {
        WslRepository.ParseInstalledPacks([]).Should().BeEmpty();
    }

    [Fact]
    public void Quotes_single_quotes_in_a_shell_value()
    {
        WslRepository.ShellValue("a'b").Should().Be("'a'\"'\"'b'");
    }
}

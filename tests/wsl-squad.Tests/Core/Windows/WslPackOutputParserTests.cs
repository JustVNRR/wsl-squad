using FluentAssertions;

using wsl_squad.Core.Infrastructure.Windows;

namespace wsl_squad.Tests.Core.Windows;

public class WslPackOutputParserTests
{
    [Fact]
    public void Parses_installed_pack_directories_into_names()
    {
        const string output = "/home/jane/.config/packs/shell\n/home/jane/.config/packs/py\n";

        WslPackOutputParser.ParseInstalledPackNames(output).Should().Equal("py", "shell");
    }

    [Fact]
    public void Handles_windows_separators_and_nuls()
    {
        WslPackOutputParser.ParseInstalledPackNames("C:\\home\\.config\\packs\\shell\0\n")
            .Should().Equal("shell");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Returns_empty_for_empty_output(string? output)
    {
        WslPackOutputParser.ParseInstalledPackNames(output).Should().BeEmpty();
    }

    [Fact]
    public void Parses_id_and_id_like_tokens()
    {
        const string osRelease = "NAME=\"Ubuntu\"\nID=ubuntu\nID_LIKE=\"debian ubuntu\"\n";

        WslPackOutputParser.ParseFamilyTokens(osRelease).Should().Equal("ubuntu", "debian");
    }

    [Fact]
    public void Parses_unquoted_family_values()
    {
        WslPackOutputParser.ParseFamilyTokens("ID=debian\n").Should().Equal("debian");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Returns_no_tokens_for_empty_os_release(string? output)
    {
        WslPackOutputParser.ParseFamilyTokens(output).Should().BeEmpty();
    }
}

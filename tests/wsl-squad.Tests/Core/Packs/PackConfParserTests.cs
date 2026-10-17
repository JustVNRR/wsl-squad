using FluentAssertions;

using wsl_squad.Core.Domain.Packs;
using wsl_squad.Core.Infrastructure.Packs;

namespace wsl_squad.Tests.Core.Packs;

public class PackConfParserTests
{
    [Fact]
    public void Parses_all_supported_fields()
    {
        const string content = """
            # A comment
            PACK_DESCRIPTION := Shell tools
            PACK_FAMILY := debian
            PACK_REQUIRES := shell ops
            PACK_VISIBLE := no
            PACK_WELCOME := Welcome aboard
            """;

        PackConf conf = PackConfParser.Parse(content);

        conf.Description.Should().Be("Shell tools");
        conf.Family.Should().Be("debian");
        conf.Requires.Should().Equal("shell", "ops");
        conf.Visible.Should().BeFalse();
        conf.Welcome.Should().Be("Welcome aboard");
    }

    [Fact]
    public void Applies_defaults_for_missing_fields()
    {
        PackConf conf = PackConfParser.Parse(string.Empty);

        conf.Family.Should().Be("debian");
        conf.Visible.Should().BeTrue();
        conf.Requires.Should().BeEmpty();
        conf.Description.Should().BeEmpty();
    }

    [Fact]
    public void Strips_trailing_comments()
    {
        PackConf conf = PackConfParser.Parse("PACK_DESCRIPTION := hello # ignored\n");

        conf.Description.Should().Be("hello");
    }

    [Fact]
    public void Supports_plain_assignment_syntax()
    {
        PackConf conf = PackConfParser.Parse("PACK_DESCRIPTION = hello\nPACK_REQUIRES = a   b\n");

        conf.Description.Should().Be("hello");
        conf.Requires.Should().Equal("a", "b");
    }

    [Fact]
    public void Ignores_unknown_keys_and_blank_lines()
    {
        PackConf conf = PackConfParser.Parse("OTHER := value\n\nPACK_DESCRIPTION := known\n");

        conf.Description.Should().Be("known");
    }
}

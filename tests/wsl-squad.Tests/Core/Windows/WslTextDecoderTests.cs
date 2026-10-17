using FluentAssertions;

using wsl_squad.Core.Infrastructure.Windows;

namespace wsl_squad.Tests.Core.Windows;

public class WslTextDecoderTests
{
    [Fact]
    public void Parses_plain_utf8_lines()
    {
        WslTextDecoder.ParseDistributionList("Ubuntu\r\nDebian\r\n")
            .Should()
            .Equal("Ubuntu", "Debian");
    }

    [Fact]
    public void Strips_nul_characters_and_bom()
    {
        WslTextDecoder.ParseDistributionList("﻿U\0b\0u\0n\0t\0u\0\r\0\n\0D\0e\0b\0i\0a\0n\0")
            .Should()
            .Equal("Ubuntu", "Debian");
    }

    [Fact]
    public void Ignores_blank_lines_and_whitespace()
    {
        WslTextDecoder.ParseDistributionList("  Ubuntu  \n\n\t\n Debian \n")
            .Should()
            .Equal("Ubuntu", "Debian");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Returns_empty_for_empty_output(string? output)
    {
        WslTextDecoder.ParseDistributionList(output).Should().BeEmpty();
    }
}

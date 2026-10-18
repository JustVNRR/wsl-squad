using FluentAssertions;

using wsl_squad.Core.Entities;

namespace wsl_squad.Core.Tests.Entities;

public class WslThemeTests
{
    [Fact]
    public void Default_uses_the_reference_look()
    {
        WslTheme theme = WslTheme.Default("alpha");

        theme.FontName.Should().Be("MesloLGS NF");
        theme.ColorScheme.Should().Be("One Half Dark");
        theme.TabTitle.Should().Be("alpha");
    }

    [Fact]
    public void Valid_icon_colours_are_kept()
    {
        WslTheme theme = new();

        theme.SetIcon(new IconDrawing("AL", "#112233", "#445566", "#FFFFFF"));

        theme.Icon.Should().Be(new IconDrawing("AL", "#112233", "#445566", "#FFFFFF"));
    }

    [Theory]
    [InlineData("red")]
    [InlineData("#12345")]
    [InlineData("#GGGGGG")]
    public void Invalid_icon_colours_are_rejected(string colour)
    {
        WslTheme theme = new();

        Action act = () => theme.SetIcon(new IconDrawing("AL", colour, null, null));

        act.Should().Throw<ArgumentException>();
    }
}

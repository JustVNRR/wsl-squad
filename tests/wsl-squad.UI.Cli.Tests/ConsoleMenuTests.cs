using FluentAssertions;

using WslSquad.UI.Cli.Interaction;

namespace WslSquad.UI.Cli.Tests;

public class ConsoleMenuTests
{
    [Fact]
    public void Formats_the_current_row_with_a_marker()
    {
        string row = ConsoleMenu.FormatRow(1, 1, ["first", "second"], null);

        row.Should().Be("  > second");
    }

    [Fact]
    public void Formats_a_checkbox_row()
    {
        string row = ConsoleMenu.FormatRow(0, 1, ["first"], [true]);

        row.Should().Be("    [x] first");
    }

    [Fact]
    public void Fits_labels_to_the_console_width()
    {
        IReadOnlyList<string> labels = ConsoleMenu.Fit(["a very long label", "short"], 12, 0);

        labels[0].Should().Be("a very l...");
        labels[1].Should().Be("short");
    }

    [Fact]
    public void Menu_items_expose_their_value()
    {
        object value = new();
        ConsoleMenuItem item = new("label", value);

        item.Text.Should().Be("label");
        item.Value.Should().BeSameAs(value);
    }

    [Fact]
    public void Theme_maps_semantic_tones()
    {
        ConsoleTheme theme = new();

        theme.GetColor(ConsoleTone.Error).Should().Be(ConsoleColor.Red);
        theme.GetColor(ConsoleTone.Success).Should().Be(ConsoleColor.Green);
    }
}

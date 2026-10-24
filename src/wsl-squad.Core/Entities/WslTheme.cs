namespace WslSquad.Core.Entities;

public sealed class WslTheme
{
    public string? IconPath { get; set; }

    public string? ColorScheme { get; set; }

    public string? FontName { get; set; }

    public string? TabTitle { get; set; }

    public IconDrawing? Icon { get; private set; }

    public void SetIcon(IconDrawing? drawing)
    {
        if (drawing is null)
        {
            Icon = null;

            return;
        }

        foreach (string? colour in new[] { drawing.Top, drawing.Bottom, drawing.TextColor })
        {
            if (!string.IsNullOrEmpty(colour) && !IsColour(colour))
            {
                throw new ArgumentException($"'{colour}' is not a colour (#RRGGBB).", nameof(drawing));
            }
        }

        Icon = drawing;
    }

    public static WslTheme Default(string? tabTitle)
    {
        return new WslTheme
        {
            ColorScheme = "One Half Dark",
            FontName = "MesloLGS NF",
            TabTitle = tabTitle,
        };
    }

    public override string ToString()
    {
        return $"font '{FontName}', colours '{ColorScheme}'";
    }

    private static bool IsColour(string value)
    {
        return value.Length == 7 && value[0] == '#' && value[1..].All(char.IsAsciiHexDigit);
    }
}

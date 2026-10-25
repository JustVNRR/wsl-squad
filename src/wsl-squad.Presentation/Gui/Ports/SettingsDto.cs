namespace WslSquad.Presentation.Gui.Ports;

public sealed record SettingsAppearanceDto
{
    public string? Font { get; init; }

    public int FontSize { get; init; }

    public string? ColourScheme { get; init; }

    public string? Variant { get; init; }
}

public sealed record SettingsDto(SettingsAppearanceDto Appearance, string? NewRoot);

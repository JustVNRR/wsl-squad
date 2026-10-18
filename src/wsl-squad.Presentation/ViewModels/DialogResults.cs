namespace wsl_squad.Presentation.ViewModels;

public sealed record DockerDialogResult(string Name, bool Restart);

public sealed record PackDuplicateResult(string Name, string Description);

public sealed record SettingsOutcome(
    string FontFamily,
    int FontSize,
    string ColourSet,
    string Variant,
    string? NewRoot);

public sealed record ExportThemeDto(string? Source, string? Target);

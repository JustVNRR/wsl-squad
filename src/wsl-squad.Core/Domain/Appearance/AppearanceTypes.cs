using wsl_squad.Core.Domain.Fleet;

namespace wsl_squad.Core.Domain.Appearance;

/// <summary>A Windows Terminal colour scheme.</summary>
public sealed record WslColorScheme(
    string Name,
    string Background,
    string Foreground,
    IReadOnlyList<string> Colors);

/// <summary>Settings written into a Windows Terminal fragment.</summary>
public sealed record TerminalFragmentSettings(
    string ProfileGuid,
    string? IconPath,
    string? Font,
    string? ColorScheme,
    string? TabTitle);

/// <summary>Requested appearance changes; null means unchanged.</summary>
public sealed record AppearanceRequest(
    string? Font = null,
    string? ColorScheme = null,
    string? TabTitle = null,
    string? IconText = null,
    string? IconTop = null,
    string? IconBottom = null,
    string? IconTextColor = null,
    string? IconImage = null);

/// <summary>Current appearance of an instance.</summary>
public sealed record AppearanceOutcome(
    string InstanceName,
    string? Font,
    string? ColorScheme,
    string? TabTitle,
    InstanceIcon? Icon,
    bool HasProfile,
    IReadOnlyList<string> Warnings);

namespace wsl_squad.Core.Entities;

/// <summary>An icon's monogram and colours, as drawn by the icon tool.</summary>
public sealed record IconDrawing(
    string? Text,
    string? Top,
    string? Bottom,
    string? TextColor);

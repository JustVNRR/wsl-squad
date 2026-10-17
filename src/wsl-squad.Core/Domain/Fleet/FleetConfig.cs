namespace wsl_squad.Core.Domain.Fleet;

/// <summary>User configuration stored in <c>config.json</c>.</summary>
public sealed class FleetConfig
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public string FleetRoot { get; init; } = string.Empty;

    public string? CatalogRoot { get; init; }
}

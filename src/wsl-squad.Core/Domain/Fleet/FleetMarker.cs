namespace wsl_squad.Core.Domain.Fleet;

/// <summary>Ownership marker written in every managed instance folder.</summary>
public sealed class FleetMarker
{
    public const string FileName = ".wsl-squad";
    public const string LegacyFileName = ".wsl-fleet";
    public const int CurrentSchemaVersion = 1;
    public const string InstanceKind = "instance";

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public string Kind { get; init; } = InstanceKind;

    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public DateTimeOffset CreatedUtc { get; init; }

    public string CreatedBy { get; init; } = string.Empty;
}

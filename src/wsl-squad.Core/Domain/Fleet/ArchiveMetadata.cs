namespace wsl_squad.Core.Domain.Fleet;

/// <summary>Metadata stored in <c>archive.json</c> inside an archive folder.</summary>
public sealed class ArchiveMetadata
{
    public const string FileName = "archive.json";
    public const int CurrentSchemaVersion = 1;
    public const string DefaultFormat = "tar.gz";

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public string Name { get; set; } = string.Empty;

    public string SourceInstance { get; set; } = string.Empty;

    public DateTimeOffset CreatedUtc { get; set; }

    public string Format { get; set; } = DefaultFormat;

    public string File { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string Sha256 { get; set; } = string.Empty;

    public string DefaultUser { get; set; } = InstanceMetadata.DefaultUserName;

    public string DockerDesktop { get; set; } = InstanceMetadata.DockerUnknown;

    public InstanceAppearance? Appearance { get; set; }
}

using System.Text.Json;
using System.Text.Json.Serialization;

namespace wsl_squad.Core.Domain.Fleet;

/// <summary>Metadata stored in <c>instance.json</c> for a live or archived instance.</summary>
public sealed class InstanceMetadata
{
    public const string FileName = "instance.json";
    public const int CurrentSchemaVersion = 1;
    public const string DefaultUserName = "root";
    public const string DockerUnknown = "unknown";

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedUtc { get; set; }

    public DateTimeOffset UpdatedUtc { get; set; }

    public InstanceSource? Source { get; set; }

    public string DefaultUser { get; set; } = DefaultUserName;

    public string DockerDesktop { get; set; } = DockerUnknown;

    public InstanceAppearance? Appearance { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

/// <summary>Describes where an instance came from.</summary>
public sealed class InstanceSource
{
    public string Kind { get; set; } = string.Empty;

    public string Reference { get; set; } = string.Empty;

    public string? ImageTag { get; set; }
}

/// <summary>Terminal appearance settings of an instance.</summary>
public sealed class InstanceAppearance
{
    public string? Font { get; set; }

    public string? ColorScheme { get; set; }

    public string? TabTitle { get; set; }

    public InstanceIcon? Icon { get; set; }
}

/// <summary>Icon drawing settings of an instance.</summary>
public sealed class InstanceIcon
{
    public string Mode { get; set; } = "auto";

    public string? Text { get; set; }

    public string? Top { get; set; }

    public string? Bottom { get; set; }

    public string? TextColor { get; set; }

    public string? Image { get; set; }
}

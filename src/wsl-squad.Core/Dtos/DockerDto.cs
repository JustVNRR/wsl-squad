namespace wsl_squad.Core.Dtos;

/// <summary>A saved Docker image row.</summary>
public sealed record WslDockerImageDto
{
    public string? Name { get; init; }

    public string? Path { get; init; }

    public bool Uploaded { get; init; }
}

/// <summary>The registry probe's answer.</summary>
public sealed record IsRegistryImageValidResponseDto
{
    public bool Valid { get; init; }

    public string? Why { get; init; }
}

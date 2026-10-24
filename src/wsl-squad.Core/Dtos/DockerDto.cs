namespace WslSquad.Core.Dtos;

public sealed record WslDockerImageDto
{
    public string? Name { get; init; }

    public string? Path { get; init; }

    public bool Uploaded { get; init; }
}

public enum RegistryValidationError
{
    None,
    DockerUnavailable,
    NotFound,
    Rejected,
    Unreachable,
}

public sealed record IsRegistryImageValidResponseDto
{
    public bool Valid { get; init; }

    public RegistryValidationError Error { get; init; }

    public string? TechnicalDetail { get; init; }
}

namespace WslSquad.Core.Dtos;

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

namespace wsl_squad.Core.Dtos;

public sealed record WslPackDto
{
    public string? Name { get; init; }

    public string? Description { get; init; }

    public string? Family { get; init; }

    public List<string> Dependents { get; init; } = [];
}

public sealed record CreatePackDto
{
    public string? SourceName { get; init; }

    public string? NewName { get; init; }

    public string? Description { get; init; }
}

public sealed record SetPackDescriptionDto
{
    public string? PackName { get; init; }

    public string? Description { get; init; }
}

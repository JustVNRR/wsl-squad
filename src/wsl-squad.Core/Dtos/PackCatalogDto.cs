namespace wsl_squad.Core.Dtos;

/// <summary>A pack row shown by the pack dialogues (reference <c>WslPackDto</c>).</summary>
public sealed record WslPackDto
{
    public string? Name { get; init; }

    public string? Description { get; init; }

    public string? Family { get; init; }

    public List<string> Dependents { get; init; } = [];
}

/// <summary>Request to duplicate a pack under a new name.</summary>
public sealed record CreatePackDto
{
    public string? SourceName { get; init; }

    public string? NewName { get; init; }

    public string? Description { get; init; }
}

/// <summary>Request to rewrite a pack's description.</summary>
public sealed record SetPackDescriptionDto
{
    public string? PackName { get; init; }

    public string? Description { get; init; }
}

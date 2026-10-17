namespace wsl_squad.Core.Abstractions.Build;

/// <summary>Generates unique identifiers for builds and image tags.</summary>
public interface IBuildIdGenerator
{
    public string NextBuildId();
}

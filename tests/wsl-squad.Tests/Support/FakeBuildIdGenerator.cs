using wsl_squad.Core.Abstractions.Build;

namespace wsl_squad.Tests.Support;

public sealed class FakeBuildIdGenerator : IBuildIdGenerator
{
    public string BuildId { get; set; } = "20261017T120000Z-abcdef12";

    public string NextBuildId()
    {
        return BuildId;
    }
}

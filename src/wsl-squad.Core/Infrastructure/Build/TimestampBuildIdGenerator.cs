using System.Globalization;
using System.Security.Cryptography;

using wsl_squad.Core.Abstractions.Build;
using wsl_squad.Core.Abstractions.Time;

namespace wsl_squad.Core.Infrastructure.Build;

/// <summary>Generates sortable build identifiers: UTC timestamp plus random suffix.</summary>
public sealed class TimestampBuildIdGenerator : IBuildIdGenerator
{
    private readonly IClock _clock;

    public TimestampBuildIdGenerator(IClock clock)
    {
        _clock = clock;
    }

    public string NextBuildId()
    {
        string timestamp = _clock.UtcNow.UtcDateTime.ToString(
            "yyyyMMdd'T'HHmmss'Z'",
            CultureInfo.InvariantCulture);
        string suffix = RandomNumberGenerator.GetHexString(8).ToLowerInvariant();

        return $"{timestamp}-{suffix}";
    }
}

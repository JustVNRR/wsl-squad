using wsl_squad.Core.Abstractions.Time;

namespace wsl_squad.Core.Infrastructure.Time;

/// <summary>Clock backed by the system UTC time.</summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

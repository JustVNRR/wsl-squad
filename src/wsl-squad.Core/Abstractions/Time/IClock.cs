namespace wsl_squad.Core.Abstractions.Time;

/// <summary>Provides the current time so time-dependent behavior can be tested.</summary>
public interface IClock
{
    public DateTimeOffset UtcNow { get; }
}

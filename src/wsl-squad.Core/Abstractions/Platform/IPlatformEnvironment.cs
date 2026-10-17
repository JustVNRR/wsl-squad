namespace wsl_squad.Core.Abstractions.Platform;

/// <summary>Provides host environment values without touching process-wide state directly.</summary>
public interface IPlatformEnvironment
{
    public bool IsWindows { get; }

    public string LocalApplicationData { get; }

    public string UserProfile { get; }

    public string? GetEnvironmentVariable(string name);
}

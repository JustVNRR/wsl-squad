using wsl_squad.Core.Abstractions.Platform;

namespace wsl_squad.Core.Infrastructure.Environment;

/// <summary>Reads values from the current process environment.</summary>
public sealed class SystemPlatformEnvironment : IPlatformEnvironment
{
    public bool IsWindows => OperatingSystem.IsWindows();

    public string LocalApplicationData =>
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);

    public string UserProfile =>
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);

    public string? GetEnvironmentVariable(string name)
    {
        return System.Environment.GetEnvironmentVariable(name);
    }
}

using wsl_squad.Core.Abstractions.Platform;

namespace wsl_squad.Core.Infrastructure.Storage;

/// <summary>Resolves the wsl-squad configuration locations.</summary>
public static class AppPaths
{
    public const string ConfigDirectoryVariable = "WSL_SQUAD_CONFIG_DIR";
    public const string FleetRootVariable = "WSL_SQUAD_ROOT";
    public const string ApplicationDirectoryName = "wsl-squad";
    public const string ConfigFileName = "config.json";

    public static string GetConfigDirectory(IPlatformEnvironment platform)
    {
        string? overridden = platform.GetEnvironmentVariable(ConfigDirectoryVariable);

        return string.IsNullOrWhiteSpace(overridden)
            ? Path.Combine(platform.LocalApplicationData, ApplicationDirectoryName)
            : overridden;
    }

    public static string GetConfigFilePath(IPlatformEnvironment platform)
    {
        return Path.Combine(GetConfigDirectory(platform), ConfigFileName);
    }

    public static string GetDefaultFleetRoot(IPlatformEnvironment platform)
    {
        return Path.Combine(platform.UserProfile, "WSL", ApplicationDirectoryName);
    }

    public static string? GetFleetRootOverride(IPlatformEnvironment platform)
    {
        string? overridden = platform.GetEnvironmentVariable(FleetRootVariable);

        return string.IsNullOrWhiteSpace(overridden) ? null : overridden;
    }
}

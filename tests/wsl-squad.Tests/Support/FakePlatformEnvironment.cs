using wsl_squad.Core.Abstractions.Platform;

namespace wsl_squad.Tests.Support;

public sealed class FakePlatformEnvironment : IPlatformEnvironment
{
    private readonly Dictionary<string, string?> _variables = new(StringComparer.Ordinal);

    public bool IsWindows { get; set; } = true;

    public string LocalApplicationData { get; set; } = "/home/test/.local/share";

    public string RoamingApplicationData { get; set; } = "/home/test/.config";

    public string UserProfile { get; set; } = "/home/test";

    public void SetVariable(string name, string? value)
    {
        _variables[name] = value;
    }

    public string? GetEnvironmentVariable(string name)
    {
        return _variables.GetValueOrDefault(name);
    }
}

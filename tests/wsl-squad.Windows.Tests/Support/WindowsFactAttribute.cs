namespace wsl_squad.Windows.Tests.Support;

/// <summary>Marks a test that requires a real Windows host (WSL, Docker Desktop, registry).</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class WindowsFactAttribute : FactAttribute
{
    public WindowsFactAttribute()
    {
        if (!OperatingSystem.IsWindows())
        {
            Skip = "Windows integration tests run on the Windows host.";
        }
    }
}

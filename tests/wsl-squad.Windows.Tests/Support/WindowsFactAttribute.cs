namespace wsl_squad.Windows.Tests.Support;

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

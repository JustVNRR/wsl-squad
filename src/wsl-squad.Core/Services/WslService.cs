using WslSquad.Core.Infrastructure;

namespace WslSquad.Core.Services;

public static class WslService
{
    private const int _probeTimeoutMilliseconds = 5000;

    public static bool IsAvailable()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        return NativeCommand.Test(
                "wsl.exe",
                ["--status"],
                _probeTimeoutMilliseconds)
            || NativeCommand.Test(
                "wsl.exe",
                ["--version"],
                _probeTimeoutMilliseconds);
    }
}

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace WslSquad.UI.Cli.Interaction;

internal static class ConsoleMode
{
    private const int _standardOutputHandle = -11;
    private const uint _enableVirtualTerminalProcessing = 0x0004;

    public static bool EnterAlternateScreen()
    {
        if (!OperatingSystem.IsWindows() || !TryEnableVirtualTerminal())
        {
            return false;
        }

        try
        {
            Console.Write("\u001b[?1049h");
            Console.Out.Flush();

            return true;
        }
        catch (IOException ex)
        {
            Trace.TraceWarning($"[ConsoleMode] Could not enter the alternate screen: {ex.Message}");

            return false;
        }
    }

    public static void LeaveAlternateScreen()
    {
        try
        {
            Console.Write("\u001b[?1049l");
            Console.Out.Flush();
        }
        catch (IOException ex)
        {
            Trace.TraceWarning($"[ConsoleMode] Could not leave the alternate screen: {ex.Message}");
        }
    }

    [SupportedOSPlatform("windows")]
    private static bool TryEnableVirtualTerminal()
    {
        IntPtr handle = GetStdHandle(_standardOutputHandle);

        if (handle == IntPtr.Zero || handle == new IntPtr(-1))
        {
            return false;
        }

        if (!GetConsoleMode(handle, out uint mode))
        {
            return false;
        }

        return (mode & _enableVirtualTerminalProcessing) != 0 ||
               SetConsoleMode(handle, mode | _enableVirtualTerminalProcessing);
    }

#pragma warning disable SYSLIB1054
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetStdHandle(int handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetConsoleMode(IntPtr handle, out uint mode);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetConsoleMode(IntPtr handle, uint mode);
#pragma warning restore SYSLIB1054
}

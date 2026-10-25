using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace WslSquad.UI.Cli.Interaction;

internal static partial class ConsoleMode
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

    [LibraryImport("kernel32.dll", SetLastError = true)]
    private static partial IntPtr GetStdHandle(int handle);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetConsoleMode(IntPtr handle, out uint mode);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetConsoleMode(IntPtr handle, uint mode);
}

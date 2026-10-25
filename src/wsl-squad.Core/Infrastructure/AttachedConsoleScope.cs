using System.Runtime.InteropServices;

namespace WslSquad.Core.Infrastructure;

internal sealed partial class AttachedConsoleScope : IDisposable
{
    private readonly bool _allocated;
    private readonly TextWriter? _previousOutput;
    private readonly TextWriter? _previousError;
    private readonly StreamWriter? _output;
    private readonly StreamWriter? _error;

    public AttachedConsoleScope(bool interactive)
    {
        if (!interactive || !OperatingSystem.IsWindows() || GetConsoleWindow() != nint.Zero)
        {
            return;
        }

        _allocated = AllocConsole();

        if (!_allocated)
        {
            return;
        }

        _previousOutput = Console.Out;
        _previousError = Console.Error;
        _output = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
        _error = new StreamWriter(Console.OpenStandardError()) { AutoFlush = true };
        Console.SetOut(_output);
        Console.SetError(_error);
    }

    public void Dispose()
    {
        if (!_allocated)
        {
            return;
        }

        if (_previousOutput is not null)
        {
            Console.SetOut(_previousOutput);
        }

        if (_previousError is not null)
        {
            Console.SetError(_previousError);
        }

        _output?.Dispose();
        _error?.Dispose();
        FreeConsole();
    }

    [LibraryImport("kernel32.dll")]
    private static partial nint GetConsoleWindow();

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool AllocConsole();

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool FreeConsole();
}

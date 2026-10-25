using WslSquad.Core.Exceptions;
using System.Diagnostics;

namespace WslSquad.Core.Infrastructure;

internal static class NativeCommand
{
    public static (int ExitCode, string Output) Run(
        string program,
        IReadOnlyList<string> arguments,
        string? workingDirectory = null,
        bool includeError = true)
    {
        ProcessStartInfo start = new()
        {
            FileName = program,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        if (!string.IsNullOrEmpty(workingDirectory))
        {
            start.WorkingDirectory = workingDirectory;
        }

        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(start)
            ?? throw new WslSquadException(WslSquadErrorCode.ExternalToolFailed, program, "could not be started");

        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();

        process.WaitForExit();

        string standardOutput = output.GetAwaiter().GetResult();
        string standardError = error.GetAwaiter().GetResult();

        return (process.ExitCode, includeError ? standardOutput + standardError : standardOutput);
    }

    public static (int ExitCode, string Output) RunWithInput(
        string program,
        IReadOnlyList<string> arguments,
        string input,
        bool includeError = true)
    {
        ProcessStartInfo start = new()
        {
            FileName = program,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(start)
            ?? throw new WslSquadException(WslSquadErrorCode.ExternalToolFailed, program, "could not be started");

        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();

        process.StandardInput.Write(input);
        process.StandardInput.Close();
        process.WaitForExit();

        string standardOutput = output.GetAwaiter().GetResult();
        string standardError = error.GetAwaiter().GetResult();

        return (process.ExitCode, includeError ? standardOutput + standardError : standardOutput);
    }

    public static int RunAttached(
        string program,
        IReadOnlyList<string> arguments,
        string? workingDirectory = null)
    {
        ProcessStartInfo start = new()
        {
            FileName = program,
            UseShellExecute = false,
            CreateNoWindow = false,
        };

        if (!string.IsNullOrEmpty(workingDirectory))
        {
            start.WorkingDirectory = workingDirectory;
        }

        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(start)
            ?? throw new WslSquadException(WslSquadErrorCode.ExternalToolFailed, program, "could not be started");

        process.WaitForExit();

        return process.ExitCode;
    }

    public static (int ExitCode, string Output) RunInteractive(
        string program,
        IReadOnlyList<string> arguments,
        string? workingDirectory = null)
    {
        ProcessStartInfo start = new()
        {
            FileName = program,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = false,
        };

        if (!string.IsNullOrEmpty(workingDirectory))
        {
            start.WorkingDirectory = workingDirectory;
        }

        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(start)
            ?? throw new WslSquadException(WslSquadErrorCode.ExternalToolFailed, program, "could not be started");
        System.Text.StringBuilder captured = new();

        void Keep(string? line, bool error)
        {
            if (line is null)
            {
                return;
            }

            lock (captured)
            {
                captured.AppendLine(line);
            }

            if (error)
            {
                Console.Error.WriteLine(line);
            }
            else
            {
                Console.WriteLine(line);
            }
        }

        process.OutputDataReceived += (_, e) => Keep(e.Data, error: false);
        process.ErrorDataReceived += (_, e) => Keep(e.Data, error: true);
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        return (process.ExitCode, captured.ToString());
    }

    public static bool Test(
        string program,
        IReadOnlyList<string> arguments,
        int timeoutMilliseconds = 0)
    {
        try
        {
            if (timeoutMilliseconds <= 0)
            {
                return Run(program, arguments).ExitCode == 0;
            }

            return RunWithTimeout(program, arguments, timeoutMilliseconds) == 0;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }

    private static int RunWithTimeout(
        string program,
        IReadOnlyList<string> arguments,
        int timeoutMilliseconds)
    {
        ProcessStartInfo start = new()
        {
            FileName = program,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(start)
            ?? throw new WslSquadException(WslSquadErrorCode.ExternalToolFailed, program, "could not be started");

        Task<string> output = process.StandardOutput.ReadToEndAsync();
        Task<string> error = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(timeoutMilliseconds))
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                return -1;
            }

            process.WaitForExit(1000);

            return -1;
        }

        Task.WhenAll(output, error).GetAwaiter().GetResult();

        return process.ExitCode;
    }

    public static void Invoke(
        string program,
        IReadOnlyList<string> arguments,
        WslSquadErrorCode code,
        string? target = null,
        string? detail = null)
    {
        (int exitCode, _) = Run(program, arguments);

        if (exitCode != 0)
        {
            string technical = string.IsNullOrEmpty(detail)
                ? $"exit code {exitCode}"
                : $"{detail} (exit code {exitCode})";

            throw new WslSquadException(code, target, technical);
        }
    }
}

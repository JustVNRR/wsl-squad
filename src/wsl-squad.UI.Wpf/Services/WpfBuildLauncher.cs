using System.Diagnostics;
using System.IO;
using System.Text.Json;

using wsl_squad.Core.Dtos;
using wsl_squad.Presentation.Gui.Ports;

namespace wsl_squad.UI.Wpf.Services;

public sealed class WpfBuildLauncher : IBuildLauncher
{
    public async Task<int> RunAsync(BuildDto request)
    {
        string requestPath = Path.Combine(
            Path.GetTempPath(),
            $"wsl-squad-build-{Guid.NewGuid():N}.json");

        await File.WriteAllTextAsync(requestPath, JsonSerializer.Serialize(request));

        try
        {
            using Process process = LaunchBuildCli($"build --request \"{requestPath}\"");
            await process.WaitForExitAsync();

            return process.ExitCode;
        }
        finally
        {
            try
            {
                File.Delete(requestPath);
            }
            catch (IOException ex)
            {
                Trace.TraceWarning($"[WpfBuildLauncher] Could not delete temporary build request file '{requestPath}': {ex.Message}");
            }
        }
    }

    private static Process LaunchBuildCli(string arguments)
    {
        string[] directories =
        [
            Path.Combine(AppContext.BaseDirectory, "cli"),
            AppContext.BaseDirectory,
        ];

        foreach (string directory in directories)
        {
            string executable = Path.Combine(directory, "wsl-squad.cli.exe");

            if (File.Exists(executable))
            {
                return Process.Start(new ProcessStartInfo(executable)
                {
                    Arguments = arguments,
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetTempPath(),
                }) ?? throw new InvalidOperationException("The CLI could not be started.");
            }

            string assembly = Path.Combine(directory, "wsl-squad.cli.dll");

            if (File.Exists(assembly))
            {
                return Process.Start(new ProcessStartInfo("dotnet")
                {
                    Arguments = $"\"{assembly}\" {arguments}",
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetTempPath(),
                }) ?? throw new InvalidOperationException("The CLI could not be started.");
            }
        }

        throw new InvalidOperationException("The CLI is missing next to the application.");
    }
}

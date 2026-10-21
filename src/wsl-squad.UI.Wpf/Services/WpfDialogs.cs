using System.Diagnostics;
using System.IO;
using System.Text.Json;

using wsl_squad.Core.Dtos;
using wsl_squad.Presentation.Abstractions;
using wsl_squad.Presentation.ViewModels;
using wsl_squad.UI.Wpf.Views;
using wsl_squad.UI.Wpf.Views.Dialogs;
using wsl_squad.UI.Wpf.Views.Instance;

namespace wsl_squad.UI.Wpf.Services;

public sealed class WpfDialogs : IDialogService
{
    public bool Confirm(string title, string question, string? danger = null)
    {
        ConfirmViewModel viewModel = new(title, question, danger);

        return DialogHost.Show(new ConfirmWindow(viewModel)) == true;
    }

    public void ShowError(LogEntryDto entry)
    {
        ErrorViewModel viewModel = new(entry);
        _ = DialogHost.Show(new ErrorWindow(viewModel));
    }

    public void OpenPath(string path)
    {
        if (!string.IsNullOrEmpty(path))
        {
            _ = Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
    }

    public string? ShowRecipeName()
    {
        RecipeNameViewModel viewModel = new();
        _ = DialogHost.Show(new RecipeNameWindow(viewModel));

        return viewModel.Result;
    }

    public PackDuplicateResult? ShowPackDuplicate(string source, string blurb, IReadOnlyList<string> taken)
    {
        PackDuplicateViewModel viewModel = new(source, blurb, taken);
        _ = DialogHost.Show(new PackDuplicateWindow(viewModel));

        return viewModel.Result;
    }

    public string? ShowRootMove(string current)
    {
        RootMoveViewModel viewModel = new(current);
        _ = DialogHost.Show(new RootMoveWindow(viewModel));

        return viewModel.Result;
    }

    public BuildDto? ShowBuild(BuildViewModel viewModel)
    {
        _ = DialogHost.Show(new BuildWindow(viewModel));

        return viewModel.Result;
    }

    public async Task<int> ShowBuildTerminalAsync(BuildDto request)
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
                Trace.TraceWarning($"[WpfDialogs] Could not delete temporary build request file '{requestPath}': {ex.Message}");
            }
        }
    }

    public SettingsOutcome? ShowSettings(SettingsViewModel viewModel)
    {
        _ = DialogHost.Show(new SettingsWindow(viewModel));

        return viewModel.Result;
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

    public ArchiveDto? ShowArchive(ArchiveViewModel viewModel)
    {
        _ = DialogHost.Show(new ArchiveWindow(viewModel));

        return viewModel.Result;
    }

    public CompactDto? ShowCompact(CompactViewModel viewModel)
    {
        _ = DialogHost.Show(new CompactWindow(viewModel));

        return viewModel.Result;
    }

    public RemoveDto? ShowRemove(RemoveViewModel viewModel)
    {
        _ = DialogHost.Show(new RemoveWindow(viewModel));

        return viewModel.Result;
    }

    public RestoreDto? ShowRestore(RestoreViewModel viewModel)
    {
        _ = DialogHost.Show(new RestoreWindow(viewModel));

        return viewModel.Result;
    }

    public DuplicateDto? ShowDuplicate(DuplicateViewModel viewModel)
    {
        _ = DialogHost.Show(new DuplicateWindow(viewModel));

        return viewModel.Result;
    }

    public string? ShowDeleteArchive(DeleteArchiveViewModel viewModel)
    {
        _ = DialogHost.Show(new DeleteArchiveWindow(viewModel));

        return viewModel.Result;
    }

    public DockerDialogResult? ShowDockerRegister(DockerRegisterViewModel viewModel)
    {
        _ = DialogHost.Show(new DockerRegisterWindow(viewModel));

        return viewModel.Result;
    }

    public SetAppearanceDto? ShowSetAppearance(SetAppearanceViewModel viewModel)
    {
        _ = DialogHost.Show(new AppearanceWindow(viewModel));

        return viewModel.Result;
    }
}

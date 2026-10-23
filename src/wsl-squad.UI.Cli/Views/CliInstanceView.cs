using System.Diagnostics;

using wsl_squad.Core.Dtos;
using wsl_squad.Presentation.Cli.Ports;
using wsl_squad.UI.Cli.Interaction;

namespace wsl_squad.UI.Cli.Views;

public sealed class CliInstanceView : CliViewBase, IInstanceView
{
    public void ShowNoInstances()
    {
        Write(string.Empty);
        Write("[ABORT] No instance of this template is registered on this machine.", ConsoleTone.Error);
        Write("        Build one with  wsl-squad build", ConsoleTone.Hint);
    }

    public string? AskOffered(IReadOnlyList<WslIndexDto> instances)
    {
        WslIndexDto? picked = Pick(
            "Our Instances",
            instances,
            row => $"{row.Name,-30} {Size(row.Size),10}");

        return picked?.Name;
    }

    public void ShowStopWarning(string name)
    {
        Write(string.Empty);
        Write($"  If '{name}' is running, this will stop it: an archive reads a still disk.", ConsoleTone.Warning);
        Write("  Save what you have open in there - stopping it loses anything unsaved.", ConsoleTone.Warning);
    }

    public void ShowReplacing(string archiveName)
    {
        Write($"  '{archiveName}' exists: replacing it.", ConsoleTone.Warning);
    }

    public void ShowBackingUp(string name)
    {
        Write(string.Empty);
        Write($"==> Backing up '{name}'", ConsoleTone.Info);
    }

    public void Show(ArchiveResponseDto response)
    {
        Write(string.Empty);
        Write("============================================================", ConsoleTone.Success);
        Write($"       Backup of '{response.InstanceName}' written", ConsoleTone.Success);
        Write("============================================================", ConsoleTone.Success);
        Write(string.Empty);
        Write($"  * Look             : font '{response.LookFont}', colours '{response.LookColourScheme}'", ConsoleTone.Muted);
        Write($"  * Docker Desktop   : {DockerText(response.Docker)}", ConsoleTone.Muted);
        Write($"  * Archive          : {response.ArchiveDir}", ConsoleTone.Info);
        Write($"  * Tar              : {Path.GetFileName(response.Tar ?? string.Empty)}", ConsoleTone.Info);
        Write(string.Empty);
        Write("------------------------------------------------------------", ConsoleTone.Muted);
        Write("To restore it as a new instance:", ConsoleTone.Hint);
        Write("  wsl-squad restore        (it lists the archives, this one included)", ConsoleTone.Hint);
        Write("------------------------------------------------------------", ConsoleTone.Muted);
        Write(string.Empty);
        Write(
            response.Stopped
                ? $"'{response.InstanceName}' is running again."
                : $"'{response.InstanceName}' is left stopped.",
            response.Stopped ? ConsoleTone.Success : ConsoleTone.Muted);
        Write(string.Empty);
    }

    public void ShowBeforeQuestions()
    {
        Write(string.Empty);
        Write("  WARNING: THE INSTANCE WILL BE STOPPED.", ConsoleTone.Warning);
        Write("  Make sure nothing is running inside.", ConsoleTone.Warning);
    }

    public void ShowNoArchive()
    {
        Write("  No archive will be written.", ConsoleTone.Muted);
    }

    public void ShowReplacingArchive(string name)
    {
        Write($"  '{name}_archive' exists: replacing it.", ConsoleTone.Warning);
    }

    public void ShowCompacting(string name, bool archiveFirst)
    {
        Write(string.Empty);
        Write($"==> Compacting '{name}'{(archiveFirst ? " (archived first)" : string.Empty)}", ConsoleTone.Info);
    }

    public void Show(CompactResponseDto response)
    {
        Write(string.Empty);
        Write($"==> '{response.InstanceName}' compacted.", ConsoleTone.Success);
        Write($"    Before {Size(response.SizeBefore)}, after {Size(response.SizeAfter)}, gained {Size(response.Gained)}.", ConsoleTone.Info);

        if (response.Archived)
        {
            Write("    An archive was written first.", ConsoleTone.Muted);
        }

        Write(
            response.Restarted
                ? $"    '{response.InstanceName}' is running again."
                : $"    '{response.InstanceName}' is left stopped.",
            response.Restarted ? ConsoleTone.Success : ConsoleTone.Muted);
    }

    public void ShowDeleting(string target)
    {
        Write(string.Empty);
        Write($"==> Deleting the archive '{target}'", ConsoleTone.Info);
    }

    public void Show(DeleteArchiveResponseDto response)
    {
        Write($"==> '{response.ArchiveName}' deleted.", ConsoleTone.Success);
    }

    public string? AskCopyName()
    {
        string? answer = AskLine("Name of the copy (Enter to cancel)");

        return answer?.Trim();
    }

    public void ShowNameProblem(string message)
    {
        Write($"  {message}", ConsoleTone.Warning);
    }

    public void ShowDiskWarning(string source)
    {
        Write(string.Empty);
        Write($"  '{source}' will be stopped while it is copied, and needs room for twice its disk.", ConsoleTone.Warning);
    }

    public void ShowDuplicating(string source, string copy)
    {
        Write(string.Empty);
        Write($"==> Duplicating '{source}' as '{copy}'", ConsoleTone.Info);
    }

    public void Show(DuplicateResponseDto response)
    {
        Write($"==> '{response.CopyName}' created from '{response.SourceName}'.", ConsoleTone.Success);
    }

    public void ShowManagingPacks(string name)
    {
        Write(string.Empty);
        Write($"==> Opening the pack manager for '{name}'...", ConsoleTone.Info);
    }

    public void ShowOpening(string name)
    {
        Write(string.Empty);
        Write($"==> Opening a shell in '{name}'...", ConsoleTone.Info);
    }

    public void Show(OpenResponseDto response)
    {
    }


    public bool? AskDockerAction(string name, bool shouldRegister)
    {
        string question = shouldRegister
            ? $"Register '{name}' with Docker Desktop?"
            : $"Unregister '{name}' from Docker Desktop?";

        if (AskConfirmation(question, defaultNo: false) != true)
        {
            return null;
        }

        return AskConfirmation("Restart Docker Desktop to apply now?", defaultNo: false) == true;
    }

    public void Show(RegisterDockerResponseDto response)
    {
        Write($"==> Docker Desktop: {response.InstanceName} {response.Restart}.", ConsoleTone.Success);
    }

    public void Show(UnregisterDockerResponseDto response)
    {
        Write($"==> Docker Desktop: {response.InstanceName} {response.Restart}.", ConsoleTone.Success);
    }

    public bool ConfirmDestruction(string instanceName)
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
                Console.Beep(1000, 400);
            }
            catch (PlatformNotSupportedException ex)
            {
                Trace.TraceWarning($"[CliInstanceView] Console.Beep not supported: {ex.Message}");
            }
        }

        Write(string.Empty);
        Banner("DANGER: TOTAL DATA LOSS IMMINENT");
        Write(string.Empty);
        Write($"  The WSL distribution '{instanceName}' and ALL its data will be deleted:", ConsoleTone.Error);
        Write(string.Empty);
        Write("  Proceeding will PERMANENTLY DESTROY this distribution:", ConsoleTone.Warning);
        Write($"    - Executing: wsl --unregister {instanceName}", ConsoleTone.Muted);
        Write("    - IRREVERSIBLE DELETION of the virtual disk (VHDX)", ConsoleTone.Muted);
        Write("    - TOTAL LOSS of projects, SSH keys, and all files in /home", ConsoleTone.Muted);
        Write(string.Empty);
        Write("  THIS OPERATION CANNOT BE UNDONE.", ConsoleTone.Error);
        Write(string.Empty);
        Write(" ----------------------------------------------------------------------", ConsoleTone.Muted);
        Write(" Press ENTER to abort immediately.", ConsoleTone.Hint);
        Write(" To confirm DESTRUCTION, type the name of the distribution:", ConsoleTone.Hint);
        string? typed = AskLine(" Confirm");
        Write(" ----------------------------------------------------------------------", ConsoleTone.Muted);
        Write(string.Empty);

        if (typed != instanceName)
        {
            Write("[ABORT] Operation cancelled. No data was modified.", ConsoleTone.Warning);

            return false;
        }

        Write(string.Empty);

        return true;
    }

    public void ShowRemovingSteps()
    {
        Write("==> Stopping the distro processes...", ConsoleTone.Info);
        Write("==> Unregistering the distro...", ConsoleTone.Info);
    }

    public void Show()
    {
        Write(string.Empty);
        Write("============================================================", ConsoleTone.Success);
        Write("                WSL SQUAD Instance Removed!                 ", ConsoleTone.Success);
        Write("============================================================", ConsoleTone.Success);
        Write(string.Empty);
        Write(" Restart Windows Terminal to refresh the profile list.");
        Write(string.Empty);
    }

    public void ShowStoppingWarning(string name)
    {
        Write(string.Empty);
        Write($"  '{name}' will be stopped before it is started again.", ConsoleTone.Warning);
    }

    public bool? AskRestart()
    {
        return AskConfirmation("Restart it now?", defaultNo: false);
    }

    public void ShowRestarting(string name)
    {
        Write($"==> Restarting '{name}'...", ConsoleTone.Info);
    }

    public void Show(RestartResponseDto response)
    {
        Write($"==> '{response.InstanceName}' restarted.", ConsoleTone.Success);
    }

    public void ShowNoArchives()
    {
        Write(string.Empty);
        Write("[ABORT] There are no archives.", ConsoleTone.Error);
    }

    public string? AskArchive(IReadOnlyList<WslIndexDto> archives)
    {
        WslIndexDto? picked = Pick(
            "Our Archives",
            archives,
            row => $"{row.Name,-30} {Size(row.Size),10}");

        return picked?.Name;
    }

    public void ShowRestoring(string archiveName)
    {
        Write(string.Empty);
        Write($"==> Restoring '{archiveName}'", ConsoleTone.Info);
    }

    public string? AskNewName()
    {
        string? answer = AskLine("Name of the restored instance");

        return answer?.Trim();
    }

    public void Show(RestoreResponseDto response)
    {
        Write($"==> '{response.InstanceName}' restored from '{response.ArchiveName}'.", ConsoleTone.Success);
    }

    private static string DockerText(string? state)
    {
        return state switch
        {
            "yes" => "knows this instance",
            "no" => "does not know it",
            _ => "not installed, or unreadable",
        };
    }
}

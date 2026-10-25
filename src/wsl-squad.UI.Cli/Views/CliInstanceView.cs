using System.Diagnostics;

using WslSquad.Core.Dtos;
using WslSquad.Presentation.Cli.Ports;
using WslSquad.UI.Cli.Interaction;
using WslSquad.Presentation.Localization;

namespace WslSquad.UI.Cli.Views;

public sealed class CliInstanceView : CliViewBase, IInstanceView
{
    public CliInstanceView(ConsoleHost host)
        : base(host)
    {
    }

    public void ShowNoInstances()
    {
        Write(string.Empty);
        Write(Localizer.Instance["CliNoInstance"], ConsoleTone.Error);
        Write(Localizer.Instance["CliBuildOne"], ConsoleTone.Hint);
    }

    public string? AskOffered(IReadOnlyList<WslIndexDto> instances)
    {
        WslIndexDto? picked = Pick(
            Localizer.Instance["CliOurInstances"],
            instances,
            row => $"{row.Name,-30} {Size(row.Size),10}");

        return picked?.Name;
    }

    public void ShowStopWarning(string name)
    {
        Write(string.Empty);
        Write(Localizer.Instance.Format("CliStopWarning1", name), ConsoleTone.Warning);
        Write(Localizer.Instance["CliStopWarning2"], ConsoleTone.Warning);
    }

    public void ShowReplacing(string archiveName)
    {
        Write(Localizer.Instance.Format("CliReplacing", archiveName), ConsoleTone.Warning);
    }

    public void ShowBackingUp(string name)
    {
        Write(string.Empty);
        Write(Localizer.Instance.Format("CliBackingUp", name), ConsoleTone.Info);
    }

    public void Show(ArchiveResponseDto response)
    {
        Write(string.Empty);
        Write(Localizer.Instance["CliRule"], ConsoleTone.Success);
        Write(Localizer.Instance.Format("CliBackupWritten", response.InstanceName), ConsoleTone.Success);
        Write(Localizer.Instance["CliRule"], ConsoleTone.Success);
        Write(string.Empty);
        Write(Localizer.Instance.Format("CliLookLine", response.LookFont, response.LookColourScheme), ConsoleTone.Muted);
        Write(Localizer.Instance.Format("CliDockerLine", DockerText(response.Docker)), ConsoleTone.Muted);
        Write(Localizer.Instance.Format("CliArchiveLine", response.ArchiveDir), ConsoleTone.Info);
        Write(Localizer.Instance.Format("CliTarLine", Path.GetFileName(response.Tar ?? string.Empty)), ConsoleTone.Info);
        Write(string.Empty);
        Write(Localizer.Instance["CliHr"], ConsoleTone.Muted);
        Write(Localizer.Instance["CliRestoreHint"], ConsoleTone.Hint);
        Write(Localizer.Instance["CliRestoreExample"], ConsoleTone.Hint);
        Write(Localizer.Instance["CliHr"], ConsoleTone.Muted);
        Write(string.Empty);
        Write(
            response.Stopped
                ? Localizer.Instance.Format("CliRunningAgain", response.InstanceName)
                : Localizer.Instance.Format("CliLeftStopped", response.InstanceName),
            response.Stopped ? ConsoleTone.Success : ConsoleTone.Muted);
        Write(string.Empty);
    }

    public void ShowBeforeQuestions()
    {
        Write(string.Empty);
        Write(Localizer.Instance["CliWarningStopped"], ConsoleTone.Warning);
        Write(Localizer.Instance["CliNothingRunning"], ConsoleTone.Warning);
    }

    public void ShowNoArchive()
    {
        Write(Localizer.Instance["CliNoArchiveWritten"], ConsoleTone.Muted);
    }

    public void ShowReplacingArchive(string name)
    {
        Write(Localizer.Instance.Format("CliReplacingArchive", name), ConsoleTone.Warning);
    }

    public void ShowCompacting(string name, bool archiveFirst)
    {
        Write(string.Empty);
        Write(Localizer.Instance.Format("CliCompacting", name, archiveFirst ? Localizer.Instance["CliArchivedFirst"] : string.Empty), ConsoleTone.Info);
    }

    public void Show(CompactResponseDto response)
    {
        Write(string.Empty);
        Write(Localizer.Instance.Format("CliCompacted", response.InstanceName), ConsoleTone.Success);
        Write(Localizer.Instance.Format("CliCompactStats", Size(response.SizeBefore), Size(response.SizeAfter), Size(response.Gained)), ConsoleTone.Info);

        if (response.Archived)
        {
            Write(Localizer.Instance["CliArchiveWrittenFirst"], ConsoleTone.Muted);
        }

        Write(
            response.Restarted
                ? Localizer.Instance.Format("CliIndentedRunningAgain", response.InstanceName)
                : Localizer.Instance.Format("CliIndentedLeftStopped", response.InstanceName),
            response.Restarted ? ConsoleTone.Success : ConsoleTone.Muted);
    }

    public void ShowDeleting(string target)
    {
        Write(string.Empty);
        Write(Localizer.Instance.Format("CliDeletingArchive", target), ConsoleTone.Info);
    }

    public void Show(DeleteArchiveResponseDto response)
    {
        Write(Localizer.Instance.Format("CliArchiveDeleted", response.ArchiveName), ConsoleTone.Success);
    }

    public string? AskCopyName()
    {
        string? answer = AskLine(Localizer.Instance["CliAskCopyName"]);

        return answer?.Trim();
    }

    public void ShowNameProblem(string message)
    {
        Write($"  {message}", ConsoleTone.Warning);
    }

    public void ShowDiskWarning(string source)
    {
        Write(string.Empty);
        Write(Localizer.Instance.Format("CliDiskWarning", source), ConsoleTone.Warning);
    }

    public void ShowDuplicating(string source, string copy)
    {
        Write(string.Empty);
        Write(Localizer.Instance.Format("CliDuplicating", source, copy), ConsoleTone.Info);
    }

    public void Show(DuplicateResponseDto response)
    {
        Write(Localizer.Instance.Format("CliDuplicateCreated", response.CopyName, response.SourceName), ConsoleTone.Success);
    }

    public void ShowManagingPacks(string name)
    {
        Write(string.Empty);
        Write(Localizer.Instance.Format("CliOpeningPackManager", name), ConsoleTone.Info);
    }

    public void ShowOpening(string name)
    {
        Write(string.Empty);
        Write(Localizer.Instance.Format("CliOpeningShell", name), ConsoleTone.Info);
    }

    public void Show(OpenResponseDto response)
    {
    }


    public bool? AskDockerAction(string name, bool shouldRegister)
    {
        string question = shouldRegister
            ? Localizer.Instance.Format("CliDockerRegister", name)
            : Localizer.Instance.Format("CliDockerUnregister", name);

        if (AskConfirmation(question, defaultNo: false) != true)
        {
            return null;
        }

        return AskConfirmation(Localizer.Instance["CliRestartDockerNow"], defaultNo: false) == true;
    }

    public void Show(RegisterDockerResponseDto response)
    {
        Write(Localizer.Instance.Format("CliDockerResult", response.InstanceName, response.Restart.ToMessage()), ConsoleTone.Success);
    }

    public void Show(UnregisterDockerResponseDto response)
    {
        Write(Localizer.Instance.Format("CliDockerResult", response.InstanceName, response.Restart.ToMessage()), ConsoleTone.Success);
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
        Banner(Localizer.Instance["CliDangerBanner"]);
        Write(string.Empty);
        Write(Localizer.Instance.Format("CliRemoveIntro", instanceName), ConsoleTone.Error);
        Write(string.Empty);
        Write(Localizer.Instance["CliRemoveDestroy"], ConsoleTone.Warning);
        Write(Localizer.Instance.Format("CliRemoveStepUnregister", instanceName), ConsoleTone.Muted);
        Write(Localizer.Instance["CliRemoveStepDisk"], ConsoleTone.Muted);
        Write(Localizer.Instance["CliRemoveStepHome"], ConsoleTone.Muted);
        Write(string.Empty);
        Write(Localizer.Instance["CliCannotUndo"], ConsoleTone.Error);
        Write(string.Empty);
        Write(Localizer.Instance["CliShortRule"], ConsoleTone.Muted);
        Write(Localizer.Instance["CliPressEnterAbort"], ConsoleTone.Hint);
        Write(Localizer.Instance["CliTypeDestroy"], ConsoleTone.Hint);
        string? typed = AskLine(Localizer.Instance["CliConfirm"]);
        Write(Localizer.Instance["CliShortRule"], ConsoleTone.Muted);
        Write(string.Empty);

        if (typed != instanceName)
        {
            Write(Localizer.Instance["CliAbortedNothingChanged"], ConsoleTone.Warning);

            return false;
        }

        Write(string.Empty);

        return true;
    }

    public void ShowRemovingSteps()
    {
        Write(Localizer.Instance["CliStoppingProcesses"], ConsoleTone.Info);
        Write(Localizer.Instance["CliUnregistering"], ConsoleTone.Info);
    }

    public void Show()
    {
        Write(string.Empty);
        Write(Localizer.Instance["CliRule"], ConsoleTone.Success);
        Write(Localizer.Instance["CliRemovedBanner"], ConsoleTone.Success);
        Write(Localizer.Instance["CliRule"], ConsoleTone.Success);
        Write(string.Empty);
        Write(Localizer.Instance["CliRestartTerminal"]);
        Write(string.Empty);
    }

    public void ShowStoppingWarning(string name)
    {
        Write(string.Empty);
        Write(Localizer.Instance.Format("CliStoppedBeforeStart", name), ConsoleTone.Warning);
    }

    public bool? AskRestart()
    {
        return AskConfirmation(Localizer.Instance["CliRestartNow"], defaultNo: false);
    }

    public void ShowRestarting(string name)
    {
        Write(Localizer.Instance.Format("CliRestarting", name), ConsoleTone.Info);
    }

    public void Show(RestartResponseDto response)
    {
        Write(Localizer.Instance.Format("CliRestarted", response.InstanceName), ConsoleTone.Success);
    }

    public void ShowNoArchives()
    {
        Write(string.Empty);
        Write(Localizer.Instance["CliNoArchives"], ConsoleTone.Error);
    }

    public string? AskArchive(IReadOnlyList<WslIndexDto> archives)
    {
        WslIndexDto? picked = Pick(
            Localizer.Instance["CliOurArchives"],
            archives,
            row => $"{row.Name,-30} {Size(row.Size),10}");

        return picked?.Name;
    }

    public void ShowRestoring(string archiveName)
    {
        Write(string.Empty);
        Write(Localizer.Instance.Format("CliRestoring", archiveName), ConsoleTone.Info);
    }

    public string? AskNewName()
    {
        string? answer = AskLine(Localizer.Instance["CliAskRestoredName"]);

        return answer?.Trim();
    }

    public void Show(RestoreResponseDto response)
    {
        Write(Localizer.Instance.Format("CliRestored", response.InstanceName, response.ArchiveName), ConsoleTone.Success);
    }

    private static string DockerText(string? state)
    {
        return state switch
        {
            "yes" => Localizer.Instance["CliDockerKnows"],
            "no" => Localizer.Instance["CliDockerDoesNotKnow"],
            _ => Localizer.Instance["CliDockerUnknown"],
        };
    }
}

using wsl_squad.Core.Dtos;

namespace wsl_squad.Presentation.Presenters.Views;

public interface IInstanceView : IPresenterViewBase
{
    public void ShowNoInstances();

    public string? AskOffered(IReadOnlyList<WslIndexDto> instances);

    public void ShowStopWarning(string name);

    public bool? Confirm(string question, bool defaultNo);

    public void ShowCancelled(string noun);

    public void ShowReplacing(string archiveName);

    public void ShowBackingUp(string name);

    public void Show(ArchiveResponseDto response);

    public void ShowBeforeQuestions();

    public void ShowNoArchive();

    public void ShowReplacingArchive(string name);

    public void ShowCompacting(string name, bool archiveFirst);

    public void Show(CompactResponseDto response);

    public void ShowNoName();

    public void ShowDeleting(string target);

    public void Show(DeleteArchiveResponseDto response);

    public string? AskCopyName();

    public void ShowNameHint();

    public void ShowDiskWarning(string source);

    public void ShowDuplicating(string source, string copy);

    public void Show(DuplicateResponseDto response);

    public void ShowManagingPacks(string name);

    public void ShowOpening(string name);

    public void Show(OpenResponseDto response);

    public void ShowAlreadyRegistered(string name);

    public void ShowNotRegistered(string name);

    public void ShowSettingsUnreadable();

    public void ShowNoCandidates();

    public bool? AskRegister(string name);

    public bool? AskUnregister(string name);

    public bool? AskRestartDocker();

    public void Show(RegisterDockerResponseDto response);

    public void Show(UnregisterDockerResponseDto response);

    public void ShowDestructionGate(string name, string question);

    public string? AskTypedName();

    public void ShowGateClose();

    public void ShowGateRefused();

    public void ShowGateOpened();

    public void ShowRemovingSteps();

    public void Show();

    public void ShowStoppingWarning(string name);

    public bool? AskRestart();

    public void ShowRestarting(string name);

    public void Show(RestartResponseDto response);

    public void ShowNoArchives();

    public string? AskArchive(IReadOnlyList<WslIndexDto> archives);

    public void ShowRestoring(string archiveName);

    public string? AskNewName();

    public void Show(RestoreResponseDto response);
}

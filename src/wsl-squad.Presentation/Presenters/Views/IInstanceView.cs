using wsl_squad.Core.Dtos;

namespace wsl_squad.Presentation.Presenters.Views;

public interface IInstanceView : IPresenterViewBase
{
    void ShowNoInstances();

    string? AskOffered(IReadOnlyList<WslIndexDto> instances);

    void ShowStopWarning(string name);

    bool? Confirm(string question, bool defaultNo);

    void ShowCancelled(string noun);

    void ShowReplacing(string archiveName);

    void ShowBackingUp(string name);

    void Show(ArchiveResponseDto response);

    void ShowBeforeQuestions();

    void ShowNoArchive();

    void ShowReplacingArchive(string name);

    void ShowCompacting(string name, bool archiveFirst);

    void Show(CompactResponseDto response);

    void ShowNoName();

    void ShowDeleting(string target);

    void Show(DeleteArchiveResponseDto response);

    string? AskCopyName();

    void ShowNameHint();

    void ShowDiskWarning(string source);

    void ShowDuplicating(string source, string copy);

    void Show(DuplicateResponseDto response);

    void ShowManagingPacks(string name);

    void ShowOpening(string name);

    void Show(OpenResponseDto response);

    void ShowAlreadyRegistered(string name);

    void ShowNotRegistered(string name);

    void ShowSettingsUnreadable();

    void ShowNoCandidates();

    bool? AskRegister(string name);

    bool? AskUnregister(string name);

    bool? AskRestartDocker();

    void Show(RegisterDockerResponseDto response);

    void Show(UnregisterDockerResponseDto response);

    void ShowDestructionGate(string name, string question);

    string? AskTypedName();

    void ShowGateClose();

    void ShowGateRefused();

    void ShowGateOpened();

    void ShowRemovingSteps();

    void Show();

    void ShowStoppingWarning(string name);

    bool? AskRestart();

    void ShowRestarting(string name);

    void Show(RestartResponseDto response);

    void ShowNoArchives();

    string? AskArchive(IReadOnlyList<WslIndexDto> archives);

    void ShowRestoring(string archiveName);

    string? AskNewName();

    void Show(RestoreResponseDto response);
}

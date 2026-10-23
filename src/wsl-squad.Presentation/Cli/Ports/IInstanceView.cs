using wsl_squad.Core.Dtos;

namespace wsl_squad.Presentation.Cli.Ports;

public interface IInstanceView : IPresenterViewBase
{
    void ShowNoInstances();

    string? AskOffered(IReadOnlyList<WslIndexDto> instances);

    void ShowNoArchives();

    string? AskArchive(IReadOnlyList<WslIndexDto> archives);

    void ShowStopWarning(string name);

    void ShowReplacing(string archiveName);

    void ShowBackingUp(string name);

    void Show(ArchiveResponseDto response);

    void ShowBeforeQuestions();

    void ShowNoArchive();

    void ShowReplacingArchive(string name);

    void ShowCompacting(string name, bool archiveFirst);

    void Show(CompactResponseDto response);

    void ShowNameHint();

    string? AskCopyName();

    void ShowDiskWarning(string source);

    void ShowDuplicating(string source, string copy);

    void Show(DuplicateResponseDto response);

    void ShowOpening(string name);

    void Show(OpenResponseDto response);

    void ShowStoppingWarning(string name);

    bool? AskRestart();

    void ShowRestarting(string name);

    void Show(RestartResponseDto response);

    void ShowManagingPacks(string name);

    bool ConfirmDestruction(string instanceName);

    void ShowRemovingSteps();

    void Show();

    void ShowAlreadyRegistered(string name);

    void ShowNotRegistered(string name);

    void ShowSettingsUnreadable();

    void ShowNoCandidates();

    bool? AskDockerAction(string name, bool shouldRegister);

    void Show(RegisterDockerResponseDto response);

    void Show(UnregisterDockerResponseDto response);

    void ShowRestoring(string archiveName);

    string? AskNewName();

    void Show(RestoreResponseDto response);

    void ShowDeleting(string target);

    void Show(DeleteArchiveResponseDto response);
}

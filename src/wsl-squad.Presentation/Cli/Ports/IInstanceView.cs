using WslSquad.Core.Dtos;

namespace WslSquad.Presentation.Cli.Ports;

public interface IInstanceView : IPresenterViewBase
{
    void ShowNoInstances();

    string? AskOffered(IReadOnlyList<WslIndexDto> instances);

    void ShowNoArchives();

    string? AskArchive(IReadOnlyList<WslIndexDto> archives);

    void ShowStopWarning(string name);

    void ShowReplacing(string archiveName);

    void ShowBackingUp(string name);

    void ShowBeforeQuestions();

    void ShowNoArchive();

    void ShowReplacingArchive(string name);

    void ShowCompacting(string name, bool archiveFirst);

    void ShowNameProblem(string message);

    string? AskCopyName();

    void ShowDiskWarning(string source);

    void ShowDuplicating(string source, string copy);

    void ShowOpening(string name);

    void ShowStoppingWarning(string name);

    bool? AskRestart();

    void ShowRestarting(string name);

    void ShowManagingPacks(string name);

    bool ConfirmDestruction(string instanceName);

    void ShowRemovingSteps();

    bool? AskDockerAction(string name, bool shouldRegister);

    void ShowRestoring(string archiveName);

    string? AskNewName();

    void ShowDeleting(string target);

    void Show(ArchiveResponseDto response);

    void Show(CompactResponseDto response);

    void Show(DuplicateResponseDto response);

    void Show(OpenResponseDto response);

    void Show(RestartResponseDto response);

    void Show(RegisterDockerResponseDto response);

    void Show(UnregisterDockerResponseDto response);

    void Show(RestoreResponseDto response);

    void Show(DeleteArchiveResponseDto response);

    void Show();
}

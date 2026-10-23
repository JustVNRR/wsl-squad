using wsl_squad.Core.Dtos;

namespace wsl_squad.Presentation.Presenters.Views;

public interface IInstanceArchiveView : IInstancePickerView
{
    void ShowStopWarning(string name);

    void ShowReplacing(string archiveName);

    void ShowBackingUp(string name);

    void Show(ArchiveResponseDto response);
}

public interface IInstanceArchiveReplacementView : IInstancePickerView
{
    void ShowReplacingArchive(string name);
}

public interface IInstanceCompactView : IInstanceArchiveReplacementView
{
    void ShowBeforeQuestions();

    void ShowNoArchive();

    void ShowCompacting(string name, bool archiveFirst);

    void Show(CompactResponseDto response);
}

public interface IInstanceNameHintView : IInstancePickerView
{
    void ShowNameHint();
}

public interface IInstanceDuplicateView : IInstanceNameHintView
{
    string? AskCopyName();

    void ShowDiskWarning(string source);

    void ShowDuplicating(string source, string copy);

    void Show(DuplicateResponseDto response);
}

public interface IInstanceOpenView : IInstancePickerView
{
    void ShowOpening(string name);

    void Show(OpenResponseDto response);
}

public interface IInstanceRestartView : IInstancePickerView
{
    void ShowStoppingWarning(string name);

    bool? AskRestart();

    void ShowRestarting(string name);

    void Show(RestartResponseDto response);
}

public interface IInstanceManagePacksView : IInstancePickerView
{
    void ShowManagingPacks(string name);
}

public interface IInstanceRemoveView : IInstanceArchiveReplacementView
{
    bool ConfirmDestruction(string instanceName);

    void ShowRemovingSteps();

    void Show();
}

public interface IInstanceDockerView : IInstancePickerView
{
    void ShowAlreadyRegistered(string name);

    void ShowNotRegistered(string name);

    void ShowSettingsUnreadable();

    void ShowNoCandidates();

    bool? AskDockerAction(string name, bool shouldRegister);

    void Show(RegisterDockerResponseDto response);

    void Show(UnregisterDockerResponseDto response);
}

public interface IInstanceRestoreView : IArchivePickerView, IInstanceNameHintView
{
    void ShowRestoring(string archiveName);

    string? AskNewName();

    void Show(RestoreResponseDto response);
}

public interface IInstanceDeleteArchiveView : IArchivePickerView
{
    void ShowDeleting(string target);

    void Show(DeleteArchiveResponseDto response);
}

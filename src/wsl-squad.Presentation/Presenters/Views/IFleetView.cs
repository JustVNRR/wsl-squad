using wsl_squad.Core.Dtos;

namespace wsl_squad.Presentation.Presenters.Views;

public interface IFleetView : IPresenterViewBase
{
    void Clear();

    string? PickInstance(
        IReadOnlyList<WslIndexDto> instances,
        IReadOnlyList<WslOrphanDto> orphans,
        IReadOnlyList<string> running);

    void ShowNoInstances(IReadOnlyList<WslOrphanDto> orphans);

    string? PickArchive(IReadOnlyList<WslIndexDto> archives);

    void ShowNoArchives();

    PresenterAction? PickAction(string title, IReadOnlyList<PresenterAction> actions);

    WslPackDto? PickPack(IReadOnlyList<WslPackDto> packs);

    void ShowNoPacks();

    string? PickPackAction(string packName);

    string? AskName(string sourceName);

    void ShowOpening(string name);

    void ShowDuplicated(string name);

    void ShowRemoved(string name);

    void ShowDependents(string name, IReadOnlyList<string> dependents);

    string? AskTarget(string root);

    void ShowSameFolder();

    void ShowTargetProblem(string problem);

    void ShowLead();

    void Show(MigrateResponseDto response);

    void Show(WslConfigResponseDto response);
}

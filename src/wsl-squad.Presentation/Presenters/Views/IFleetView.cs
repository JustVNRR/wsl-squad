using wsl_squad.Core.Dtos;

namespace wsl_squad.Presentation.Presenters.Views;

/// <summary>The fleet browser, settings and migration prompts.</summary>
public interface IFleetView : IPresenterViewBase
{
    public void Clear();

    public string? PickInstance(
        IReadOnlyList<WslIndexDto> instances,
        IReadOnlyList<WslOrphanDto> orphans,
        IReadOnlyList<string> running);

    public void ShowNoInstances(IReadOnlyList<WslOrphanDto> orphans);

    public string? PickArchive(IReadOnlyList<WslIndexDto> archives);

    public void ShowNoArchives();

    public PresenterAction? PickAction(string title, IReadOnlyList<PresenterAction> actions);

    public WslPackDto? PickPack(IReadOnlyList<WslPackDto> packs);

    public void ShowNoPacks();

    public string? PickPackAction(string packName);

    public string? AskName(string sourceName);

    public void ShowOpening(string name);

    public void ShowDuplicated(string name);

    public void ShowRemoved(string name);

    public void ShowDependents(string name, IReadOnlyList<string> dependents);

    public bool? Confirm(string question, bool defaultNo);

    public string? AskTarget(string root);

    public void ShowCancelled(string noun);

    public void ShowSameFolder();

    public void ShowTargetProblem(string problem);

    public void ShowLead();

    public void Show(MigrateResponseDto response);

    public void Show(WslConfigResponseDto response);
}

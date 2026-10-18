using wsl_squad.Core.Dtos;

namespace wsl_squad.Presentation.Presenters.Views;

/// <summary>A build source or saved image, as offered by the dialogue.</summary>
public sealed record BuildSourceChoice(string Name, string Path, string? Family);

/// <summary>A build road.</summary>
public sealed record BuildRoad(WslBuildType Type, string Label);

/// <summary>The build assistant's prompts and reports.</summary>
public interface IBuildView : IPresenterViewBase
{
    public string? AskInstanceName(string prompt);

    public void ShowNameHint();

    public BuildRoad? AskRoad();

    public void ShowAbort(IReadOnlyList<string> lines);

    public string? AskRegistryReference(string prompt);

    public void ShowUnknownRegistry(string reference, string why);

    public BuildSourceChoice? AskSource(string label, IReadOnlyList<BuildSourceChoice> rows);

    public IReadOnlyList<WslBuildPackDto>? AskPacks(
        string name,
        IReadOnlyList<WslBuildPackDto> packs,
        IReadOnlyList<int> checkedIndexes,
        string note);

    public void ShowNoPackChosen();

    public void ShowNoOnboarding();

    public bool? AskRunOnboarding(bool isRecipe);

    public BuildSourceChoice? AskOnboarding(IReadOnlyList<BuildSourceChoice> rows);

    public void ShowBeforeAccount();

    public string? AskUserName(string prompt);

    public void ShowUserHint();

    public bool? AskKeepImage();

    public bool? AskRegisterDocker(string name);

    public void ShowCancelled(string noun);

    public void Show(BuildResponseDto response);
}

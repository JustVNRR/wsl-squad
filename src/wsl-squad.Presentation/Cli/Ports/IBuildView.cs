using WslSquad.Core.Dtos;

namespace WslSquad.Presentation.Cli.Ports;

public sealed record BuildSourceChoice(string Name, string Path);

public sealed record BuildRoad(WslBuildType Type, string Label);

public interface IBuildView : IPresenterViewBase
{
    string? AskInstanceName(string prompt);

    void ShowNameProblem(string message);

    BuildRoad? AskRoad();

    void ShowAbort(IReadOnlyList<string> lines);

    string? AskRegistryReference(string prompt);

    void ShowUnknownRegistry(string reference, string why);

    BuildSourceChoice? AskSource(string label, IReadOnlyList<BuildSourceChoice> rows);

    void ShowNoOnboarding();

    bool? AskRunOnboarding();

    BuildSourceChoice? AskOnboarding(IReadOnlyList<BuildSourceChoice> rows);

    void ShowBeforeAccount();

    string? AskUserName(string prompt);

    void ShowUserHint();

    bool? AskKeepImage();

    bool? AskRegisterDocker(string name);

    void Show(BuildResponseDto response);
}

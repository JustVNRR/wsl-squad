using WslSquad.Core.Dtos;

namespace WslSquad.Presentation.Cli.Ports;

public sealed record BuildSourceChoice(string Name, string Path);

public interface IBuildView : IPresenterViewBase
{
    string? AskInstanceName(string prompt);

    void ShowNameProblem(string message);

    void ShowAbort(IReadOnlyList<string> lines);

    BuildSourceChoice? AskSource(string label, IReadOnlyList<BuildSourceChoice> rows);

    bool? AskKeepImage();

    void Show(BuildResponseDto response);
}

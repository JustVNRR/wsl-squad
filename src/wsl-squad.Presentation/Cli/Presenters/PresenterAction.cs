using WslSquad.Presentation.Localization;
using WslSquad.Presentation.Workflows;

namespace WslSquad.Presentation.Cli.Presenters;

public sealed record PresenterAction(
    string Key,
    string Label,
    Func<string, IWorkflowResult> Execute,
    string? DisplayKey = null)
{
    public string Text => Localizer.Instance[Label];

    public string Display => Localizer.Instance[DisplayKey ?? Key];
}

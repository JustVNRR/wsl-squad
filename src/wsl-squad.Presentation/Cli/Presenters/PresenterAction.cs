using WslSquad.Presentation.Workflows;

namespace WslSquad.Presentation.Cli.Presenters;

public sealed record PresenterAction(
    string Key,
    string Label,
    Func<string, IWorkflowResult> Execute);

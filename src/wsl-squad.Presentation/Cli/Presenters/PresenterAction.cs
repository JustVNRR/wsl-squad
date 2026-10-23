using wsl_squad.Presentation.Workflows;

namespace wsl_squad.Presentation.Cli.Presenters;

public sealed record PresenterAction(
    string Key,
    string Label,
    Func<string, IWorkflowResult> Execute);

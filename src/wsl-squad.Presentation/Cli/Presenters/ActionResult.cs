namespace wsl_squad.Presentation.Cli.Presenters;

public enum ActionResultStatus
{
    Ok,
    Nok,
}

public sealed record ActionResult(ActionResultStatus Status, string Message)
{
    public static ActionResult Ok(string message)
    {
        return new ActionResult(ActionResultStatus.Ok, message);
    }

    public static ActionResult Nok(string message)
    {
        return new ActionResult(ActionResultStatus.Nok, message);
    }
}

public sealed record PresenterAction(
    string Key,
    string Label,
    Func<string, ActionResult> Execute);

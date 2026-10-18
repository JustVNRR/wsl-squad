namespace wsl_squad.Presentation.Presenters;

/// <summary>Outcome of a presenter run (reference <c>ActionResult.psm1</c>).</summary>
public enum ActionResultStatus
{
    Ok,
    Nok,
}

/// <summary>The message returned to the caller of a presenter.</summary>
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

/// <summary>A menu action offered by a presenter (reference action hashtable).</summary>
public sealed record PresenterAction(
    string Key,
    string Label,
    Func<string, ActionResult> Execute);

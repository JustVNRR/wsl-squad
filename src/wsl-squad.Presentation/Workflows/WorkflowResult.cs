namespace wsl_squad.Presentation.Workflows;

public enum WorkflowStatus
{
    Success,
    Cancelled,
    NoTarget,
    Failed,
}

public interface IWorkflowResult
{
    WorkflowStatus Status { get; }

    string Subject { get; }

    string Message { get; }
}

public sealed record WorkflowOutcome(
    WorkflowStatus Status,
    string Subject,
    string Message) : IWorkflowResult;

public sealed record WorkflowResult<T>(
    WorkflowStatus Status,
    string Subject,
    string Message,
    T? Response) : IWorkflowResult;

public static class WorkflowResults
{
    public static WorkflowResult<T> Success<T>(string subject, T response)
    {
        return new WorkflowResult<T>(WorkflowStatus.Success, subject, string.Empty, response);
    }

    public static WorkflowResult<T> Cancelled<T>(string subject)
    {
        return new WorkflowResult<T>(WorkflowStatus.Cancelled, subject, string.Empty, default);
    }

    public static WorkflowResult<T> NoTarget<T>(string message)
    {
        return new WorkflowResult<T>(WorkflowStatus.NoTarget, string.Empty, message, default);
    }

    public static WorkflowResult<T> Failed<T>(string subject, string message)
    {
        return new WorkflowResult<T>(WorkflowStatus.Failed, subject, message, default);
    }

    public static WorkflowOutcome Success(string subject)
    {
        return new WorkflowOutcome(WorkflowStatus.Success, subject, string.Empty);
    }

    public static WorkflowOutcome Cancelled(string subject)
    {
        return new WorkflowOutcome(WorkflowStatus.Cancelled, subject, string.Empty);
    }

    public static WorkflowOutcome Failed(string subject, string message)
    {
        return new WorkflowOutcome(WorkflowStatus.Failed, subject, message);
    }
}

using wsl_squad.Presentation.Cli.Presenters;

namespace wsl_squad.Presentation.Workflows;

public static class WorkflowResultExtensions
{
    public static ActionResult ToActionResult<T>(
        this WorkflowResult<T> result,
        string operation,
        Action<T>? onSuccess = null,
        Action<string>? onError = null,
        string noTargetReason = "no instance")
    {
        switch (result.Status)
        {
            case WorkflowStatus.Success:
                if (result.Response is not null)
                {
                    onSuccess?.Invoke(result.Response);
                }

                return ActionResult.Ok($"{operation} : {result.Subject}");
            case WorkflowStatus.Cancelled:
                return ActionResult.Ok($"{operation} : cancelled");
            case WorkflowStatus.NoTarget:
                return ActionResult.Nok($"{operation} : {noTargetReason}");
            case WorkflowStatus.Failed:
                if (!string.IsNullOrEmpty(result.Message))
                {
                    onError?.Invoke(result.Message);
                }

                string detail = (result.Subject, result.Message) switch
                {
                    ({ Length: > 0 } subject, { Length: > 0 } message) => $"{subject}\n{message}",
                    ({ Length: > 0 } subject, _) => subject,
                    (_, { Length: > 0 } message) => message,
                    _ => "failed",
                };

                return ActionResult.Nok($"{operation} : {detail}");
            default:
                return ActionResult.Nok($"{operation} : failed");
        }
    }
}

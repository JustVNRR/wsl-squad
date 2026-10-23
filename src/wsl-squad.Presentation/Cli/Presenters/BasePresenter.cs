using wsl_squad.Presentation.Cli.Ports;
using wsl_squad.Presentation.Workflows;

namespace wsl_squad.Presentation.Cli.Presenters;

public abstract class BasePresenter<TView>
    where TView : IPresenterViewBase
{
    protected BasePresenter(TView view)
    {
        View = view;
    }

    protected TView View { get; }

    protected void ShowFailure(string message)
    {
        View.ShowError(message);
    }

    protected IWorkflowResult Finish<T>(WorkflowResult<T> result, Action<T>? onSuccess = null)
    {
        if (result.Status == WorkflowStatus.Success && result.Response is not null)
        {
            onSuccess?.Invoke(result.Response);
        }
        else if (result.Status == WorkflowStatus.Failed && !string.IsNullOrEmpty(result.Message))
        {
            View.ShowError(result.Message);
        }

        return result;
    }
}

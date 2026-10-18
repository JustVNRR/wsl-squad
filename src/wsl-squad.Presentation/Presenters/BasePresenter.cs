using wsl_squad.Presentation.Presenters.Views;

namespace wsl_squad.Presentation.Presenters;

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
}

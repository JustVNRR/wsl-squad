namespace wsl_squad.Presentation.Presenters.Views;

public interface IPresenterViewBase
{
    void ShowError(string message);

    bool? Confirm(string question, bool defaultNo = true);

    void ShowCancelled(string noun);
}

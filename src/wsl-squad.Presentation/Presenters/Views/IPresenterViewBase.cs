namespace wsl_squad.Presentation.Presenters.Views;

/// <summary>Every presenter view can show a failure.</summary>
public interface IPresenterViewBase
{
    public void ShowError(string message);
}

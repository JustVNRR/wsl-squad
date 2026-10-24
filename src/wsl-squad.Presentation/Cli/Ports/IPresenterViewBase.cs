namespace WslSquad.Presentation.Cli.Ports;

public interface IPresenterViewBase
{
    void ShowError(string message);

    bool? Confirm(string question, bool defaultNo = true);

    void ShowCancelled(string noun);
}

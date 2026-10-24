using CommunityToolkit.Mvvm.ComponentModel;

namespace WslSquad.Presentation.Gui.ViewModels;

public abstract class BaseViewModel : ObservableObject
{
    public event EventHandler? CloseRequested;

    protected void RequestClose()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}

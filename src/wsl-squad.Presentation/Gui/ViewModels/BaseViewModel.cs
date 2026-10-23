using CommunityToolkit.Mvvm.ComponentModel;

namespace wsl_squad.Presentation.Gui.ViewModels;

public abstract class BaseViewModel : ObservableObject
{
    public event EventHandler? CloseRequested;

    protected void RequestClose()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}

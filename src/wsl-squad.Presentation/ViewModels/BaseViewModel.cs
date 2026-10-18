using CommunityToolkit.Mvvm.ComponentModel;

namespace wsl_squad.Presentation.ViewModels;

/// <summary>Base des ViewModels (référence <c>BaseViewModel.psm1</c>).</summary>
public abstract class BaseViewModel : ObservableObject
{
    public event EventHandler? CloseRequested;

    protected void RequestClose()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}

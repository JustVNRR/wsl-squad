using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WslSquad.Presentation.Gui.ViewModels;

public sealed partial class DockerRegisterViewModel : BaseViewModel
{
    [ObservableProperty]
    private bool _restart = true;

    public DockerRegisterViewModel(string name, string state)
    {
        Lead = state == "yes"
            ? $"Unregister '{name}' from Docker Desktop?".ToUpperInvariant()
            : $"Register '{name}' with Docker Desktop?".ToUpperInvariant();
        ConfirmCommand = new RelayCommand(Confirm);
    }

    public string Lead { get; }

    public bool? Result { get; private set; }

    public IRelayCommand ConfirmCommand { get; }

    private void Confirm()
    {
        Result = Restart;
        RequestClose();
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using wsl_squad.Core.Dtos;

namespace wsl_squad.Presentation.ViewModels;

public sealed partial class DockerRegisterViewModel : BaseViewModel
{
    private readonly string _name;

    [ObservableProperty]
    private bool _restart = true;

    public DockerRegisterViewModel(string name, string state)
    {
        _name = name;
        Lead = state == "yes"
            ? $"Unregister '{name}' from Docker Desktop?".ToUpperInvariant()
            : $"Register '{name}' with Docker Desktop?".ToUpperInvariant();
        ConfirmCommand = new RelayCommand(Confirm);
    }

    public string Lead { get; }

    public DockerDialogDto? Result { get; private set; }

    public IRelayCommand ConfirmCommand { get; }

    private void Confirm()
    {
        Result = new DockerDialogDto(_name, Restart);
        RequestClose();
    }
}

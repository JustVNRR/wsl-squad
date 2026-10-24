using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using WslSquad.Presentation.Localization;

namespace WslSquad.Presentation.Gui.ViewModels;

public sealed partial class DockerRegisterViewModel : BaseViewModel
{
    private string Name { get; }
    private bool Unregister { get; }

    [ObservableProperty]
    public partial bool Restart { get; set; }

    public DockerRegisterViewModel(string name, string state)
    {
        Name = name;
        Unregister = state == "yes";
        Restart = true;
        ConfirmCommand = new RelayCommand(Confirm);
    }

    public string Lead => Localizer.Instance.Format(
        Unregister ? "LeadUnregisterDocker" : "LeadRegisterDocker",
        Name).ToUpperInvariant();

    public bool? Result { get; private set; }

    public IRelayCommand ConfirmCommand { get; }

    private void Confirm()
    {
        Result = Restart;
        RequestClose();
    }
}

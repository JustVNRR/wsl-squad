using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using WslSquad.Core.Dtos;

using WslSquad.Presentation.Localization;

namespace WslSquad.Presentation.Gui.ViewModels;

public sealed partial class RemoveViewModel : BaseViewModel
{
    private readonly string _name;

    [ObservableProperty]
    public partial string Typed { get; set; }

    [ObservableProperty]
    public partial bool ArchiveFirst { get; set; }

    public RemoveViewModel(string name)
    {
        _name = name;
        ConfirmCommand = new RelayCommand(Confirm, CanConfirm);
        Typed = string.Empty;
    }

    public string Lead => Localizer.Instance.Format("LeadRemove", _name).ToUpperInvariant();

    public RemoveDto? Result { get; private set; }

    public IRelayCommand ConfirmCommand { get; }

    partial void OnTypedChanged(string value)
    {
        ConfirmCommand.NotifyCanExecuteChanged();
    }

    private bool CanConfirm()
    {
        return string.Equals(Typed, _name, StringComparison.OrdinalIgnoreCase);
    }

    private void Confirm()
    {
        if (!CanConfirm())
        {
            return;
        }

        Result = new RemoveDto { InstanceName = _name, ArchiveFirst = ArchiveFirst };
        RequestClose();
    }
}

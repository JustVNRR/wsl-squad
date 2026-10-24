using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using WslSquad.Presentation.Localization;

namespace WslSquad.Presentation.Gui.ViewModels;

public sealed partial class DeleteArchiveViewModel : BaseViewModel
{
    private readonly string _archiveName;

    [ObservableProperty]
    public partial string Typed { get; set; }

    public DeleteArchiveViewModel(string archiveName)
    {
        _archiveName = archiveName;
        ConfirmCommand = new RelayCommand(Confirm, CanConfirm);
        Typed = string.Empty;
    }

    public string Lead => Localizer.Instance.Format("LeadDeleteArchive", _archiveName).ToUpperInvariant();

    public string? Result { get; private set; }

    public IRelayCommand ConfirmCommand { get; }

    partial void OnTypedChanged(string value)
    {
        ConfirmCommand.NotifyCanExecuteChanged();
    }

    private bool CanConfirm()
    {
        return string.Equals(Typed, _archiveName, StringComparison.OrdinalIgnoreCase);
    }

    private void Confirm()
    {
        if (!CanConfirm())
        {
            return;
        }

        Result = _archiveName;
        RequestClose();
    }
}

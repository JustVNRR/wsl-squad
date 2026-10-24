using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WslSquad.Presentation.Gui.ViewModels;

public sealed partial class DeleteArchiveViewModel : BaseViewModel
{
    private readonly string _archiveName;

    [ObservableProperty]
    public partial string Typed { get; set; }

    public DeleteArchiveViewModel(string archiveName)
    {
        _archiveName = archiveName;
        Lead = $"The archive '{archiveName}' will be deleted and will not be restorable again.".ToUpperInvariant();
        ConfirmCommand = new RelayCommand(Confirm, CanConfirm);
        Typed = string.Empty;
    }

    public string Lead { get; }

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

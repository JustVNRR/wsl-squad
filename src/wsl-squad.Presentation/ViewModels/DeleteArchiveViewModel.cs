using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace wsl_squad.Presentation.ViewModels;

public sealed partial class DeleteArchiveViewModel : BaseViewModel
{
    private readonly string _archiveName;

    [ObservableProperty]
    private string _typed = string.Empty;

    public DeleteArchiveViewModel(string archiveName)
    {
        _archiveName = archiveName;
        Lead = $"The archive '{archiveName}' will be deleted and will not be restorable again.".ToUpperInvariant();
        ConfirmCommand = new RelayCommand(Confirm, CanConfirm);
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

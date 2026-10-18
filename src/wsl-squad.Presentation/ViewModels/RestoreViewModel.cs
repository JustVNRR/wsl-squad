using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using wsl_squad.Core.Dtos;

namespace wsl_squad.Presentation.ViewModels;

public sealed partial class RestoreViewModel : BaseViewModel
{
    private readonly string _archiveName;

    [ObservableProperty]
    private string _name;

    public RestoreViewModel(string archiveName)
    {
        _archiveName = archiveName;
        _name = archiveName;
        Lead = $"The archive '{archiveName}' comes back as a new instance.".ToUpperInvariant();
        ConfirmCommand = new RelayCommand(Confirm, CanConfirm);
    }

    public string Lead { get; }

    public RestoreDto? Result { get; private set; }

    public IRelayCommand ConfirmCommand { get; }

    partial void OnNameChanged(string value)
    {
        ConfirmCommand.NotifyCanExecuteChanged();
    }

    private bool CanConfirm()
    {
        return Name.Trim().Length > 0;
    }

    private void Confirm()
    {
        string wanted = Name.Trim();

        if (wanted.Length == 0)
        {
            return;
        }

        Result = new RestoreDto { ArchiveName = _archiveName, InstanceName = wanted };
        RequestClose();
    }
}

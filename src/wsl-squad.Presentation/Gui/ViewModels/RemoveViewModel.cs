using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using wsl_squad.Core.Dtos;

namespace wsl_squad.Presentation.Gui.ViewModels;

public sealed partial class RemoveViewModel : BaseViewModel
{
    private readonly string _name;

    [ObservableProperty]
    private string _typed = string.Empty;

    [ObservableProperty]
    private bool _archiveFirst;

    public RemoveViewModel(string name)
    {
        _name = name;
        Lead = $"The WSL distribution '{name}' and ALL its data will be deleted.".ToUpperInvariant();
        ConfirmCommand = new RelayCommand(Confirm, CanConfirm);
    }

    public string Lead { get; }

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

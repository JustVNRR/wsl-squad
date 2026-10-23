using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using wsl_squad.Core.Dtos;

namespace wsl_squad.Presentation.Gui.ViewModels;

public sealed partial class CompactViewModel : BaseViewModel
{
    private readonly string _name;

    [ObservableProperty]
    private bool _archiveFirst;

    public CompactViewModel(string name, bool replacesArchive)
    {
        _name = name;
        ReplacesArchive = replacesArchive;
        Lead = $"Compacting '{name}'.".ToUpperInvariant();
        ConfirmCommand = new RelayCommand(Confirm);
    }

    public string Lead { get; }

    public bool ReplacesArchive { get; }

    public CompactDto? Result { get; private set; }

    public IRelayCommand ConfirmCommand { get; }

    private void Confirm()
    {
        Result = new CompactDto { InstanceName = _name, ArchiveFirst = ArchiveFirst };
        RequestClose();
    }
}

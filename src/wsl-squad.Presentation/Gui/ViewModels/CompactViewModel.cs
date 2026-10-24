using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using WslSquad.Core.Dtos;
using WslSquad.Presentation.Localization;

namespace WslSquad.Presentation.Gui.ViewModels;

public sealed partial class CompactViewModel : BaseViewModel
{
    private readonly string _name;

    [ObservableProperty]
    public partial bool ArchiveFirst { get; set; }

    public CompactViewModel(string name, bool replacesArchive)
    {
        _name = name;
        ReplacesArchive = replacesArchive;
        ConfirmCommand = new RelayCommand(Confirm);
    }

    public string Lead => Localizer.Instance.Format("LeadCompact", _name).ToUpperInvariant();

    public bool ReplacesArchive { get; }

    public CompactDto? Result { get; private set; }

    public IRelayCommand ConfirmCommand { get; }

    private void Confirm()
    {
        Result = new CompactDto { InstanceName = _name, ArchiveFirst = ArchiveFirst };
        RequestClose();
    }
}

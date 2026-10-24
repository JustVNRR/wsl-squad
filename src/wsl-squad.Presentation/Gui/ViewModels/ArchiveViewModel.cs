using CommunityToolkit.Mvvm.Input;

using WslSquad.Core.Dtos;
using WslSquad.Presentation.Localization;

namespace WslSquad.Presentation.Gui.ViewModels;

public sealed class ArchiveViewModel : BaseViewModel
{
    private readonly string _name;

    public ArchiveViewModel(string name, bool replacesArchive)
    {
        _name = name;
        TakenVisible = replacesArchive;
        ConfirmCommand = new RelayCommand(Confirm);
    }

    public string Lead => Localizer.Instance.Format("LeadArchive", _name).ToUpperInvariant();

    public bool TakenVisible { get; }

    public ArchiveDto? Result { get; private set; }

    public IRelayCommand ConfirmCommand { get; }

    private void Confirm()
    {
        Result = new ArchiveDto
        {
            InstanceName = _name,
            Format = "tar.gz",
            Permit = true,
        };
        RequestClose();
    }
}

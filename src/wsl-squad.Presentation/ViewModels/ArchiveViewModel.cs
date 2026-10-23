using CommunityToolkit.Mvvm.Input;

using wsl_squad.Core.Dtos;

namespace wsl_squad.Presentation.ViewModels;

public sealed class ArchiveViewModel : BaseViewModel
{
    private readonly string _name;

    public ArchiveViewModel(string name, bool replacesArchive)
    {
        _name = name;
        TakenVisible = replacesArchive;
        Lead = $"Back up '{name}' as '{name}_archive'.".ToUpperInvariant();
        ConfirmCommand = new RelayCommand(Confirm);
    }

    public string Lead { get; }

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

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using wsl_squad.Core.Dtos;
using wsl_squad.Core.Validation;
using wsl_squad.Presentation.Localization;

namespace wsl_squad.Presentation.Gui.ViewModels;

public sealed partial class RestoreViewModel : BaseViewModel
{
    private readonly string _archiveName;
    private readonly IReadOnlyList<string> _taken;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _nameError = string.Empty;

    public RestoreViewModel(string archiveName, IReadOnlyList<string> taken)
    {
        _archiveName = archiveName;
        _taken = taken;
        _name = archiveName;
        Lead = $"The archive '{archiveName}' comes back as a new instance.".ToUpperInvariant();
        ConfirmCommand = new RelayCommand(Confirm, CanConfirm);
        ValidateName();
    }

    public string Lead { get; }

    public RestoreDto? Result { get; private set; }

    public IRelayCommand ConfirmCommand { get; }

    partial void OnNameChanged(string value)
    {
        ValidateName();
    }

    private void ValidateName()
    {
        string trimmed = Name.Trim();
        NameProblem problem = NameRules.ValidateInstanceName(trimmed, _taken);

        NameError = problem.ToMessage(trimmed) ?? string.Empty;
        ConfirmCommand.NotifyCanExecuteChanged();
    }

    private bool CanConfirm()
    {
        return NameError.Length == 0 && Name.Trim().Length > 0;
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

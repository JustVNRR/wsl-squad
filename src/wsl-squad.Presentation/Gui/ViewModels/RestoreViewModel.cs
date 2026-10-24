using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using WslSquad.Core.Dtos;
using WslSquad.Core.Validation;
using WslSquad.Presentation.Localization;

namespace WslSquad.Presentation.Gui.ViewModels;

public sealed partial class RestoreViewModel : BaseViewModel
{
    private readonly string _archiveName;
    private readonly IReadOnlyList<string> _taken;

    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial string NameError { get; set; }

    public RestoreViewModel(string archiveName, IReadOnlyList<string> taken)
    {
        _archiveName = archiveName;
        _taken = taken;
        NameError = string.Empty;
        ConfirmCommand = new RelayCommand(Confirm, CanConfirm);
        Name = archiveName;
    }

    public string Lead => Localizer.Instance.Format("LeadRestore", _archiveName).ToUpperInvariant();

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

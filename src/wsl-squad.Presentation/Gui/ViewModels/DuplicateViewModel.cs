using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using WslSquad.Core.Dtos;
using WslSquad.Core.Validation;
using WslSquad.Presentation.Localization;

namespace WslSquad.Presentation.Gui.ViewModels;

public sealed partial class DuplicateViewModel : BaseViewModel
{
    private readonly string _source;
    private readonly IReadOnlyList<string> _taken;
    private bool _nameOk;

    [ObservableProperty]
    public partial string Name { get; set; }

    [ObservableProperty]
    public partial string NameError { get; set; }

    public DuplicateViewModel(string source, IReadOnlyList<string> taken)
    {
        _source = source;
        _taken = taken;
        NameError = string.Empty;
        Lead = $"Duplicate '{source}'.".ToUpperInvariant();
        ConfirmCommand = new RelayCommand(Confirm, () => _nameOk);
        Name = $"{source}-copy";
    }

    public string Lead { get; }

    public DuplicateDto? Result { get; private set; }

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
        _nameOk = problem == NameProblem.None;
        ConfirmCommand.NotifyCanExecuteChanged();
    }

    private void Confirm()
    {
        string copyName = Name.Trim();

        if (copyName.Length == 0)
        {
            return;
        }

        Result = new DuplicateDto { SourceName = _source, CopyName = copyName };
        RequestClose();
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using wsl_squad.Core.Dtos;
using wsl_squad.Core.Validation;

namespace wsl_squad.Presentation.Gui.ViewModels;

public sealed partial class DuplicateViewModel : BaseViewModel
{
    private readonly string _source;
    private readonly IReadOnlyList<string> _taken;
    private bool _nameOk;

    [ObservableProperty]
    private string _name;

    [ObservableProperty]
    private string _nameError = string.Empty;

    public DuplicateViewModel(string source, IReadOnlyList<string> taken)
    {
        _source = source;
        _taken = taken;
        _name = $"{source}-copy";
        Lead = $"Duplicate '{source}'.".ToUpperInvariant();
        ConfirmCommand = new RelayCommand(Confirm, () => _nameOk);
        ValidateName();
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
        string problem = trimmed switch
        {
            { Length: 0 } => "Give a name.",
            _ when !NameRules.IsValid(trimmed) => "Letters, digits, '.', '_' and '-' only.",
            _ when _taken.Contains(trimmed, StringComparer.OrdinalIgnoreCase) =>
                $"An instance named '{trimmed}' already exists.",
            _ => string.Empty,
        };

        NameError = problem;
        _nameOk = problem.Length == 0;
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

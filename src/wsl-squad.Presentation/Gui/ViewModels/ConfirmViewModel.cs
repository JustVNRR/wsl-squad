using CommunityToolkit.Mvvm.Input;

namespace WslSquad.Presentation.Gui.ViewModels;

public sealed class ConfirmViewModel : BaseViewModel
{
    public ConfirmViewModel(string question, string? danger)
    {
        string[] lines = question.ReplaceLineEndings("\n").Split('\n');
        Lead = lines[0].Trim().ToUpperInvariant();
        Body = lines.Length > 1 ? string.Join('\n', lines.Skip(1)).Trim() : string.Empty;
        Danger = danger ?? string.Empty;
        ConfirmCommand = new RelayCommand(Confirm);
    }

    public string Lead { get; }

    public string Body { get; }

    public string Danger { get; }

    public IRelayCommand ConfirmCommand { get; }

    private void Confirm()
    {
        RequestClose();
    }
}

using CommunityToolkit.Mvvm.Input;

namespace WslSquad.Presentation.Gui.ViewModels;

public sealed class ConfirmViewModel : BaseViewModel
{
    public ConfirmViewModel(string title, string question, string? danger)
    {
        Title = title;
        Question = question;
        string[] lines = question.ReplaceLineEndings("\n").Split('\n');
        Lead = lines[0].Trim().ToUpperInvariant();
        Body = lines.Length > 1 ? string.Join('\n', lines.Skip(1)).Trim() : string.Empty;
        Danger = danger ?? string.Empty;
        ConfirmCommand = new RelayCommand(Confirm);
    }

    public string Title { get; }

    public string Question { get; }

    public string Lead { get; }

    public string Body { get; }

    public string Danger { get; }

    public bool? Result { get; private set; }

    public IRelayCommand ConfirmCommand { get; }

    private void Confirm()
    {
        Result = true;
        RequestClose();
    }
}

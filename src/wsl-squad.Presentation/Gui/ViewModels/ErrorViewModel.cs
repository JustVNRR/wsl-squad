using WslSquad.Core.Dtos;

namespace WslSquad.Presentation.Gui.ViewModels;

public sealed class ErrorViewModel : BaseViewModel
{
    public ErrorViewModel(LogEntryDto entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        Lead = entry.Message ?? string.Empty;
        Stamp = $"{entry.When:yyyy-MM-dd HH:mm:ss} [{entry.Level}]";
    }

    public string Lead { get; }

    public string Stamp { get; }
}

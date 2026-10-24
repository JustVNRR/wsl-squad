using System.Windows;

using Microsoft.Win32;

using WslSquad.Core.Dtos;
using WslSquad.Presentation.Gui.Ports;
using WslSquad.Presentation.Gui.ViewModels;
using WslSquad.UI.Wpf.Views;
using WslSquad.UI.Wpf.Views.Dialogs;
using WslSquad.UI.Wpf.Views.Instance;

namespace WslSquad.UI.Wpf.Services;

public sealed class WpfDialogs : IDialogService
{
    private readonly Stack<Window> _stack = new();

    private Window? ActiveOwner =>
        _stack.Count > 0 ? _stack.Peek() : Application.Current?.MainWindow;

    private bool? ShowDialog(Window dialog)
    {
        ArgumentNullException.ThrowIfNull(dialog);

        Window? owner = ActiveOwner;

        if (owner is not null && !ReferenceEquals(owner, dialog) && owner.IsVisible)
        {
            dialog.Owner = owner;
        }

        _stack.Push(dialog);

        try
        {
            return dialog.ShowDialog();
        }
        finally
        {
            _stack.Pop();
        }
    }

    public bool Confirm(string title, string question, string? danger = null)
    {
        ConfirmViewModel viewModel = new(question, danger);

        return ShowDialog(new ConfirmWindow(viewModel)) == true;
    }

    public string? PickOpenFile(string title, string filter)
    {
        OpenFileDialog dialog = new()
        {
            Title = title,
            Filter = filter,
        };

        return ShowFileDialog(dialog);
    }

    public string? PickSaveFile(string title, string defaultFileName, string filter = "All files (*.*)|*.*")
    {
        SaveFileDialog dialog = new()
        {
            Title = title,
            FileName = defaultFileName,
            Filter = filter,
        };

        return ShowFileDialog(dialog);
    }

    public void ShowError(LogEntryDto entry)
    {
        ErrorViewModel viewModel = new(entry);
        ShowDialog(new ErrorWindow(viewModel));
    }

    public string? ShowRecipeName()
    {
        RecipeNameViewModel viewModel = new();
        ShowDialog(new RecipeNameWindow(viewModel));

        return viewModel.Result;
    }

    public (string Name, string Description)? ShowPackDuplicate(string source, string blurb, IReadOnlyList<string> taken)
    {
        PackDuplicateViewModel viewModel = new(source, blurb, taken);
        ShowDialog(new PackDuplicateWindow(viewModel));

        return viewModel.Result;
    }

    public string? ShowRootMove(string current)
    {
        RootMoveViewModel viewModel = new(current);
        ShowDialog(new RootMoveWindow(viewModel));

        return viewModel.Result;
    }

    public BuildDto? ShowBuild(BuildViewModel viewModel)
    {
        ShowDialog(new BuildWindow(viewModel));

        return viewModel.Result;
    }

    public SettingsDto? ShowSettings(SettingsViewModel viewModel)
    {
        ShowDialog(new SettingsWindow(viewModel));

        return viewModel.Result;
    }

    public ArchiveDto? ShowArchive(ArchiveViewModel viewModel)
    {
        ShowDialog(new ArchiveWindow(viewModel));

        return viewModel.Result;
    }

    public CompactDto? ShowCompact(CompactViewModel viewModel)
    {
        ShowDialog(new CompactWindow(viewModel));

        return viewModel.Result;
    }

    public RemoveDto? ShowRemove(RemoveViewModel viewModel)
    {
        ShowDialog(new RemoveWindow(viewModel));

        return viewModel.Result;
    }

    public RestoreDto? ShowRestore(RestoreViewModel viewModel)
    {
        ShowDialog(new RestoreWindow(viewModel));

        return viewModel.Result;
    }

    public DuplicateDto? ShowDuplicate(DuplicateViewModel viewModel)
    {
        ShowDialog(new DuplicateWindow(viewModel));

        return viewModel.Result;
    }

    public string? ShowDeleteArchive(DeleteArchiveViewModel viewModel)
    {
        ShowDialog(new DeleteArchiveWindow(viewModel));

        return viewModel.Result;
    }

    public bool? ShowDockerRegister(DockerRegisterViewModel viewModel)
    {
        ShowDialog(new DockerRegisterWindow(viewModel));

        return viewModel.Result;
    }

    public SetAppearanceDto? ShowSetAppearance(SetAppearanceViewModel viewModel)
    {
        ShowDialog(new AppearanceWindow(viewModel));

        return viewModel.Result;
    }

    private string? ShowFileDialog(FileDialog dialog)
    {
        Window? owner = ActiveOwner;
        bool? accepted = owner is not null ? dialog.ShowDialog(owner) : dialog.ShowDialog();

        return accepted == true ? dialog.FileName : null;
    }
}

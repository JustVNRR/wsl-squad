using WslSquad.Core.Dtos;
using WslSquad.Presentation.Gui.Ports;
using WslSquad.Presentation.Gui.ViewModels;
using WslSquad.UI.Wpf.Views;
using WslSquad.UI.Wpf.Views.Dialogs;
using WslSquad.UI.Wpf.Views.Instance;

namespace WslSquad.UI.Wpf.Services;

public sealed class WpfDialogs : IDialogService
{
    public bool Confirm(string title, string question, string? danger = null)
    {
        ConfirmViewModel viewModel = new(question, danger);

        return DialogHost.Show(new ConfirmWindow(viewModel)) == true;
    }

    public void ShowError(LogEntryDto entry)
    {
        ErrorViewModel viewModel = new(entry);
        _ = DialogHost.Show(new ErrorWindow(viewModel));
    }

    public string? ShowRecipeName()
    {
        RecipeNameViewModel viewModel = new();
        _ = DialogHost.Show(new RecipeNameWindow(viewModel));

        return viewModel.Result;
    }

    public (string Name, string Description)? ShowPackDuplicate(string source, string blurb, IReadOnlyList<string> taken)
    {
        PackDuplicateViewModel viewModel = new(source, blurb, taken);
        _ = DialogHost.Show(new PackDuplicateWindow(viewModel));

        return viewModel.Result;
    }

    public string? ShowRootMove(string current)
    {
        RootMoveViewModel viewModel = new(current);
        _ = DialogHost.Show(new RootMoveWindow(viewModel));

        return viewModel.Result;
    }

    public BuildDto? ShowBuild(BuildViewModel viewModel)
    {
        _ = DialogHost.Show(new BuildWindow(viewModel));

        return viewModel.Result;
    }

    public SettingsDto? ShowSettings(SettingsViewModel viewModel)
    {
        _ = DialogHost.Show(new SettingsWindow(viewModel));

        return viewModel.Result;
    }

    public ArchiveDto? ShowArchive(ArchiveViewModel viewModel)
    {
        _ = DialogHost.Show(new ArchiveWindow(viewModel));

        return viewModel.Result;
    }

    public CompactDto? ShowCompact(CompactViewModel viewModel)
    {
        _ = DialogHost.Show(new CompactWindow(viewModel));

        return viewModel.Result;
    }

    public RemoveDto? ShowRemove(RemoveViewModel viewModel)
    {
        _ = DialogHost.Show(new RemoveWindow(viewModel));

        return viewModel.Result;
    }

    public RestoreDto? ShowRestore(RestoreViewModel viewModel)
    {
        _ = DialogHost.Show(new RestoreWindow(viewModel));

        return viewModel.Result;
    }

    public DuplicateDto? ShowDuplicate(DuplicateViewModel viewModel)
    {
        _ = DialogHost.Show(new DuplicateWindow(viewModel));

        return viewModel.Result;
    }

    public string? ShowDeleteArchive(DeleteArchiveViewModel viewModel)
    {
        _ = DialogHost.Show(new DeleteArchiveWindow(viewModel));

        return viewModel.Result;
    }

    public bool? ShowDockerRegister(DockerRegisterViewModel viewModel)
    {
        _ = DialogHost.Show(new DockerRegisterWindow(viewModel));

        return viewModel.Result;
    }

    public SetAppearanceDto? ShowSetAppearance(SetAppearanceViewModel viewModel)
    {
        _ = DialogHost.Show(new AppearanceWindow(viewModel));

        return viewModel.Result;
    }
}

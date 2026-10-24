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
        DialogHost.Show(new ErrorWindow(viewModel));
    }

    public string? ShowRecipeName()
    {
        RecipeNameViewModel viewModel = new();
        DialogHost.Show(new RecipeNameWindow(viewModel));

        return viewModel.Result;
    }

    public (string Name, string Description)? ShowPackDuplicate(string source, string blurb, IReadOnlyList<string> taken)
    {
        PackDuplicateViewModel viewModel = new(source, blurb, taken);
        DialogHost.Show(new PackDuplicateWindow(viewModel));

        return viewModel.Result;
    }

    public string? ShowRootMove(string current)
    {
        RootMoveViewModel viewModel = new(current);
        DialogHost.Show(new RootMoveWindow(viewModel));

        return viewModel.Result;
    }

    public BuildDto? ShowBuild(BuildViewModel viewModel)
    {
        DialogHost.Show(new BuildWindow(viewModel));

        return viewModel.Result;
    }

    public SettingsDto? ShowSettings(SettingsViewModel viewModel)
    {
        DialogHost.Show(new SettingsWindow(viewModel));

        return viewModel.Result;
    }

    public ArchiveDto? ShowArchive(ArchiveViewModel viewModel)
    {
        DialogHost.Show(new ArchiveWindow(viewModel));

        return viewModel.Result;
    }

    public CompactDto? ShowCompact(CompactViewModel viewModel)
    {
        DialogHost.Show(new CompactWindow(viewModel));

        return viewModel.Result;
    }

    public RemoveDto? ShowRemove(RemoveViewModel viewModel)
    {
        DialogHost.Show(new RemoveWindow(viewModel));

        return viewModel.Result;
    }

    public RestoreDto? ShowRestore(RestoreViewModel viewModel)
    {
        DialogHost.Show(new RestoreWindow(viewModel));

        return viewModel.Result;
    }

    public DuplicateDto? ShowDuplicate(DuplicateViewModel viewModel)
    {
        DialogHost.Show(new DuplicateWindow(viewModel));

        return viewModel.Result;
    }

    public string? ShowDeleteArchive(DeleteArchiveViewModel viewModel)
    {
        DialogHost.Show(new DeleteArchiveWindow(viewModel));

        return viewModel.Result;
    }

    public bool? ShowDockerRegister(DockerRegisterViewModel viewModel)
    {
        DialogHost.Show(new DockerRegisterWindow(viewModel));

        return viewModel.Result;
    }

    public SetAppearanceDto? ShowSetAppearance(SetAppearanceViewModel viewModel)
    {
        DialogHost.Show(new AppearanceWindow(viewModel));

        return viewModel.Result;
    }
}

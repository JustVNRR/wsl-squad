using WslSquad.Core.Dtos;
using WslSquad.Presentation.Gui.Ports;
using WslSquad.Presentation.Gui.ViewModels;

namespace WslSquad.Presentation.Tests.Support;

public sealed class TestDialogService : IDialogService
{
    public bool ConfirmAnswer { get; set; }

    public (string Title, string Question, string? Danger)? LastConfirm { get; private set; }

    public string? RecipeNameAnswer { get; set; }

    public (string Name, string Description)? PackDuplicateAnswer { get; set; }

    public string? RootMoveAnswer { get; set; }

    public BuildDto? BuildAnswer { get; set; }

    public SettingsDto? SettingsAnswer { get; set; }

    public ArchiveDto? ArchiveAnswer { get; set; }

    public CompactDto? CompactAnswer { get; set; }

    public RemoveDto? RemoveAnswer { get; set; }

    public RestoreDto? RestoreAnswer { get; set; }

    public DuplicateDto? DuplicateAnswer { get; set; }

    public string? DeleteArchiveAnswer { get; set; }

    public bool? DockerRegisterAnswer { get; set; }

    public SetAppearanceDto? SetAppearanceAnswer { get; set; }

    public List<LogEntryDto> Errors { get; } = [];

    public bool Confirm(string title, string question, string? danger = null)
    {
        LastConfirm = (title, question, danger);

        return ConfirmAnswer;
    }

    public void ShowError(LogEntryDto entry)
    {
        Errors.Add(entry);
    }

    public string? ShowRecipeName()
    {
        return RecipeNameAnswer;
    }

    public (string Name, string Description)? ShowPackDuplicate(string source, string blurb, IReadOnlyList<string> taken)
    {
        return PackDuplicateAnswer;
    }

    public string? ShowRootMove(string current)
    {
        return RootMoveAnswer;
    }

    public BuildDto? ShowBuild(BuildViewModel viewModel)
    {
        return BuildAnswer;
    }

    public SettingsDto? ShowSettings(SettingsViewModel viewModel)
    {
        return SettingsAnswer;
    }

    public ArchiveDto? ShowArchive(ArchiveViewModel viewModel)
    {
        return ArchiveAnswer;
    }

    public CompactDto? ShowCompact(CompactViewModel viewModel)
    {
        return CompactAnswer;
    }

    public RemoveDto? ShowRemove(RemoveViewModel viewModel)
    {
        return RemoveAnswer;
    }

    public RestoreDto? ShowRestore(RestoreViewModel viewModel)
    {
        return RestoreAnswer;
    }

    public DuplicateDto? ShowDuplicate(DuplicateViewModel viewModel)
    {
        return DuplicateAnswer;
    }

    public string? ShowDeleteArchive(DeleteArchiveViewModel viewModel)
    {
        return DeleteArchiveAnswer;
    }

    public bool? ShowDockerRegister(DockerRegisterViewModel viewModel)
    {
        return DockerRegisterAnswer;
    }

    public SetAppearanceDto? ShowSetAppearance(SetAppearanceViewModel viewModel)
    {
        return SetAppearanceAnswer;
    }
}

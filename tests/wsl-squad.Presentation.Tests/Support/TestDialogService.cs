using wsl_squad.Core.Dtos;
using wsl_squad.Presentation.Abstractions;
using wsl_squad.Presentation.ViewModels;

namespace wsl_squad.Presentation.Tests.Support;

public sealed class TestDialogService : IDialogService
{
    public bool ConfirmAnswer { get; set; }

    public (string Title, string Question, string? Danger)? LastConfirm { get; private set; }

    public string? RecipeNameAnswer { get; set; }

    public PackDuplicateResult? PackDuplicateAnswer { get; set; }

    public string? RootMoveAnswer { get; set; }

    public BuildDto? BuildAnswer { get; set; }

    public BuildDto? BuildTerminal { get; private set; }

    public int BuildTerminalExitCode { get; set; }

    public SettingsOutcome? SettingsAnswer { get; set; }

    public ManagePacksDto? ManagePacksAnswer { get; set; }

    public (string InstanceName, IReadOnlyList<string> ToAdd, IReadOnlyList<string> ToRemove)? PackTerminal { get; private set; }

    public ArchiveDto? ArchiveAnswer { get; set; }

    public CompactDto? CompactAnswer { get; set; }

    public RemoveDto? RemoveAnswer { get; set; }

    public RestoreDto? RestoreAnswer { get; set; }

    public DuplicateDto? DuplicateAnswer { get; set; }

    public string? DeleteArchiveAnswer { get; set; }

    public DockerDialogResult? DockerRegisterAnswer { get; set; }

    public SetAppearanceDto? SetAppearanceAnswer { get; set; }

    public List<LogEntryDto> Errors { get; } = [];

    public List<string> OpenedPaths { get; } = [];

    public bool Confirm(string title, string question, string? danger = null)
    {
        LastConfirm = (title, question, danger);

        return ConfirmAnswer;
    }

    public void ShowError(LogEntryDto entry)
    {
        Errors.Add(entry);
    }

    public void OpenPath(string path)
    {
        OpenedPaths.Add(path);
    }

    public string? ShowRecipeName()
    {
        return RecipeNameAnswer;
    }

    public PackDuplicateResult? ShowPackDuplicate(string source, string blurb, IReadOnlyList<string> taken)
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

    public Task<int> ShowBuildTerminalAsync(BuildDto request)
    {
        BuildTerminal = request;

        return Task.FromResult(BuildTerminalExitCode);
    }

    public SettingsOutcome? ShowSettings(SettingsViewModel viewModel)
    {
        return SettingsAnswer;
    }

    public ManagePacksDto? ShowManagePacks(ManagePacksViewModel viewModel)
    {
        return ManagePacksAnswer;
    }

    public void ShowPackTerminal(string instanceName, IReadOnlyList<string> toAdd, IReadOnlyList<string> toRemove)
    {
        PackTerminal = (instanceName, [.. toAdd], [.. toRemove]);
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

    public DockerDialogResult? ShowDockerRegister(DockerRegisterViewModel viewModel)
    {
        return DockerRegisterAnswer;
    }

    public SetAppearanceDto? ShowSetAppearance(SetAppearanceViewModel viewModel)
    {
        return SetAppearanceAnswer;
    }
}

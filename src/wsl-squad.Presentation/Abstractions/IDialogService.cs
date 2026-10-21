using wsl_squad.Core.Dtos;
using wsl_squad.Presentation.ViewModels;

namespace wsl_squad.Presentation.Abstractions;

public interface IDialogService
{
    public bool Confirm(string title, string question, string? danger = null);

    public void ShowError(LogEntryDto entry);

    public void OpenPath(string path);

    public string? ShowRecipeName();

    public PackDuplicateResult? ShowPackDuplicate(string source, string blurb, IReadOnlyList<string> taken);

    public string? ShowRootMove(string current);

    public BuildDto? ShowBuild(BuildViewModel viewModel);

    public Task<int> ShowBuildTerminalAsync(BuildDto request);

    public SettingsOutcome? ShowSettings(SettingsViewModel viewModel);

    public ArchiveDto? ShowArchive(ArchiveViewModel viewModel);

    public CompactDto? ShowCompact(CompactViewModel viewModel);

    public RemoveDto? ShowRemove(RemoveViewModel viewModel);

    public RestoreDto? ShowRestore(RestoreViewModel viewModel);

    public DuplicateDto? ShowDuplicate(DuplicateViewModel viewModel);

    public string? ShowDeleteArchive(DeleteArchiveViewModel viewModel);

    public DockerDialogResult? ShowDockerRegister(DockerRegisterViewModel viewModel);

    public SetAppearanceDto? ShowSetAppearance(SetAppearanceViewModel viewModel);
}

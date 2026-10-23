using wsl_squad.Core.Dtos;
using wsl_squad.Presentation.ViewModels;

namespace wsl_squad.Presentation.Abstractions;

public interface IDialogService
{
    bool Confirm(string title, string question, string? danger = null);

    void ShowError(LogEntryDto entry);

    string? ShowRecipeName();

    (string Name, string Description)? ShowPackDuplicate(string source, string blurb, IReadOnlyList<string> taken);

    string? ShowRootMove(string current);

    BuildDto? ShowBuild(BuildViewModel viewModel);

    SettingsDto? ShowSettings(SettingsViewModel viewModel);

    ArchiveDto? ShowArchive(ArchiveViewModel viewModel);

    CompactDto? ShowCompact(CompactViewModel viewModel);

    RemoveDto? ShowRemove(RemoveViewModel viewModel);

    RestoreDto? ShowRestore(RestoreViewModel viewModel);

    DuplicateDto? ShowDuplicate(DuplicateViewModel viewModel);

    string? ShowDeleteArchive(DeleteArchiveViewModel viewModel);

    bool? ShowDockerRegister(DockerRegisterViewModel viewModel);

    SetAppearanceDto? ShowSetAppearance(SetAppearanceViewModel viewModel);
}

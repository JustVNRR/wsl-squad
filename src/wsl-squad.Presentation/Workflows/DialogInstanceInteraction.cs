using wsl_squad.Core.Dtos;
using wsl_squad.Presentation.Abstractions;
using wsl_squad.Presentation.ViewModels;

namespace wsl_squad.Presentation.Workflows;

public sealed class DialogInstanceInteraction : IInstanceInteraction, IAppearanceInteraction
{
    private readonly IDialogService _dialogs;

    public DialogInstanceInteraction(IDialogService dialogs)
    {
        _dialogs = dialogs;
    }

    public TargetResolution SelectInstance(string? target, IReadOnlyList<WslIndexDto> rows)
    {
        return string.IsNullOrEmpty(target)
            ? new TargetResolution(TargetResolutionStatus.None, string.Empty)
            : new TargetResolution(TargetResolutionStatus.Selected, target);
    }

    public TargetResolution SelectArchive(string? target, IReadOnlyList<WslIndexDto> rows)
    {
        return SelectInstance(target, rows);
    }

    public bool PromptRestart(string instanceName)
    {
        return _dialogs.Confirm("Restart", $"Restart '{instanceName}'?");
    }

    public ArchiveDto? PromptArchive(string instanceName, bool replacesArchive)
    {
        return _dialogs.ShowArchive(new ArchiveViewModel(instanceName, replacesArchive));
    }

    public CompactDto? PromptCompact(string instanceName, bool replacesArchive)
    {
        return _dialogs.ShowCompact(new CompactViewModel(instanceName, replacesArchive));
    }

    public DuplicateDto? PromptDuplicate(string instanceName, IReadOnlyList<string> takenNames)
    {
        return _dialogs.ShowDuplicate(new DuplicateViewModel(instanceName, takenNames));
    }

    public RemoveDto? PromptRemove(string instanceName, bool replacesArchive)
    {
        return _dialogs.ShowRemove(new RemoveViewModel(instanceName));
    }

    public RestoreDto? PromptRestore(string archiveName, IReadOnlyList<string> takenNames)
    {
        return _dialogs.ShowRestore(new RestoreViewModel(archiveName, takenNames));
    }

    public bool PromptDeleteArchive(string archiveName)
    {
        return _dialogs.ShowDeleteArchive(new DeleteArchiveViewModel(archiveName)) is not null;
    }

    public void ShowDockerProblem(string instanceName, DockerProblem problem)
    {
    }

    public bool? PromptDocker(string instanceName, bool shouldRegister)
    {
        string state = shouldRegister ? "no" : "yes";

        return _dialogs.ShowDockerRegister(new DockerRegisterViewModel(instanceName, state));
    }

    public IReadOnlyList<SetAppearanceDto> PromptAppearance(
        string instanceName,
        AppearanceContextDto context)
    {
        SetAppearanceViewModel viewModel = new(
            instanceName,
            context.Appearance,
            context.Fonts,
            context.ColorSchemes,
            context.IconPairs);

        SetAppearanceDto? request = _dialogs.ShowSetAppearance(viewModel);

        return request is null ? [] : [request];
    }
}

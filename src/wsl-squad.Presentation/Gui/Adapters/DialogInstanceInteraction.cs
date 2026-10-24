using WslSquad.Core.Dtos;
using WslSquad.Presentation.Gui.Ports;
using WslSquad.Presentation.Localization;
using WslSquad.Presentation.Gui.ViewModels;
using WslSquad.Presentation.Workflows;

namespace WslSquad.Presentation.Gui.Adapters;

public sealed class DialogInstanceInteraction : IInstanceInteraction, IAppearanceInteraction
{
    private readonly IDialogService _dialogs;

    public DialogInstanceInteraction(IDialogService dialogs)
    {
        _dialogs = dialogs;
    }

    public string? PickInstance(IReadOnlyList<WslIndexDto> candidates)
    {
        return null;
    }

    public string? PickArchive(IReadOnlyList<WslIndexDto> archives)
    {
        return null;
    }

    public bool PromptRestart(string instanceName)
    {
        return _dialogs.Confirm(
            Localizer.Instance["ConfirmRestartTitle"],
            Localizer.Instance.Format("ConfirmRestart", instanceName));
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
            context.IconPairs,
            _dialogs);

        SetAppearanceDto? request = _dialogs.ShowSetAppearance(viewModel);

        return request is null ? [] : [request];
    }
}

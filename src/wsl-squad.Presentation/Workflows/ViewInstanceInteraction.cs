using wsl_squad.Core.Dtos;
using wsl_squad.Core.Validation;
using wsl_squad.Presentation.Presenters.Views;

namespace wsl_squad.Presentation.Workflows;

public sealed class ViewInstanceInteraction : IInstanceInteraction
{
    private readonly IInstanceView _view;

    public ViewInstanceInteraction(IInstanceView view)
    {
        _view = view;
    }

    public TargetResolution SelectInstance(string? target, IReadOnlyList<WslIndexDto> rows)
    {
        if (!string.IsNullOrEmpty(target))
        {
            return new TargetResolution(TargetResolutionStatus.Selected, target);
        }

        List<WslIndexDto> instances = [.. rows.Where(row => !row.IsArchive)];

        if (instances.Count == 0)
        {
            _view.ShowNoInstances();

            return new TargetResolution(TargetResolutionStatus.None, string.Empty);
        }

        string? picked = _view.AskOffered(instances);

        return string.IsNullOrEmpty(picked)
            ? new TargetResolution(TargetResolutionStatus.Cancelled, string.Empty)
            : new TargetResolution(TargetResolutionStatus.Selected, picked);
    }

    public TargetResolution SelectArchive(string? target, IReadOnlyList<WslIndexDto> rows)
    {
        if (!string.IsNullOrEmpty(target))
        {
            return new TargetResolution(TargetResolutionStatus.Selected, target);
        }

        List<WslIndexDto> archives = [.. rows.Where(row => row.IsArchive)];

        if (archives.Count == 0)
        {
            _view.ShowNoArchives();

            return new TargetResolution(TargetResolutionStatus.None, string.Empty);
        }

        string? picked = _view.AskArchive(archives);

        return string.IsNullOrEmpty(picked)
            ? new TargetResolution(TargetResolutionStatus.Cancelled, string.Empty)
            : new TargetResolution(TargetResolutionStatus.Selected, picked);
    }

    public bool PromptRestart(string instanceName)
    {
        _view.ShowStoppingWarning(instanceName);

        return _view.AskRestart() == true;
    }

    public ArchiveDto? PromptArchive(string instanceName, bool replacesArchive)
    {
        _view.ShowStopWarning(instanceName);

        if (_view.Confirm("Continue?", defaultNo: false) != true)
        {
            return null;
        }

        if (replacesArchive)
        {
            _view.ShowReplacing($"{instanceName}_archive");
        }

        _view.ShowBackingUp(instanceName);

        return new ArchiveDto
        {
            InstanceName = instanceName,
            Format = "tar.gz",
            Permit = true,
        };
    }

    public CompactDto? PromptCompact(string instanceName, bool replacesArchive)
    {
        _view.ShowBeforeQuestions();
        bool? archiveFirst = _view.Confirm("Archive it first?", defaultNo: false);

        if (archiveFirst is null)
        {
            return null;
        }

        if (archiveFirst == false)
        {
            _view.ShowNoArchive();
        }
        else if (replacesArchive)
        {
            _view.ShowReplacingArchive(instanceName);
        }

        _view.ShowCompacting(instanceName, archiveFirst.Value);

        return new CompactDto
        {
            InstanceName = instanceName,
            ArchiveFirst = archiveFirst.Value,
        };
    }

    public DuplicateDto? PromptDuplicate(string instanceName, IReadOnlyList<string> takenNames)
    {
        string copy;

        while (true)
        {
            string? answer = _view.AskCopyName();

            if (string.IsNullOrEmpty(answer))
            {
                return null;
            }

            if (NameRules.IsValid(answer) &&
                !takenNames.Contains(answer, StringComparer.OrdinalIgnoreCase))
            {
                copy = answer;

                break;
            }

            _view.ShowNameHint();
        }

        _view.ShowDiskWarning(instanceName);

        if (_view.Confirm("Continue?", defaultNo: false) != true)
        {
            return null;
        }

        _view.ShowDuplicating(instanceName, copy);

        return new DuplicateDto
        {
            SourceName = instanceName,
            CopyName = copy,
        };
    }

    public RemoveDto? PromptRemove(string instanceName, bool replacesArchive)
    {

        if (!_view.ConfirmDestruction(instanceName))
        {
            return null;
        }

        bool? archiveFirst = _view.Confirm("Archive it before deleting?", defaultNo: true);

        if (archiveFirst is null)
        {
            return null;
        }

        if (archiveFirst == true && replacesArchive)
        {
            _view.ShowReplacingArchive(instanceName);
        }

        _view.ShowRemovingSteps();

        return new RemoveDto
        {
            InstanceName = instanceName,
            ArchiveFirst = archiveFirst.Value,
        };
    }

    public RestoreDto? PromptRestore(string archiveName, IReadOnlyList<string> takenNames)
    {
        _view.ShowRestoring(archiveName);
        string? name = _view.AskNewName();

        while (!string.IsNullOrEmpty(name))
        {
            string trimmed = name.Trim();

            if (NameRules.IsValid(trimmed) &&
                !takenNames.Contains(trimmed, StringComparer.OrdinalIgnoreCase))
            {
                return new RestoreDto
                {
                    ArchiveName = archiveName,
                    InstanceName = trimmed,
                };
            }

            _view.ShowNameHint();
            name = _view.AskNewName();
        }

        return null;
    }

    public bool PromptDeleteArchive(string archiveName)
    {

        return _view.Confirm(
            $"Delete the archive '{archiveName}'? It cannot be restored.",
            defaultNo: true) == true;
    }

    public void ShowDockerProblem(string instanceName, DockerProblem problem)
    {

        switch (problem)
        {
            case DockerProblem.AlreadyRegistered:
                _view.ShowAlreadyRegistered(instanceName);

                break;
            case DockerProblem.NotRegistered:
                _view.ShowNotRegistered(instanceName);

                break;
            case DockerProblem.Unknown:
                _view.ShowSettingsUnreadable();

                break;
            case DockerProblem.NoCandidates:
                _view.ShowNoCandidates();

                break;
            default:
                break;
        }
    }

    public bool? PromptDocker(string instanceName, bool shouldRegister)
    {
        return _view.AskDockerAction(instanceName, shouldRegister);
    }
}

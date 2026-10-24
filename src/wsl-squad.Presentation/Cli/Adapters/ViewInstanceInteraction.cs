using WslSquad.Core.Dtos;
using WslSquad.Core.Validation;
using WslSquad.Presentation.Cli.Ports;
using WslSquad.Presentation.Workflows;
using WslSquad.Presentation.Localization;

namespace WslSquad.Presentation.Cli.Adapters;

public sealed class ViewInstanceInteraction : IInstanceInteraction
{
    private readonly IInstanceView _view;

    public ViewInstanceInteraction(IInstanceView view)
    {
        _view = view;
    }

    public string? PickInstance(IReadOnlyList<WslIndexDto> candidates)
    {
        return _view.AskOffered(candidates);
    }

    public string? PickArchive(IReadOnlyList<WslIndexDto> archives)
    {
        return _view.AskArchive(archives);
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

            string trimmed = answer.Trim();
            NameProblem problem = NameRules.ValidateInstanceName(trimmed, takenNames);

            if (problem != NameProblem.None)
            {
                _view.ShowNameProblem(problem.ToMessage(trimmed)!);

                continue;
            }

            copy = trimmed;

            break;
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

            NameProblem problem = NameRules.ValidateInstanceName(trimmed, takenNames);

            if (problem != NameProblem.None)
            {
                _view.ShowNameProblem(problem.ToMessage(trimmed)!);
                name = _view.AskNewName();

                continue;
            }

            return new RestoreDto
            {
                ArchiveName = archiveName,
                InstanceName = trimmed,
            };
        }

        return null;
    }

    public bool PromptDeleteArchive(string archiveName)
    {
        return _view.Confirm(
            $"Delete the archive '{archiveName}'? It cannot be restored.",
            defaultNo: true) == true;
    }

    public bool? PromptDocker(string instanceName, bool shouldRegister)
    {
        return _view.AskDockerAction(instanceName, shouldRegister);
    }
}

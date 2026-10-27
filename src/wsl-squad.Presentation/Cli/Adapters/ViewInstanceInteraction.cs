using WslSquad.Core.Dtos;
using WslSquad.Core.Validation;
using WslSquad.Presentation.Cli.Ports;
using WslSquad.Presentation.Workflows;
using WslSquad.Presentation.Localization;

namespace WslSquad.Presentation.Cli.Adapters;

public sealed class ViewInstanceInteraction : IInstanceInteraction
{
    private readonly IInstanceView _view;
    private readonly IAppearanceView? _appearanceView;

    public ViewInstanceInteraction(IInstanceView view)
    {
        _view = view;
    }

    public ViewInstanceInteraction(IInstanceView view, IAppearanceView appearanceView)
    {
        _view = view;
        _appearanceView = appearanceView;
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

        if (_view.Confirm(Localizer.Instance["CliContinue"], defaultNo: false) != true)
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
        bool? archiveFirst = _view.Confirm(Localizer.Instance["CliArchiveFirstQuestion"], defaultNo: false);

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

        if (_view.Confirm(Localizer.Instance["CliContinue"], defaultNo: false) != true)
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

        bool? archiveFirst = _view.Confirm(Localizer.Instance["CliArchiveBeforeDelete"], defaultNo: true);

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
            Localizer.Instance.Format("CliDeleteArchiveQuestion", archiveName),
            defaultNo: true) == true;
    }

    public bool? PromptDocker(string instanceName, bool shouldRegister)
    {
        return _view.AskDockerAction(instanceName, shouldRegister);
    }

    public IReadOnlyList<SetAppearanceDto> PromptAppearance(
        string instanceName,
        AppearanceContextDto context)
    {
        if (_appearanceView is null)
        {
            throw new InvalidOperationException("No appearance view is available.");
        }

        GetAppearanceResponseDto appearance = context.Appearance;
        List<SetAppearanceDto> requests = [];
        string font = appearance.Font ?? string.Empty;
        string scheme = appearance.ColourScheme ?? string.Empty;
        WslIconDto recipe = new()
        {
            Text = appearance.Icon?.Text ?? string.Empty,
            Top = appearance.Icon?.Top ?? string.Empty,
            Bottom = appearance.Icon?.Bottom ?? string.Empty,
            TextColor = appearance.Icon?.TextColor ?? string.Empty,
        };

        while (true)
        {
            _appearanceView.Clear();
            string? chosen = _appearanceView.AskTheme(instanceName);

            if (chosen is null)
            {
                return requests;
            }

            string? newFont = null;
            string? newScheme = null;
            WslIconDto? newIcon = null;
            string? newImage = null;
            bool changed = false;

            if (chosen == "font")
            {
                _appearanceView.Clear();
                string? picked = _appearanceView.AskFont(instanceName, context.Fonts, font);

                if (!string.IsNullOrEmpty(picked))
                {
                    newFont = picked;
                    font = picked;
                    changed = true;
                }
            }
            else if (chosen == "color")
            {
                _appearanceView.Clear();
                string? picked = _appearanceView.AskColour(instanceName, context.ColorSchemes, scheme);

                if (!string.IsNullOrEmpty(picked))
                {
                    newScheme = picked;
                    scheme = picked;
                    changed = true;
                }
            }
            else
            {
                while (true)
                {
                    _appearanceView.Clear();
                    string? how = _appearanceView.AskIconType(instanceName);

                    if (how is null)
                    {
                        break;
                    }

                    if (how == "auto")
                    {
                        recipe = new WslIconDto
                        {
                            Text = string.Empty,
                            Top = string.Empty,
                            Bottom = string.Empty,
                            TextColor = string.Empty,
                        };
                        newIcon = recipe;
                        changed = true;

                        break;
                    }

                    if (how == "letters")
                    {
                        _appearanceView.Clear();
                        string suggestion = recipe.Text ?? string.Empty;
                        string question = suggestion.Length > 0
                            ? Localizer.Instance.Format("CliLettersQuestion", suggestion)
                            : Localizer.Instance["CliLetters"];
                        bool settled = false;

                        while (true)
                        {
                            string? answer = _appearanceView.AskLetters(question);

                            if (answer is null)
                            {
                                break;
                            }

                            if (string.IsNullOrWhiteSpace(answer))
                            {
                                if (suggestion.Length > 0)
                                {
                                    recipe = recipe with { Text = suggestion.ToUpperInvariant() };
                                    settled = true;
                                }

                                break;
                            }

                            string trimmed = answer.Trim();

                            if (trimmed.Length is >= 1 and <= 3 &&
                                trimmed.All(char.IsAsciiLetterOrDigit))
                            {
                                recipe = recipe with { Text = trimmed.ToUpperInvariant() };
                                settled = true;

                                break;
                            }

                            _appearanceView.ShowLettersHint();
                        }

                        if (!settled)
                        {
                            continue;
                        }

                        newIcon = recipe;
                        changed = true;

                        break;
                    }

                    if (how == "colours")
                    {
                        _appearanceView.Clear();
                        WslIconPairDto? pair = _appearanceView.AskIconPair(
                            instanceName,
                            context.IconPairs,
                            recipe.Text ?? string.Empty,
                            recipe.Top,
                            recipe.Bottom);

                        if (pair is null)
                        {
                            continue;
                        }

                        recipe = recipe with
                        {
                            Top = pair.Top ?? string.Empty,
                            Bottom = pair.Bottom ?? string.Empty,
                            TextColor = pair.TextColor ?? string.Empty,
                        };
                        newIcon = recipe;
                        changed = true;

                        break;
                    }

                    bool imageSettled = false;
                    _appearanceView.Clear();

                    while (true)
                    {
                        string? answer = _appearanceView.AskImagePath();

                        if (answer is null)
                        {
                            break;
                        }

                        if (!File.Exists(answer))
                        {
                            _appearanceView.ShowNoFile();

                            continue;
                        }

                        string extension = Path.GetExtension(answer).ToLowerInvariant();

                        if (extension is not ".png" and not ".jpg" and not ".jpeg" and not ".ico" and not ".bmp")
                        {
                            _appearanceView.ShowBadExtension();

                            continue;
                        }

                        newImage = Path.GetFullPath(answer);
                        imageSettled = true;

                        break;
                    }

                    if (imageSettled)
                    {
                        changed = true;

                        break;
                    }
                }
            }

            if (!changed)
            {
                continue;
            }

            requests.Add(new SetAppearanceDto
            {
                InstanceName = instanceName,
                Font = newFont,
                ColourScheme = newScheme,
                Icon = newIcon,
                IconImage = newImage,
            });
        }
    }
}

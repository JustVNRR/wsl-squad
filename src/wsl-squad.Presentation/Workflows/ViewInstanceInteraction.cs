using wsl_squad.Core.Dtos;
using wsl_squad.Core.Validation;
using wsl_squad.Presentation.Presenters.Views;

namespace wsl_squad.Presentation.Workflows;

public sealed class ViewInstanceInteraction : IInstanceInteraction
{
    private readonly object _view;
    private readonly IInstancePickerView _picker;

    public ViewInstanceInteraction(object view)
    {
        _view = view;
        _picker = view as IInstancePickerView
            ?? throw new ArgumentException("The view must implement IInstancePickerView.", nameof(view));
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
            _picker.ShowNoInstances();

            return new TargetResolution(TargetResolutionStatus.None, string.Empty);
        }

        string? picked = _picker.AskOffered(instances);

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

        if (_view is not IArchivePickerView picker)
        {
            throw new InvalidOperationException("The view cannot select an archive.");
        }

        List<WslIndexDto> archives = [.. rows.Where(row => row.IsArchive)];

        if (archives.Count == 0)
        {
            picker.ShowNoArchives();

            return new TargetResolution(TargetResolutionStatus.None, string.Empty);
        }

        string? picked = picker.AskArchive(archives);

        return string.IsNullOrEmpty(picked)
            ? new TargetResolution(TargetResolutionStatus.Cancelled, string.Empty)
            : new TargetResolution(TargetResolutionStatus.Selected, picked);
    }

    public bool PromptRestart(string instanceName)
    {
        IInstanceRestartView view = Cast<IInstanceRestartView>();
        view.ShowStoppingWarning(instanceName);

        return view.AskRestart() == true;
    }

    public ArchiveDto? PromptArchive(string instanceName, bool replacesArchive)
    {
        IInstanceArchiveView view = Cast<IInstanceArchiveView>();
        view.ShowStopWarning(instanceName);

        if (view.Confirm("Continue?", defaultNo: false) != true)
        {
            return null;
        }

        if (replacesArchive)
        {
            view.ShowReplacing($"{instanceName}_archive");
        }

        view.ShowBackingUp(instanceName);

        return new ArchiveDto
        {
            InstanceName = instanceName,
            Format = "tar.gz",
            Permit = true,
        };
    }

    public CompactDto? PromptCompact(string instanceName, bool replacesArchive)
    {
        IInstanceCompactView view = Cast<IInstanceCompactView>();
        view.ShowBeforeQuestions();
        bool? archiveFirst = view.Confirm("Archive it first?", defaultNo: false);

        if (archiveFirst is null)
        {
            return null;
        }

        if (archiveFirst == false)
        {
            view.ShowNoArchive();
        }
        else if (replacesArchive)
        {
            view.ShowReplacingArchive(instanceName);
        }

        view.ShowCompacting(instanceName, archiveFirst.Value);

        return new CompactDto
        {
            InstanceName = instanceName,
            ArchiveFirst = archiveFirst.Value,
        };
    }

    public DuplicateDto? PromptDuplicate(string instanceName, IReadOnlyList<string> takenNames)
    {
        IInstanceDuplicateView view = Cast<IInstanceDuplicateView>();
        string copy;

        while (true)
        {
            string? answer = view.AskCopyName();

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

            view.ShowNameHint();
        }

        view.ShowDiskWarning(instanceName);

        if (view.Confirm("Continue?", defaultNo: false) != true)
        {
            return null;
        }

        view.ShowDuplicating(instanceName, copy);

        return new DuplicateDto
        {
            SourceName = instanceName,
            CopyName = copy,
        };
    }

    public RemoveDto? PromptRemove(string instanceName, bool replacesArchive)
    {
        IInstanceRemoveView view = Cast<IInstanceRemoveView>();

        if (!view.ConfirmDestruction(instanceName))
        {
            return null;
        }

        bool? archiveFirst = view.Confirm("Archive it before deleting?", defaultNo: true);

        if (archiveFirst is null)
        {
            return null;
        }

        if (archiveFirst == true && replacesArchive)
        {
            view.ShowReplacingArchive(instanceName);
        }

        view.ShowRemovingSteps();

        return new RemoveDto
        {
            InstanceName = instanceName,
            ArchiveFirst = archiveFirst.Value,
        };
    }

    public RestoreDto? PromptRestore(string archiveName, IReadOnlyList<string> takenNames)
    {
        IInstanceRestoreView view = Cast<IInstanceRestoreView>();
        view.ShowRestoring(archiveName);
        string? name = view.AskNewName();

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

            view.ShowNameHint();
            name = view.AskNewName();
        }

        return null;
    }

    public bool PromptDeleteArchive(string archiveName)
    {
        IInstanceDeleteArchiveView view = Cast<IInstanceDeleteArchiveView>();

        return view.Confirm(
            $"Delete the archive '{archiveName}'? It cannot be restored.",
            defaultNo: true) == true;
    }

    public void ShowDockerProblem(string instanceName, DockerProblem problem)
    {
        IInstanceDockerView view = Cast<IInstanceDockerView>();

        switch (problem)
        {
            case DockerProblem.AlreadyRegistered:
                view.ShowAlreadyRegistered(instanceName);

                break;
            case DockerProblem.NotRegistered:
                view.ShowNotRegistered(instanceName);

                break;
            case DockerProblem.Unknown:
                view.ShowSettingsUnreadable();

                break;
            case DockerProblem.NoCandidates:
                view.ShowNoCandidates();

                break;
            default:
                break;
        }
    }

    public bool? PromptDocker(string instanceName, bool shouldRegister)
    {
        return Cast<IInstanceDockerView>().AskDockerAction(instanceName, shouldRegister);
    }

    public IReadOnlyList<SetAppearanceDto> PromptAppearance(
        string instanceName,
        AppearanceContextDto context)
    {
        IAppearanceView view = Cast<IAppearanceView>();
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
            string? chosen = view.AskTheme(instanceName);

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
                string? picked = view.AskFont(instanceName, context.Fonts, font);

                if (!string.IsNullOrEmpty(picked))
                {
                    newFont = picked;
                    font = picked;
                    changed = true;
                }
            }
            else if (chosen == "color")
            {
                string? picked = view.AskColour(instanceName, context.ColorSchemes, scheme);

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
                    string? how = view.AskIconType(instanceName);

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
                        string suggestion = recipe.Text ?? string.Empty;
                        string question = suggestion.Length > 0
                            ? $"Letters, 1 to 3 [{suggestion}]"
                            : "Letters, 1 to 3";
                        bool settled = false;

                        while (true)
                        {
                            string? answer = view.AskLetters(question);

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

                            view.ShowLettersHint();
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
                        WslIconPairDto? pair = view.AskIconPair(
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

                    while (true)
                    {
                        string? answer = view.AskImagePath();

                        if (answer is null)
                        {
                            break;
                        }

                        if (!File.Exists(answer))
                        {
                            view.ShowNoFile();

                            continue;
                        }

                        string extension = Path.GetExtension(answer).ToLowerInvariant();

                        if (extension is not ".png" and not ".jpg" and not ".jpeg" and not ".ico" and not ".bmp")
                        {
                            view.ShowBadExtension();

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

    private TView Cast<TView>()
        where TView : class
    {
        return _view as TView
            ?? throw new InvalidOperationException($"The view does not implement {typeof(TView).Name}.");
    }
}

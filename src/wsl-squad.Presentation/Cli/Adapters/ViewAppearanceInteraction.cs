using wsl_squad.Core.Dtos;
using wsl_squad.Presentation.Cli.Ports;
using wsl_squad.Presentation.Workflows;

namespace wsl_squad.Presentation.Cli.Adapters;

public sealed class ViewAppearanceInteraction : IAppearanceInteraction
{
    private readonly IAppearanceView _view;

    public ViewAppearanceInteraction(IAppearanceView view)
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

    public IReadOnlyList<SetAppearanceDto> PromptAppearance(
        string instanceName,
        AppearanceContextDto context)
    {
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
            string? chosen = _view.AskTheme(instanceName);

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
                string? picked = _view.AskFont(instanceName, context.Fonts, font);

                if (!string.IsNullOrEmpty(picked))
                {
                    newFont = picked;
                    font = picked;
                    changed = true;
                }
            }
            else if (chosen == "color")
            {
                string? picked = _view.AskColour(instanceName, context.ColorSchemes, scheme);

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
                    string? how = _view.AskIconType(instanceName);

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
                            string? answer = _view.AskLetters(question);

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

                            _view.ShowLettersHint();
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
                        WslIconPairDto? pair = _view.AskIconPair(
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
                        string? answer = _view.AskImagePath();

                        if (answer is null)
                        {
                            break;
                        }

                        if (!File.Exists(answer))
                        {
                            _view.ShowNoFile();

                            continue;
                        }

                        string extension = Path.GetExtension(answer).ToLowerInvariant();

                        if (extension is not ".png" and not ".jpg" and not ".jpeg" and not ".ico" and not ".bmp")
                        {
                            _view.ShowBadExtension();

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

using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;

namespace wsl_squad.Presentation.Presenters.Instance;

public sealed class SetAppearancePresenter : BasePresenter<IAppearanceView>
{
    private readonly IInstanceService _service;
    private readonly SelectInstancePresenter _picker;

    public SetAppearancePresenter(IInstanceService service, IInstanceView instanceView, IAppearanceView view)
        : base(view)
    {
        _service = service;
        _picker = new SelectInstancePresenter(instanceView);
    }

    public ActionResult Run(string? target)
    {
        IReadOnlyList<WslIndexDto> rows;

        try
        {
            rows = _service.GetAll();
        }
        catch (InvalidOperationException exception)
        {
            ShowFailure(exception.Message);

            return ActionResult.Nok($"SetAppearance : {exception.Message}");
        }

        string name = _picker.Pick(target, rows);

        if (name.Length == 0)
        {
            return _picker.Miss == "none"
                ? ActionResult.Nok("SetAppearance : no instance")
                : ActionResult.Ok("SetAppearance : cancelled");
        }

        GetAppearanceResponseDto appearance;
        IReadOnlyList<string> fonts;
        IReadOnlyList<WslColorSchemeDto> schemes;
        IReadOnlyList<WslIconPairDto> pairs;

        try
        {
            appearance = _service.GetAppearance(name);
            List<string> catalogFonts = [.. _service.GetFonts()];

            if (!string.IsNullOrEmpty(appearance.Font) &&
                !catalogFonts.Contains(appearance.Font, StringComparer.Ordinal))
            {
                catalogFonts.Add(appearance.Font);
            }

            fonts = catalogFonts;
            schemes = _service.GetColorSchemes();
            pairs = _service.GetIconPairs();
        }
        catch (InvalidOperationException exception)
        {
            ShowFailure(exception.Message);

            return ActionResult.Nok($"SetAppearance : {name} - {exception.Message}");
        }

        if (!appearance.HasFragment)
        {
            View.ShowNoFragmentWarning();
        }

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
            string? chosen = View.AskTheme(name);

            if (chosen is null)
            {
                return ActionResult.Ok("SetAppearance : cancelled");
            }

            string? newFont = null;
            string? newScheme = null;
            WslIconDto? newIcon = null;
            string? newImage = null;
            bool changed = false;

            if (chosen == "font")
            {
                string? picked = View.AskFont(name, fonts, font);

                if (!string.IsNullOrEmpty(picked))
                {
                    newFont = picked;
                    font = picked;
                    changed = true;
                }
            }
            else if (chosen == "color")
            {
                string? picked = View.AskColour(name, schemes, scheme);

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
                    string? how = View.AskIconType(name);

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
                            string? answer = View.AskLetters(question);

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

                            if (trimmed.Length is >= 1 and <= 3 && trimmed.All(char.IsAsciiLetterOrDigit))
                            {
                                recipe = recipe with { Text = trimmed.ToUpperInvariant() };
                                settled = true;

                                break;
                            }

                            View.ShowLettersHint();
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
                        WslIconPairDto? pair = View.AskIconPair(
                            name,
                            pairs,
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
                        string? answer = View.AskImagePath();

                        if (answer is null)
                        {
                            break;
                        }

                        if (!File.Exists(answer))
                        {
                            View.ShowNoFile();

                            continue;
                        }

                        string extension = Path.GetExtension(answer).ToLowerInvariant();

                        if (extension is not ".png" and not ".jpg" and not ".jpeg" and not ".ico" and not ".bmp")
                        {
                            View.ShowBadExtension();

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

            SetAppearanceDto dto = new()
            {
                InstanceName = name,
                Font = newFont,
                ColourScheme = newScheme,
                Icon = newIcon,
                IconImage = newImage,
            };

            try
            {
                SetAppearanceResponseDto response = _service.SetAppearance(dto);
                View.Show(response);
            }
            catch (InvalidOperationException exception)
            {
                ShowFailure(exception.Message);

                return ActionResult.Nok($"SetAppearance : {name} - {exception.Message}");
            }
        }
    }
}

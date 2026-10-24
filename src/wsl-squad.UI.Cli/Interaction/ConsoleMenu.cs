using WslSquad.Presentation.Localization;

namespace WslSquad.UI.Cli.Interaction;

public sealed class ConsoleMenu
{
    private readonly List<ConsoleMenuItem> _items = [];
    private readonly ConsoleHost _console;

    public ConsoleMenu(string title, ConsoleHost console)
    {
        Title = title;
        _console = console;
    }

    public string Title { get; }

    public string Note { get; set; } = string.Empty;

    public bool Multi { get; set; }

    public int Current { get; set; }

    public IReadOnlyList<ConsoleMenuItem> Items => _items;

    public static string FormatRow(
        int index,
        int current,
        IReadOnlyList<string> labels,
        IReadOnlyList<bool>? checkedFlags)
    {
        string marker = index == current ? "  > " : "    ";

        if (checkedFlags is null)
        {
            return marker + labels[index];
        }

        string box = checkedFlags[index] ? "[x] " : "[ ] ";

        return marker + box + labels[index];
    }

    public static IReadOnlyList<string> Fit(IReadOnlyList<string> labels, int width, int prefix)
    {
        if (width <= 0)
        {
            return labels;
        }

        int room = width - 1;

        return
        [
            .. labels.Select(label =>
            {
                int max = room - prefix;

                if (label.Length <= max)
                {
                    return label;
                }

                int keep = Math.Max(1, max - 3);

                return label[..keep] + "...";
            }),
        ];
    }

    public ConsoleMenu Add(ConsoleMenuItem item)
    {
        _items.Add(item);
        Restyle();

        return this;
    }

    public ConsoleMenu Add(string key, string description)
    {
        return Add(new ConsoleMenuItem(key, description));
    }

    public ConsoleMenu Add(string key, string label, string description)
    {
        return Add(new ConsoleMenuItem(key, label, description));
    }

    public IReadOnlyList<ConsoleMenuItem>? Prompt()
    {
        if (_items.Count == 0)
        {
            return null;
        }

        return _console.HasKeyboard() ? PromptArrows() : PromptNumbers();
    }

    private void Restyle()
    {
        int width = Math.Max(
            12,
            _items
                .Where(item => !string.IsNullOrEmpty(item.Key))
                .Select(item => item.Label.Length)
                .DefaultIfEmpty(0)
                .Max());

        foreach (ConsoleMenuItem item in _items)
        {
            if (!string.IsNullOrEmpty(item.Key))
            {
                item.SetKeyWidth(width);
            }
        }
    }

    private IReadOnlyList<bool>? Flags()
    {
        return Multi ? [.. _items.Select(item => item.Checked)] : null;
    }

    private void DrawRow(int index, IReadOnlyList<string> texts)
    {
        ConsoleTone? tone = index == Current ? ConsoleTone.Info : null;
        _console.Line(FormatRow(index, Current, texts, Flags()), tone);
    }

    private IReadOnlyList<ConsoleMenuItem>? PromptArrows()
    {
        int count = _items.Count;

        if (Current < 0 || Current >= count)
        {
            Current = 0;
        }

        IReadOnlyList<string> texts = [.. _items.Select(item => item.Text)];
        string hint = Multi
            ? Localizer.Instance["CliMenuHintMulti"]
            : Localizer.Instance["CliMenuHintSingle"];
        (int width, int height) = _console.Size();
        int extra = string.IsNullOrEmpty(Note) ? 0 : 1;
        IReadOnlyList<string> shown = Fit(texts, width, Multi ? 8 : 4);
        string shownTitle = Fit([Title], width, 0)[0];
        string shownHint = Fit([hint], width, 0)[0];
        string shownNote = Fit([Note], width, 0)[0];
        int visible = height > 0 ? Math.Min(count, Math.Max(1, height - 3 - extra)) : count;
        int first = Math.Max(0, Math.Min(Current - ((visible - 1) / 2), count - visible));

        _console.Line(string.Empty);
        if (!string.IsNullOrEmpty(shownTitle))
        {
            _console.Line(shownTitle, ConsoleTone.Info);
        }

        for (int row = 0; row < visible; row++)
        {
            DrawRow(first + row, shown);
        }

        _console.Line(shownHint, ConsoleTone.Muted);
        if (!string.IsNullOrEmpty(shownNote))
        {
            _console.Line(shownNote, ConsoleTone.Muted);
        }

        int? cursor = _console.Top();
        int top = cursor is null ? 0 : cursor.Value - (visible + 1 + extra);
        bool canPaint = cursor is not null && top >= 0;
        (bool captured, bool prior) = ConsoleHost.CaptureControlC();

        try
        {
            while (true)
            {
                ConsoleKey key = _console.ReadKey();
                int last = count - 1;
                bool moved = true;

                if (key == ConsoleKey.UpArrow)
                {
                    Current = Current == 0 ? last : Current - 1;
                }
                else if (key == ConsoleKey.DownArrow)
                {
                    Current = Current == last ? 0 : Current + 1;
                }
                else if (key == ConsoleKey.Spacebar && Multi)
                {
                    _items[Current].Checked = !_items[Current].Checked;
                }
                else if (key == ConsoleKey.Enter)
                {
                    ReturnToBottom(canPaint, top, visible, extra);

                    return Multi ? Checked() : [_items[Current]];
                }
                else if (key == ConsoleKey.Escape)
                {
                    ReturnToBottom(canPaint, top, visible, extra);

                    return null;
                }
                else if (TryDigit(key, out int wanted))
                {
                    if (wanted < count)
                    {
                        if (Multi)
                        {
                            _items[wanted].Checked = !_items[wanted].Checked;
                        }
                        else
                        {
                            ReturnToBottom(canPaint, top, visible, extra);

                            return [_items[wanted]];
                        }
                    }
                }
                else
                {
                    moved = false;
                }

                if (!moved || !canPaint)
                {
                    continue;
                }

                if (Current < first)
                {
                    first = Current;
                }

                if (Current >= first + visible)
                {
                    first = Current - visible + 1;
                }

                bool placed = true;

                for (int row = 0; row < visible; row++)
                {
                    if (!_console.SetTop(top + row))
                    {
                        placed = false;

                        break;
                    }

                    DrawRow(first + row, shown);
                }

                if (!placed)
                {
                    for (int row = 0; row < visible; row++)
                    {
                        DrawRow(first + row, shown);
                    }
                }
            }
        }
        finally
        {
            if (captured)
            {
                ConsoleHost.RestoreControlC(prior);
            }
        }
    }

    private static bool TryDigit(ConsoleKey key, out int index)
    {
        if (key is >= ConsoleKey.D1 and <= ConsoleKey.D9)
        {
            index = (int)key - (int)ConsoleKey.D1;

            return true;
        }

        if (key is >= ConsoleKey.NumPad1 and <= ConsoleKey.NumPad9)
        {
            index = (int)key - (int)ConsoleKey.NumPad1;

            return true;
        }

        index = -1;

        return false;
    }

    private IReadOnlyList<ConsoleMenuItem>? PromptNumbers()
    {
        int count = _items.Count;
        IReadOnlyList<string> texts = [.. _items.Select(item => item.Text)];

        if (Multi)
        {
            while (true)
            {
                _console.Line(string.Empty);
                if (!string.IsNullOrEmpty(Title))
                {
                    _console.Line(Title, ConsoleTone.Info);
                }

                for (int index = 0; index < count; index++)
                {
                    string box = _items[index].Checked ? "[x]" : "[ ]";
                    _console.Line($"  {index + 1,2}.  {box} {texts[index]}");
                }

                _console.Line(Localizer.Instance["CliMenuCancel"]);
                if (!string.IsNullOrEmpty(Note))
                {
                    _console.Line($"  {Note}", ConsoleTone.Muted);
                }

                string? answer = _console.Prompt(Localizer.Instance["CliMenuNumberHint"]);

                if (string.IsNullOrWhiteSpace(answer) || answer.Trim() == "0")
                {
                    return null;
                }

                if (answer.Trim() is "v" or "V")
                {
                    return Checked();
                }

                if (int.TryParse(answer.Trim(), out int number) && number >= 1 && number <= count)
                {
                    _items[number - 1].Checked = !_items[number - 1].Checked;

                    continue;
                }

                _console.Line(Localizer.Instance.Format("CliMenuNotANumber", answer), ConsoleTone.Warning);
            }
        }

        _console.Line(string.Empty);
        if (!string.IsNullOrEmpty(Title))
        {
            _console.Line(Title, ConsoleTone.Info);
        }

        for (int index = 0; index < count; index++)
        {
            _console.Line($"  {index + 1,2}.  {texts[index]}");
        }

        _console.Line(Localizer.Instance["CliMenuCancel"]);
        if (!string.IsNullOrEmpty(Note))
        {
            _console.Line($"  {Note}", ConsoleTone.Muted);
        }

        while (true)
        {
            string? answer = _console.Prompt(Localizer.Instance["CliMenuWhich"]);

            if (string.IsNullOrWhiteSpace(answer))
            {
                return null;
            }

            if (int.TryParse(answer.Trim(), out int number))
            {
                if (number == 0)
                {
                    return null;
                }

                if (number >= 1 && number <= count)
                {
                    return [_items[number - 1]];
                }
            }

            _console.Line(Localizer.Instance.Format("CliMenuNotANumber", answer), ConsoleTone.Warning);
        }
    }

    private IReadOnlyList<ConsoleMenuItem> Checked()
    {
        return [.. _items.Where(item => item.Checked)];
    }

    private void ReturnToBottom(bool canPaint, int top, int visible, int extra)
    {
        if (canPaint)
        {
            _console.SetTop(top + visible + 1 + extra);
        }
    }
}

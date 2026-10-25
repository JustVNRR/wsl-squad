namespace WslSquad.UI.Cli.Interaction;

public sealed class ConsoleMenuItem
{
    public ConsoleMenuItem(string key, string description)
        : this(key, key, description)
    {
    }

    public ConsoleMenuItem(string key, string label, string description)
    {
        Key = key;
        Label = label;
        Description = description;
        SetKeyWidth(12);
    }

    public ConsoleMenuItem(string text, object? value)
    {
        Key = string.Empty;
        Label = string.Empty;
        Description = string.Empty;
        Text = text;
        Value = value;
    }

    public string Key { get; }

    public string Label { get; }

    public string Description { get; }

    public string Text { get; private set; } = string.Empty;

    public object? Value { get; }

    public void SetKeyWidth(int width)
    {
        int size = width < 1 ? 1 : width;
        Text = $"{Label.PadRight(size)} {Description}";
    }
}

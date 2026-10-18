namespace wsl_squad.UI.Cli.Interaction;

public class ConsoleTheme
{
    private readonly Dictionary<ConsoleTone, ConsoleColor> _fallback = new()
    {
        [ConsoleTone.Error] = ConsoleColor.Red,
        [ConsoleTone.Warning] = ConsoleColor.Yellow,
        [ConsoleTone.Success] = ConsoleColor.Green,
        [ConsoleTone.Info] = ConsoleColor.Cyan,
        [ConsoleTone.Muted] = ConsoleColor.DarkGray,
        [ConsoleTone.Hint] = ConsoleColor.White,
        [ConsoleTone.Danger] = ConsoleColor.DarkRed,
    };

    public ConsoleColor GetColor(ConsoleTone tone)
    {
        return _fallback.TryGetValue(tone, out ConsoleColor color) ? color : ConsoleColor.Gray;
    }

    public virtual (ConsoleColor Box, ConsoleColor Ink) GetBannerColors()
    {
        return (GetColor(ConsoleTone.Danger), ConsoleColor.White);
    }
}

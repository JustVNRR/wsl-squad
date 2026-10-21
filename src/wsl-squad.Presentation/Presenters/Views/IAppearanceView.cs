using wsl_squad.Core.Dtos;

namespace wsl_squad.Presentation.Presenters.Views;

public interface IAppearanceView : IPresenterViewBase
{
    public string? AskTheme(string name);

    public string? AskFont(string name, IReadOnlyList<string> fonts, string current);

    public string? AskColour(string name, IReadOnlyList<WslColorSchemeDto> schemes, string current);

    public string? AskIconType(string name);

    public string? AskLetters(string question);

    public void ShowLettersHint();

    public WslIconPairDto? AskIconPair(
        string name,
        IReadOnlyList<WslIconPairDto> pairs,
        string suggestion,
        string? top,
        string? bottom);

    public string? AskImagePath();

    public void ShowNoFile();

    public void ShowBadExtension();

    public void Show(SetAppearanceResponseDto response);
}

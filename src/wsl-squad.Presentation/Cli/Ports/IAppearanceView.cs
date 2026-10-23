using wsl_squad.Core.Dtos;

namespace wsl_squad.Presentation.Cli.Ports;

public interface IAppearanceView : IPresenterViewBase
{
    void ShowNoInstances();

    string? AskOffered(IReadOnlyList<WslIndexDto> instances);

    string? AskTheme(string name);

    string? AskFont(string name, IReadOnlyList<string> fonts, string current);

    string? AskColour(string name, IReadOnlyList<WslColorSchemeDto> schemes, string current);

    string? AskIconType(string name);

    string? AskLetters(string question);

    void ShowLettersHint();

    WslIconPairDto? AskIconPair(
        string name,
        IReadOnlyList<WslIconPairDto> pairs,
        string suggestion,
        string? top,
        string? bottom);

    string? AskImagePath();

    void ShowNoFile();

    void ShowBadExtension();

    void Show(SetAppearanceResponseDto response);
}

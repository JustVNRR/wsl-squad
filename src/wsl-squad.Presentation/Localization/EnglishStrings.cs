namespace WslSquad.Presentation.Localization;

internal static class EnglishStrings
{
    public static IReadOnlyDictionary<string, string> All { get; } =
        LanguageFile.ReadEmbeddedEnglish().Strings!;
}

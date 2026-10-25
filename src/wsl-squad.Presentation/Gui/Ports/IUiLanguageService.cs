namespace WslSquad.Presentation.Gui.Ports;

public interface IUiLanguageService
{
    string? LanguageCodeFor(string source);

    string UploadLanguage(string source);

    void DeleteLanguage(string code);

    void OpenLanguage(string code);

    void ExportLanguage(string code, string target);

    string LanguageFileFor(string code);
}

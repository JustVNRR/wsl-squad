using WslSquad.Presentation.Gui.Ports;

namespace WslSquad.Presentation.Tests.Support;

public sealed class TestLanguageService : IUiLanguageService
{
    public string? Code { get; set; } = "fr";

    public string? UploadedCode { get; set; } = "fr";

    public List<string> Uploaded { get; } = [];

    public List<string> Deleted { get; } = [];

    public List<string> Opened { get; } = [];

    public List<(string Code, string Target)> Exported { get; } = [];

    public string? LanguageCodeFor(string source)
    {
        return Code;
    }

    public string UploadLanguage(string source)
    {
        Uploaded.Add(source);

        return UploadedCode ?? "fr";
    }

    public void DeleteLanguage(string code)
    {
        Deleted.Add(code);
    }

    public void OpenLanguage(string code)
    {
        Opened.Add(code);
    }

    public void ExportLanguage(string code, string target)
    {
        Exported.Add((code, target));
    }

    public string LanguageFileFor(string code)
    {
        return $"{code}.json";
    }
}

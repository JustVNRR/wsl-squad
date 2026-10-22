namespace wsl_squad.Core.Repositories;

public interface IConfigRepository
{
    IReadOnlyList<string> GetThemeNames();

    IReadOnlyList<string> GetFontNames();

    IReadOnlyList<string> GetPromptFontNames();

    IReadOnlyList<SourceRow> GetDockerfiles();

    IReadOnlyList<SourceRow> GetOnboardings();

    IReadOnlyList<SourceRow> GetRecipes();

    string CreateRecipe(string name);

    string CopyRecipe(string kind, string source, string fileName);

    void RemoveRecipe(string path);

    void ExportRecipe(string path, string target);

    string CopyFont(string source);

    string CopyTheme(string source);

    void DeleteTheme(string name);

    void OpenTheme(string name);

    string GetSavedRoot();

    void SaveRoot(string root);

    string EnsureWslConfig();
}

namespace wsl_squad.Core.Repositories;

public interface IConfigRepository
{
    public IReadOnlyList<string> GetThemeNames();

    public IReadOnlyList<string> GetFontNames();

    public IReadOnlyList<string> GetPromptFontNames();

    public IReadOnlyList<SourceRow> GetDockerfiles();

    public IReadOnlyList<SourceRow> GetOnboardings();

    public IReadOnlyList<SourceRow> GetRecipes();

    public string CreateRecipe(string name);

    public string CopyRecipe(string kind, string source, string fileName);

    public void RemoveRecipe(string path);

    public void ExportRecipe(string path, string target);

    public string CopyFont(string source);

    public string CopyTheme(string source);

    public void DeleteTheme(string name);

    public void OpenTheme(string name);

    public string GetSavedRoot();

    public void SaveRoot(string root);

    public string EnsureWslConfig();
}

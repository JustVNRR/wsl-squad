namespace WslSquad.Core.Repositories;

public interface IConfigRepository
{
    IReadOnlyList<string> GetFontNames();

    IReadOnlyList<string> GetPromptFontNames();

    IReadOnlyList<SourceRow> GetDockerfiles();

    IReadOnlyList<SourceRow> GetOnboardings();

    IReadOnlyList<SourceRow> GetRecipes();

    string CreateRecipe(string name);

    string CopyRecipe(string kind, string source, string fileName);

    void RemoveRecipe(string path);

    void ExportRecipe(string path, string target);

    string GetSavedRoot();

    void SaveRoot(string root);

    string EnsureWslConfig();
}

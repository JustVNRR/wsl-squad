namespace WslSquad.Core.Repositories;

public interface IConfigRepository
{
    IReadOnlyList<string> GetFontNames();

    IReadOnlyList<string> GetPromptFontNames();

    IReadOnlyList<SourceRow> GetRecipes();

    string CreateRecipe(string name);

    void RemoveRecipe(string path);

    string GetSavedRoot();

    void SaveRoot(string root);

    string GetLanguage();

    void SaveLanguage(string code);

    string EnsureWslConfig();
}

namespace WslSquad.Core.Repositories;

public interface IDockerRepository
{
    void Build(string tag, string dockerfile, string context, bool interactive = false);

    string Load(string tarPath, bool interactive = false);

    void ExportRootfs(string imageName, string outputPath, bool interactive = false);

    bool SaveImage(string tag, string outputPath);

    bool RemoveImage(string tag);

    bool RestartDesktop();

    bool StartDesktop();

    bool TestRunning();

    string GetSavedImagePath(string name);

    IReadOnlyList<SavedImageRow> GetAllSavedImages();

    IReadOnlyList<string>? GetDesktopNames();

    string GetDesktopState(string instanceName);

    void SetDesktopState(string instanceName);

    void RemoveDesktopState(string instanceName);
}

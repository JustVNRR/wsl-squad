namespace wsl_squad.Core.Repositories;

public interface IDockerRepository
{
    public RegistryAnswer TestRegistryImage(string reference);

    public void Build(string tag, string dockerfile, string context, bool interactive = false);

    public string Load(string tarPath, bool interactive = false);

    public void ExportRootfs(string imageName, string outputPath, bool interactive = false);

    public bool SaveImage(string tag, string outputPath);

    public bool RemoveImage(string tag);

    public bool RestartDesktop();

    public bool StartDesktop();

    public bool TestRunning();

    public string GetSavedImagePath(string name);

    public IReadOnlyList<SavedImageRow> GetAllSavedImages();

    public IReadOnlyList<string>? GetDesktopNames();

    public string GetDesktopState(string instanceName);

    public void SetDesktopState(string instanceName);

    public void RemoveDesktopState(string instanceName);
}

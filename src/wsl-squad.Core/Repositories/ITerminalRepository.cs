using wsl_squad.Core.Dtos;
using wsl_squad.Core.Entities;

namespace wsl_squad.Core.Repositories;

public interface ITerminalRepository
{
    public string GetFragmentPath(string instanceName);

    public void SaveFragment(string instanceName, string profileGuid, WslTheme theme);

    public void DeleteFragment(string instanceName);

    public string GetProfileGuid(string instanceName);

    public bool HasWslProfile(string instanceName);

    public WslGuids GetWslGuids(string instanceName);

    public PruneReport PruneGhostEntries(string instanceName, IReadOnlyList<string> liveGuids);

    public IReadOnlyList<string> RemoveStaleFragments(IReadOnlyList<string> liveGuids);

    public void TouchSettings();

    public WslTheme GetAppearance(string instanceName);

    public IReadOnlyList<WslColorSchemeDto> GetColorSchemes();

    public bool IsFontInstalled(string faceName);

    public void InstallFont(string faceName, string? from);

    public IReadOnlyList<TerminalFont> GetTerminalFonts();

    public IconDrawing DrawIcon(string instanceName, string outputPath, IconDrawing? drawing);

    public IReadOnlyList<WslIconPairDto> GetIconPairs();
}

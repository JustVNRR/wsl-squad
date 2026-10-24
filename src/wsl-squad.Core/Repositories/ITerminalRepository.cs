using WslSquad.Core.Dtos;
using WslSquad.Core.Entities;

namespace WslSquad.Core.Repositories;

public interface ITerminalRepository
{
    string GetFragmentPath(string instanceName);

    void SaveFragment(string instanceName, string profileGuid, WslTheme theme);

    void DeleteFragment(string instanceName);

    string GetProfileGuid(string instanceName);

    bool HasWslProfile(string instanceName);

    WslGuids GetWslGuids(string instanceName);

    PruneReport PruneGhostEntries(string instanceName, IReadOnlyList<string> liveGuids);

    IReadOnlyList<string> RemoveStaleFragments(IReadOnlyList<string> liveGuids);

    void TouchSettings();

    WslTheme GetAppearance(string instanceName);

    IReadOnlyList<WslColorSchemeDto> GetColorSchemes();

    bool IsFontInstalled(string faceName);

    void InstallFont(string faceName, string? from);

    IReadOnlyList<TerminalFont> GetTerminalFonts();

    IconDrawing DrawIcon(string instanceName, string outputPath, IconDrawing? drawing);

    IReadOnlyList<WslIconPairDto> GetIconPairs();
}

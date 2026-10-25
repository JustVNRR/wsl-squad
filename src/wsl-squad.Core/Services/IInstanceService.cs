using WslSquad.Core.Dtos;

namespace WslSquad.Core.Services;

public interface IInstanceService
{
    IReadOnlyList<WslIndexDto> GetAll();

    IReadOnlyList<WslOrphanDto> GetOrphans();

    IReadOnlyList<string> GetRunningNames();

    IReadOnlyList<WslBuildSourceDto> GetRecipes();

    string? GetSavedRecipeImage(string recipeName);

    WslBuildSourceDto CreateRecipe(NewRecipeDto dto);

    WslBuildSourceDto DuplicateRecipe(DuplicateRecipeDto dto);

    void OpenInEditor(string path);

    RegisterDockerResponseDto RegisterDocker(RegisterDockerDto dto);

    UnregisterDockerResponseDto UnregisterDocker(UnregisterDockerDto dto);

    void DeleteRecipe(string path);

    BuildResponseDto Build(BuildDto dto);

    OpenResponseDto Open(string instanceName);

    RestartResponseDto Restart(string instanceName);

    void Remove(RemoveDto dto);

    CompactResponseDto Compact(CompactDto dto);

    IReadOnlyList<string> GetFonts();

    IReadOnlyList<WslIconPairDto> GetIconPairs();

    GetAppearanceResponseDto GetAppearance(string instanceName);

    SetAppearanceResponseDto SetAppearance(SetAppearanceDto dto);

    IReadOnlyList<WslColorSchemeDto> GetColorSchemes();

    void OpenPackManager(string instanceName);

    ArchiveResponseDto Archive(ArchiveDto dto);

    RestoreResponseDto Restore(RestoreDto dto);

    DeleteArchiveResponseDto DeleteArchive(string archive);

    DuplicateResponseDto Duplicate(DuplicateDto dto);
}

using wsl_squad.Core.Dtos;

namespace wsl_squad.Core.Services;

public interface IInstanceService
{
    IReadOnlyList<WslIndexDto> GetAll();

    IReadOnlyList<WslOrphanDto> GetOrphans();

    IReadOnlyList<string> GetRunningNames();

    GetBuildSourcesResponseDto GetBuildSources();

    WslBuildSourceDto CreateRecipe(NewRecipeDto dto);

    void OpenInEditor(string path);

    RegisterDockerResponseDto RegisterDocker(RegisterDockerDto dto);

    UnregisterDockerResponseDto UnregisterDocker(UnregisterDockerDto dto);

    WslBuildSourceDto UploadRecipe(UploadRecipeDto dto);

    void DeleteRecipe(string path);

    void ExportRecipe(ExportRecipeDto dto);

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

    IReadOnlyList<WslDockerImageDto> GetAllSavedImages();

    IsRegistryImageValidResponseDto IsRegistryImageValid(string imageRef);

    ArchiveResponseDto Archive(ArchiveDto dto);

    RestoreResponseDto Restore(RestoreDto dto);

    DeleteArchiveResponseDto DeleteArchive(string archive);

    DuplicateResponseDto Duplicate(DuplicateDto dto);
}

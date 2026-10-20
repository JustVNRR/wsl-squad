using wsl_squad.Core.Dtos;

namespace wsl_squad.Core.Services;

public interface IInstanceService
{
    public IReadOnlyList<WslIndexDto> GetAll();

    public WslIndexDto? GetByName(string name);

    public IReadOnlyList<WslOrphanDto> GetOrphans();

    public IReadOnlyList<string> GetRunningNames();

    public GetBuildSourcesResponseDto GetBuildSources();

    public WslBuildSourceDto CreateRecipe(NewRecipeDto dto);

    public void OpenInEditor(string path);

    public RegisterDockerResponseDto RegisterDocker(RegisterDockerDto dto);

    public UnregisterDockerResponseDto UnregisterDocker(UnregisterDockerDto dto);

    public WslBuildSourceDto UploadRecipe(UploadRecipeDto dto);

    public bool TestDocker();

    public bool StartDocker();

    public void DeleteRecipe(string path);

    public void ExportRecipe(ExportRecipeDto dto);

    public BuildResponseDto Build(BuildDto dto);

    public OpenResponseDto Open(string instanceName);

    public RestartResponseDto Restart(string instanceName);

    public void Remove(RemoveDto dto);

    public CompactResponseDto Compact(CompactDto dto);

    public IReadOnlyList<string> GetFonts();

    public IReadOnlyList<WslIconPairDto> GetIconPairs();

    public GetAppearanceResponseDto GetAppearance(string instanceName);

    public SetAppearanceResponseDto SetAppearance(SetAppearanceDto dto);

    public IReadOnlyList<WslColorSchemeDto> GetColorSchemes();

    public GetPacksResponseDto GetPacks(string instanceName);

    public ManagePacksResponseDto ManagePacks(ManagePacksDto dto);

    public IReadOnlyList<WslDockerImageDto> GetAllSavedImages();

    public IsRegistryImageValidResponseDto IsRegistryImageValid(string imageRef);

    public ArchiveResponseDto Archive(ArchiveDto dto);

    public RestoreResponseDto Restore(RestoreDto dto);

    public DeleteArchiveResponseDto DeleteArchive(string archive);

    public DuplicateResponseDto Duplicate(DuplicateDto dto);
}

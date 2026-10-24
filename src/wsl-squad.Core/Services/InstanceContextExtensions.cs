using WslSquad.Core.Dtos;

namespace WslSquad.Core.Services;

public static class InstanceContextExtensions
{
    public static BuildContextDto GetBuildContext(this IInstanceService service)
    {
        ArgumentNullException.ThrowIfNull(service);

        GetBuildSourcesResponseDto sources = service.GetBuildSources();
        IReadOnlyList<WslDockerImageDto> images = service.GetAllSavedImages();
        List<string> taken = [.. (service.GetAll() ?? [])
            .Where(row => !row.IsArchive && !string.IsNullOrEmpty(row.Name))
            .Select(row => row.Name!)];

        return new BuildContextDto
        {
            Sources = sources,
            Images = images,
            TakenNames = taken,
        };
    }

    public static AppearanceContextDto GetAppearanceContext(
        this IInstanceService service,
        string instanceName)
    {
        ArgumentNullException.ThrowIfNull(service);

        GetAppearanceResponseDto appearance = service.GetAppearance(instanceName);
        List<string> fonts = [.. service.GetFonts()];

        if (!string.IsNullOrEmpty(appearance.Font) &&
            !fonts.Contains(appearance.Font, StringComparer.Ordinal))
        {
            fonts.Add(appearance.Font);
        }

        return new AppearanceContextDto
        {
            Appearance = appearance,
            Fonts = fonts,
            ColorSchemes = service.GetColorSchemes(),
            IconPairs = service.GetIconPairs(),
        };
    }
}

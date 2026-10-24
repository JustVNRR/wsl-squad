using WslSquad.Core.Dtos;
using WslSquad.Core.Entities;

namespace WslSquad.Core.Mappers;

public sealed class InstanceMapper : IInstanceMapper
{
    public WslIndexDto ToIndexDto(WslInstance instance)
    {
        ArgumentNullException.ThrowIfNull(instance);

        return new WslIndexDto
        {
            Name = instance.Name,
            Size = instance.Size,
            IsArchive = instance.IsArchive,
            Family = instance.Family,
        };
    }

    public WslIconDto ToIconDto(WslTheme? theme)
    {
        IconDrawing? icon = theme?.Icon;

        return new WslIconDto
        {
            Text = icon?.Text,
            Top = icon?.Top,
            Bottom = icon?.Bottom,
            TextColor = icon?.TextColor,
        };
    }

    public WslPackDto ToPackDto(WslPack pack)
    {
        ArgumentNullException.ThrowIfNull(pack);

        return new WslPackDto
        {
            Name = pack.Name,
            Description = pack.Description,
            Family = pack.Family,
        };
    }

    public GetAppearanceResponseDto ToAppearanceResponse(WslTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);

        return new GetAppearanceResponseDto
        {
            Font = theme.FontName,
            ColourScheme = theme.ColorScheme,
            Icon = ToIconDto(theme),
            IconPath = theme.IconPath,
        };
    }

    public IconDrawing ToDrawing(WslIconDto icon)
    {
        ArgumentNullException.ThrowIfNull(icon);

        return new IconDrawing(icon.Text, icon.Top, icon.Bottom, icon.TextColor);
    }
}

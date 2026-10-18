using wsl_squad.Core.Dtos;
using wsl_squad.Core.Entities;

namespace wsl_squad.Core.Mappers;

/// <summary>Maps fleet entities to their DTOs (reference <c>InstanceMapper</c>).</summary>
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

    public GetAppearanceResponseDto ToAppearanceResponse(WslTheme theme, bool hasFragment)
    {
        ArgumentNullException.ThrowIfNull(theme);

        return new GetAppearanceResponseDto
        {
            Font = theme.FontName,
            ColourScheme = theme.ColorScheme,
            Icon = ToIconDto(theme),
            HasFragment = hasFragment,
            IconPath = theme.IconPath,
        };
    }

    public WslTheme FromSetAppearanceDto(SetAppearanceDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        WslTheme theme = new();

        if (!string.IsNullOrEmpty(dto.Font))
        {
            theme.FontName = dto.Font;
        }

        if (!string.IsNullOrEmpty(dto.ColourScheme))
        {
            theme.ColorScheme = dto.ColourScheme;
        }

        if (dto.Icon is not null)
        {
            theme.SetIcon(ToDrawing(dto.Icon));
        }

        if (!string.IsNullOrEmpty(dto.IconImage))
        {
            theme.IconPath = dto.IconImage;
        }

        return theme;
    }

    public IconDrawing ToDrawing(WslIconDto icon)
    {
        ArgumentNullException.ThrowIfNull(icon);

        return new IconDrawing(icon.Text, icon.Top, icon.Bottom, icon.TextColor);
    }
}

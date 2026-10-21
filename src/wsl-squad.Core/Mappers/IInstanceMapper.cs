using wsl_squad.Core.Dtos;
using wsl_squad.Core.Entities;

namespace wsl_squad.Core.Mappers;

public interface IInstanceMapper
{
    public WslIndexDto ToIndexDto(WslInstance instance);

    public WslIconDto ToIconDto(WslTheme? theme);

    public WslPackDto ToPackDto(WslPack pack);

    public GetAppearanceResponseDto ToAppearanceResponse(WslTheme theme);

    public WslTheme FromSetAppearanceDto(SetAppearanceDto dto);

    public IconDrawing ToDrawing(WslIconDto icon);
}

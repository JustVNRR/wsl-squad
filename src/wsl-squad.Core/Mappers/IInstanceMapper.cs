using wsl_squad.Core.Dtos;
using wsl_squad.Core.Entities;

namespace wsl_squad.Core.Mappers;

public interface IInstanceMapper
{
    WslIndexDto ToIndexDto(WslInstance instance);

    WslIconDto ToIconDto(WslTheme? theme);

    WslPackDto ToPackDto(WslPack pack);

    GetAppearanceResponseDto ToAppearanceResponse(WslTheme theme);

    WslTheme FromSetAppearanceDto(SetAppearanceDto dto);

    IconDrawing ToDrawing(WslIconDto icon);
}

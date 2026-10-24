using WslSquad.Core.Dtos;
using WslSquad.Core.Entities;

namespace WslSquad.Core.Mappers;

public interface IInstanceMapper
{
    WslIndexDto ToIndexDto(WslInstance instance);

    WslIconDto ToIconDto(WslTheme? theme);

    WslPackDto ToPackDto(WslPack pack);

    GetAppearanceResponseDto ToAppearanceResponse(WslTheme theme);

    WslTheme FromSetAppearanceDto(SetAppearanceDto dto);

    IconDrawing ToDrawing(WslIconDto icon);
}

using WslSquad.Core.Dtos;

namespace WslSquad.Presentation.Workflows;

public interface IAppearanceInteraction
{
    string? PickInstance(IReadOnlyList<WslIndexDto> candidates);

    IReadOnlyList<SetAppearanceDto> PromptAppearance(string instanceName, AppearanceContextDto context);
}

using wsl_squad.Core.Dtos;

namespace wsl_squad.Presentation.Workflows;

public interface IAppearanceInteraction
{
    string? PickInstance(IReadOnlyList<WslIndexDto> candidates);

    IReadOnlyList<SetAppearanceDto> PromptAppearance(string instanceName, AppearanceContextDto context);
}

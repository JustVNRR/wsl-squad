using wsl_squad.Core.Dtos;

namespace wsl_squad.Presentation.Presenters.Views;

public interface IInstancePickerView : IPresenterViewBase
{
    void ShowNoInstances();

    string? AskOffered(IReadOnlyList<WslIndexDto> instances);
}

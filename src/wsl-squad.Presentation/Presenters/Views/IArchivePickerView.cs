using wsl_squad.Core.Dtos;

namespace wsl_squad.Presentation.Presenters.Views;

public interface IArchivePickerView : IPresenterViewBase
{
    void ShowNoArchives();

    string? AskArchive(IReadOnlyList<WslIndexDto> archives);
}

using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;

namespace wsl_squad.Presentation.Presenters.Instance;

public sealed class ArchiveSelector
{
    private readonly IInstanceService _service;
    private readonly IInstanceView _view;

    public ArchiveSelector(IInstanceService service, IInstanceView view)
    {
        _service = service;
        _view = view;
    }

    public TargetSelection Select(string? target)
    {
        if (!string.IsNullOrEmpty(target))
        {
            return new TargetSelection(TargetSelectionStatus.Selected, target, []);
        }

        IReadOnlyList<WslIndexDto> rows = _service.GetAll();
        List<WslIndexDto> archives = [.. rows.Where(row => row.IsArchive)];

        if (archives.Count == 0)
        {
            _view.ShowNoArchives();

            return new TargetSelection(TargetSelectionStatus.None, string.Empty, rows);
        }

        string? picked = _view.AskArchive(archives);

        return string.IsNullOrEmpty(picked)
            ? new TargetSelection(TargetSelectionStatus.Cancelled, string.Empty, rows)
            : new TargetSelection(TargetSelectionStatus.Selected, picked, rows);
    }
}

using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;

namespace wsl_squad.Presentation.Presenters.Instance;

public enum TargetSelectionStatus
{
    Selected,
    None,
    Cancelled,
}

public sealed record TargetSelection(
    TargetSelectionStatus Status,
    string Name,
    IReadOnlyList<WslIndexDto> Rows);

public sealed class TargetSelector
{
    private readonly IInstanceService _service;
    private readonly IInstancePickerView _view;

    public TargetSelector(IInstanceService service, IInstancePickerView view)
    {
        _service = service;
        _view = view;
    }

    public TargetSelection Select(string? target, Func<WslIndexDto, bool> filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        IReadOnlyList<WslIndexDto> rows = _service.GetAll();

        if (!string.IsNullOrEmpty(target))
        {
            return new TargetSelection(TargetSelectionStatus.Selected, target, rows);
        }

        List<WslIndexDto> candidates = [.. rows.Where(filter)];

        if (candidates.Count == 0)
        {
            _view.ShowNoInstances();

            return new TargetSelection(TargetSelectionStatus.None, string.Empty, rows);
        }

        string? picked = _view.AskOffered(candidates);

        return string.IsNullOrEmpty(picked)
            ? new TargetSelection(TargetSelectionStatus.Cancelled, string.Empty, rows)
            : new TargetSelection(TargetSelectionStatus.Selected, picked, rows);
    }
}

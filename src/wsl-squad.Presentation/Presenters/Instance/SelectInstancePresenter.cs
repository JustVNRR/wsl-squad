using wsl_squad.Core.Dtos;
using wsl_squad.Presentation.Presenters.Views;

namespace wsl_squad.Presentation.Presenters.Instance;

/// <summary>Asks for an instance when the caller gave no name (reference <c>SelectInstancePresenter.psm1</c>).</summary>
public sealed class SelectInstancePresenter : BasePresenter<IInstanceView>
{
    public SelectInstancePresenter(IInstanceView view)
        : base(view)
    {
    }

    public string Miss { get; private set; } = string.Empty;

    public string Pick(string? target, IReadOnlyList<WslIndexDto> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        Miss = string.Empty;

        if (!string.IsNullOrEmpty(target))
        {
            return target;
        }

        List<WslIndexDto> all = [.. rows.Where(row => !row.IsArchive)];

        if (all.Count == 0)
        {
            View.ShowNoInstances();
            Miss = "none";

            return string.Empty;
        }

        string? picked = View.AskOffered(all);

        if (picked is null)
        {
            Miss = "cancelled";

            return string.Empty;
        }

        return picked;
    }
}

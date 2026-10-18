using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;

namespace wsl_squad.Presentation.ViewModels;

public sealed record InstanceRowViewModel(string Name, string Size, bool IsArchive, string Docker)
{
    public static InstanceRowViewModel From(WslIndexDto row)
    {
        ArgumentNullException.ThrowIfNull(row);

        return new InstanceRowViewModel(
            row.Name ?? string.Empty,
            ReferenceHelpers.FormatSize(row.Size),
            row.IsArchive,
            row.Docker ?? string.Empty);
    }
}

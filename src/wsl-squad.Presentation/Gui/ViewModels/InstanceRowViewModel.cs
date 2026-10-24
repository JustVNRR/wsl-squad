using System.Windows.Input;

using WslSquad.Core.Dtos;
using WslSquad.Core.Services;

namespace WslSquad.Presentation.Gui.ViewModels;

public sealed record InstanceRowViewModel(
    string Name,
    string Size,
    bool IsArchive,
    string Docker,
    IReadOnlyList<InstanceCommandViewModel> Commands)
{
    public static InstanceRowViewModel From(WslIndexDto row, ICommand command)
    {
        ArgumentNullException.ThrowIfNull(row);
        ArgumentNullException.ThrowIfNull(command);

        List<InstanceCommandViewModel> commands =
        [
            .. row.Commands.Select(item =>
            {
                (string glyph, string tooltip) = Describe(item.Kind);

                return new InstanceCommandViewModel(
                    row.Name ?? string.Empty,
                    item.Kind,
                    glyph,
                    tooltip,
                    command);
            }),
        ];

        return new InstanceRowViewModel(
            row.Name ?? string.Empty,
            ReferenceHelpers.FormatSize(row.Size),
            row.IsArchive,
            row.Docker ?? string.Empty,
            commands);
    }

    private static (string Glyph, string Tooltip) Describe(WslCommandKind kind)
    {
        return kind switch
        {
            WslCommandKind.Shell => ("\uF120", "Shell"),
            WslCommandKind.Restart => ("\uF01E", "Restart"),
            WslCommandKind.ManagePacks => ("\uF304", "Edit"),
            WslCommandKind.Appearance => ("\uF53F", "Appearance"),
            WslCommandKind.Duplicate => ("\uF0C5", "Duplicate"),
            WslCommandKind.Archive => ("\uF187", "Archive"),
            WslCommandKind.Compact => ("\uF066", "Compact"),
            WslCommandKind.RegisterDocker or WslCommandKind.UnregisterDocker => ("\uF1B2", "Docker Desktop"),
            WslCommandKind.Remove => ("\uF1F8", "Remove"),
            WslCommandKind.Restore => ("\uF2EA", "Restore"),
            WslCommandKind.DeleteArchive => ("\uF1F8", "Delete"),
            _ => ("\uF128", string.Empty),
        };
    }
}

public sealed record InstanceCommandViewModel(
    string InstanceName,
    WslCommandKind Kind,
    string Glyph,
    string Tooltip,
    ICommand Command)
{
    public InstanceCommandParameter Parameter => new(InstanceName, Kind);
}

public sealed record InstanceCommandParameter(string Name, WslCommandKind Kind);

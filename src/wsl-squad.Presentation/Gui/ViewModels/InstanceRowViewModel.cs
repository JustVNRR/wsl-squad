using System.Windows.Input;

using WslSquad.Core.Dtos;
using WslSquad.Core.Services;
using WslSquad.Presentation.Localization;

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
            .. row.Commands.Select(item => CommandFor(row, item.Kind, command, enabled: true)),
        ];

        if (!row.IsArchive)
        {
            if (commands.TrueForAll(item => item.Kind != WslCommandKind.ManagePacks))
            {
                int restart = commands.FindIndex(item => item.Kind == WslCommandKind.Restart);
                commands.Insert(
                    restart >= 0 ? restart + 1 : commands.Count,
                    CommandFor(row, WslCommandKind.ManagePacks, command, enabled: false));
            }

            if (commands.TrueForAll(item => item.Kind is not (WslCommandKind.RegisterDocker or WslCommandKind.UnregisterDocker)))
            {
                int remove = commands.FindIndex(item => item.Kind == WslCommandKind.Remove);
                commands.Insert(
                    remove >= 0 ? remove : commands.Count,
                    CommandFor(row, WslCommandKind.RegisterDocker, command, enabled: false));
            }
        }

        return new InstanceRowViewModel(
            row.Name ?? string.Empty,
            ReferenceHelpers.FormatSize(row.Size),
            row.IsArchive,
            row.Docker ?? string.Empty,
            commands);
    }

    private static InstanceCommandViewModel CommandFor(
        WslIndexDto row,
        WslCommandKind kind,
        ICommand command,
        bool enabled)
    {
        (string glyph, string tooltip) = Describe(kind);

        return new InstanceCommandViewModel(
            row.Name ?? string.Empty,
            kind,
            glyph,
            tooltip,
            command,
            enabled);
    }

    private static (string Glyph, string Tooltip) Describe(WslCommandKind kind)
    {
        return kind switch
        {
            WslCommandKind.Shell => ("\uF120", Localizer.Instance["CommandShell"]),
            WslCommandKind.Restart => ("\uF01E", Localizer.Instance["CommandRestart"]),
            WslCommandKind.ManagePacks => ("\uF304", Localizer.Instance["CommandEdit"]),
            WslCommandKind.Appearance => ("\uF53F", Localizer.Instance["CommandAppearance"]),
            WslCommandKind.Duplicate => ("\uF0C5", Localizer.Instance["CommandDuplicate"]),
            WslCommandKind.Archive => ("\uF187", Localizer.Instance["CommandArchive"]),
            WslCommandKind.Compact => ("\uF066", Localizer.Instance["CommandCompact"]),
            WslCommandKind.RegisterDocker or WslCommandKind.UnregisterDocker => ("\uF1B2", Localizer.Instance["CommandDockerDesktop"]),
            WslCommandKind.Remove => ("\uF1F8", Localizer.Instance["CommandRemove"]),
            WslCommandKind.Restore => ("\uF2EA", Localizer.Instance["CommandRestore"]),
            WslCommandKind.DeleteArchive => ("\uF1F8", Localizer.Instance["CommandDelete"]),
            _ => ("\uF128", string.Empty),
        };
    }
}

public sealed record InstanceCommandViewModel(
    string InstanceName,
    WslCommandKind Kind,
    string Glyph,
    string Tooltip,
    ICommand Command,
    bool Enabled)
{
    public InstanceCommandParameter Parameter => new(InstanceName, Kind);
}

public sealed record InstanceCommandParameter(string Name, WslCommandKind Kind);

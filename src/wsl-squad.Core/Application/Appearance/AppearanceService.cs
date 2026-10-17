using System.IO.Abstractions;
using System.Text.RegularExpressions;

using wsl_squad.Core.Abstractions.Time;
using wsl_squad.Core.Abstractions.Windows;
using wsl_squad.Core.Application.Common;
using wsl_squad.Core.Application.Fleet;
using wsl_squad.Core.Application.Lifecycle;
using wsl_squad.Core.Domain.Appearance;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Fleet;
using wsl_squad.Core.Infrastructure.Storage;

namespace wsl_squad.Core.Application.Appearance;

/// <summary>Reads and updates an instance's terminal appearance.</summary>
public sealed partial class AppearanceService
{
    private readonly IFileSystem _fileSystem;
    private readonly InstanceStateService _instances;
    private readonly InstanceMetadataStore _metadata;
    private readonly IWindowsTerminalStore _terminal;
    private readonly IIconRenderer _icons;
    private readonly IClock _clock;

    public AppearanceService(
        IFileSystem fileSystem,
        InstanceStateService instances,
        InstanceMetadataStore metadata,
        IWindowsTerminalStore terminal,
        IIconRenderer icons,
        IClock clock)
    {
        _fileSystem = fileSystem;
        _instances = instances;
        _metadata = metadata;
        _terminal = terminal;
        _icons = icons;
        _clock = clock;
    }

    public OperationResult<AppearanceOutcome> Get(string fleetRoot, string instanceName)
    {
        OperationResult<string> managed = _instances.GetManagedPath(fleetRoot, instanceName);

        if (!managed.IsSuccess)
        {
            return OperationFailure.From<AppearanceOutcome>(managed.Error!);
        }

        OperationResult<InstanceMetadata> metadata = _metadata.Read(managed.Value);

        if (!metadata.IsSuccess)
        {
            return OperationFailure.From<AppearanceOutcome>(metadata.Error!);
        }

        InstanceAppearance? appearance = metadata.Value.Appearance;
        List<string> warnings = [];
        OperationResult<string?> guid = _terminal.GetProfileGuid(instanceName);

        if (!guid.IsSuccess)
        {
            warnings.Add(guid.Error!.Message);
        }

        AppearanceOutcome outcome = new(
            instanceName,
            appearance?.Font,
            appearance?.ColorScheme,
            appearance?.TabTitle,
            appearance?.Icon,
            guid.IsSuccess && guid.Value is not null,
            warnings);

        return warnings.Count == 0
            ? OperationResult<AppearanceOutcome>.Ok(outcome)
            : OperationResult<AppearanceOutcome>.Warn(outcome, [.. warnings]);
    }

    public OperationResult<AppearanceOutcome> Set(
        string fleetRoot,
        string instanceName,
        AppearanceRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        OperationResult<string> managed = _instances.GetManagedPath(fleetRoot, instanceName);

        if (!managed.IsSuccess)
        {
            return OperationFailure.From<AppearanceOutcome>(managed.Error!);
        }

        OperationResult validation = Validate(request);

        if (!validation.IsSuccess)
        {
            return OperationFailure.From<AppearanceOutcome>(validation.Error!);
        }

        OperationResult schemeValidation = ValidateColorScheme(request.ColorScheme);

        if (!schemeValidation.IsSuccess)
        {
            return OperationFailure.From<AppearanceOutcome>(schemeValidation.Error!);
        }

        OperationResult<InstanceMetadata> read = _metadata.Read(managed.Value);
        InstanceMetadata metadata;

        if (read.IsSuccess)
        {
            metadata = read.Value;
        }
        else if (read.Error!.Code == ErrorCode.NotFound)
        {
            metadata = new InstanceMetadata
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = instanceName,
                CreatedUtc = _clock.UtcNow,
                DefaultUser = InstanceMetadata.DefaultUserName,
            };
        }
        else
        {
            return OperationFailure.From<AppearanceOutcome>(read.Error!);
        }

        InstanceAppearance appearance = metadata.Appearance ??= new InstanceAppearance();
        Apply(request, appearance);
        List<string> warnings = [];

        bool iconVisualsChanged = request.IconText is not null ||
                                  request.IconTop is not null ||
                                  request.IconBottom is not null ||
                                  request.IconTextColor is not null;

        if (iconVisualsChanged &&
            string.IsNullOrWhiteSpace(request.IconImage) &&
            !string.IsNullOrWhiteSpace(appearance.Icon?.Text))
        {
            string iconPath = _fileSystem.Path.Combine(managed.Value, "terminal-icon.png");
            OperationResult<string> rendered = _icons.Render(
                iconPath,
                instanceName,
                appearance.Icon.Text,
                appearance.Icon.Top,
                appearance.Icon.Bottom,
                appearance.Icon.TextColor);

            if (rendered.IsSuccess)
            {
                appearance.Icon.Image = rendered.Value;
            }
            else
            {
                warnings.Add(rendered.Error!.Message);
            }
        }

        OperationResult saved = _metadata.Save(managed.Value, metadata);

        if (!saved.IsSuccess)
        {
            return OperationFailure.From<AppearanceOutcome>(saved.Error!);
        }

        OperationResult<string?> guid = _terminal.GetProfileGuid(instanceName);
        bool hasProfile = guid.IsSuccess && guid.Value is not null;

        if (guid.IsSuccess && guid.Value is string profileGuid)
        {
            OperationResult write = _terminal.WriteFragment(
                instanceName,
                new TerminalFragmentSettings(
                    profileGuid,
                    appearance.Icon?.Image,
                    appearance.Font,
                    appearance.ColorScheme,
                    appearance.TabTitle));

            if (!write.IsSuccess)
            {
                warnings.Add(write.Error!.Message);
            }
        }
        else if (!guid.IsSuccess)
        {
            warnings.Add(guid.Error!.Message);
        }
        else
        {
            warnings.Add("No Windows Terminal profile was found; the fragment was not written.");
        }

        AppearanceOutcome outcome = new(
            instanceName,
            appearance.Font,
            appearance.ColorScheme,
            appearance.TabTitle,
            appearance.Icon,
            hasProfile,
            warnings);

        return warnings.Count == 0
            ? OperationResult<AppearanceOutcome>.Ok(outcome)
            : OperationResult<AppearanceOutcome>.Warn(outcome, [.. warnings]);
    }

    private static void Apply(AppearanceRequest request, InstanceAppearance appearance)
    {
        if (request.Font is not null)
        {
            appearance.Font = EmptyToNull(request.Font);
        }

        if (request.ColorScheme is not null)
        {
            appearance.ColorScheme = EmptyToNull(request.ColorScheme);
        }

        if (request.TabTitle is not null)
        {
            appearance.TabTitle = EmptyToNull(request.TabTitle);
        }

        bool iconChanged = request.IconText is not null ||
                           request.IconTop is not null ||
                           request.IconBottom is not null ||
                           request.IconTextColor is not null ||
                           request.IconImage is not null;

        if (!iconChanged)
        {
            return;
        }

        appearance.Icon ??= new InstanceIcon();

        if (request.IconText is not null)
        {
            appearance.Icon.Text = EmptyToNull(request.IconText);
        }

        if (request.IconTop is not null)
        {
            appearance.Icon.Top = EmptyToNull(request.IconTop);
        }

        if (request.IconBottom is not null)
        {
            appearance.Icon.Bottom = EmptyToNull(request.IconBottom);
        }

        if (request.IconTextColor is not null)
        {
            appearance.Icon.TextColor = EmptyToNull(request.IconTextColor);
        }

        if (request.IconImage is not null)
        {
            appearance.Icon.Image = EmptyToNull(request.IconImage);
        }
    }

    private OperationResult Validate(AppearanceRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.IconText) && !IconTextPattern().IsMatch(request.IconText))
        {
            return OperationResult.Fail(
                ErrorCode.Validation,
                $"Icon text '{request.IconText}' must contain 1 to 3 alphanumeric characters.");
        }

        foreach (string? color in new[] { request.IconTop, request.IconBottom, request.IconTextColor })
        {
            if (!string.IsNullOrWhiteSpace(color) && !ColorPattern().IsMatch(color))
            {
                return OperationResult.Fail(
                    ErrorCode.Validation,
                    $"Colour '{color}' must use the #RRGGBB format.");
            }
        }

        if (!string.IsNullOrWhiteSpace(request.IconImage) &&
            (!PathHelper.IsFullyQualified(request.IconImage) || !_fileSystem.File.Exists(request.IconImage)))
        {
            return OperationResult.Fail(
                ErrorCode.Validation,
                $"Icon image '{request.IconImage}' does not exist.");
        }

        return OperationResult.Ok();
    }

    private OperationResult ValidateColorScheme(string? colorScheme)
    {
        if (string.IsNullOrWhiteSpace(colorScheme))
        {
            return OperationResult.Ok();
        }

        OperationResult<IReadOnlyList<WslColorScheme>> schemes = _terminal.GetColorSchemes();

        if (!schemes.IsSuccess || schemes.Value.Count == 0)
        {
            return OperationResult.Ok();
        }

        return schemes.Value.Any(scheme =>
            string.Equals(scheme.Name, colorScheme, StringComparison.OrdinalIgnoreCase))
            ? OperationResult.Ok()
            : OperationResult.Fail(
                ErrorCode.Validation,
                $"Colour scheme '{colorScheme}' was not found in Windows Terminal.");
    }

    private static string? EmptyToNull(string value)
    {
        string trimmed = value.Trim();

        return trimmed.Length == 0 ? null : trimmed;
    }

    [GeneratedRegex("^[A-Za-z0-9]{1,3}$", RegexOptions.CultureInvariant)]
    private static partial Regex IconTextPattern();

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex ColorPattern();
}

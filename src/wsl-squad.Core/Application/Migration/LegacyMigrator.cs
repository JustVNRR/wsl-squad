using System.Globalization;
using System.IO.Abstractions;
using System.Text.Json;

using wsl_squad.Core.Abstractions.Platform;
using wsl_squad.Core.Abstractions.Time;
using wsl_squad.Core.Abstractions.Windows;
using wsl_squad.Core.Application.Common;
using wsl_squad.Core.Application.Fleet;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Fleet;
using wsl_squad.Core.Domain.Migration;
using wsl_squad.Core.Infrastructure.Json;
using wsl_squad.Core.Infrastructure.Storage;

namespace wsl_squad.Core.Application.Migration;

/// <summary>Adopts a legacy <c>.wsl-fleet</c> fleet into the wsl-squad format.</summary>
public sealed class LegacyMigrator
{
    private const string _legacyConfigDirectory = "wsl-fleet";
    private const string _legacyRootFile = "root.txt";

    private static readonly JsonDocumentOptions _documentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
    };

    private readonly IFileSystem _fileSystem;
    private readonly IPlatformEnvironment _platform;
    private readonly IWslRegistryGateway _registry;
    private readonly FleetMarkerStore _markers;
    private readonly InstanceMetadataStore _metadata;
    private readonly ArchiveStore _archives;
    private readonly FleetConfigStore _config;
    private readonly IClock _clock;

    public LegacyMigrator(
        IFileSystem fileSystem,
        IPlatformEnvironment platform,
        IWslRegistryGateway registry,
        FleetMarkerStore markers,
        InstanceMetadataStore metadata,
        ArchiveStore archives,
        FleetConfigStore config,
        IClock clock)
    {
        _fileSystem = fileSystem;
        _platform = platform;
        _registry = registry;
        _markers = markers;
        _metadata = metadata;
        _archives = archives;
        _config = config;
        _clock = clock;
    }

    public OperationResult<MigrationPlan> CreatePlan(string? sourceRoot)
    {
        OperationResult<string> root = ResolveLegacyRoot(sourceRoot);

        if (!root.IsSuccess)
        {
            return OperationFailure.From<MigrationPlan>(root.Error!);
        }

        if (!_registry.IsSupported)
        {
            return OperationResult<MigrationPlan>.Fail(
                ErrorCode.TerminalUnavailable,
                "Legacy migration requires Windows.");
        }

        IReadOnlyList<WslRegistryEntry> entries;

        try
        {
            entries = _registry.GetDistributions();
        }
        catch (Exception exception) when (exception is PlatformNotSupportedException or IOException or UnauthorizedAccessException)
        {
            return OperationResult<MigrationPlan>.Fail(
                ErrorCode.TerminalUnavailable,
                "WSL distributions could not be read from the registry.",
                exception.Message);
        }

        List<MigrationInstance> instances = [];
        List<MigrationArchive> archives = [];
        List<string> warnings = [];
        List<string> blockers = [];

        foreach (WslRegistryEntry entry in entries)
        {
            if (!PathHelper.IsFullyQualified(entry.BasePath) ||
                !PathHelper.AreSamePath(PathHelper.GetParent(entry.BasePath), root.Value))
            {
                continue;
            }

            string legacyMarker = _fileSystem.Path.Combine(entry.BasePath, FleetMarker.LegacyFileName);

            if (!_fileSystem.File.Exists(legacyMarker))
            {
                continue;
            }

            if (_markers.Exists(entry.BasePath))
            {
                warnings.Add($"{entry.Name}: already migrated, skipped.");
                continue;
            }

            LegacyMetadata metadata = ReadLegacyMetadata(entry.BasePath, out bool readable, out string? warning);

            if (warning is not null)
            {
                warnings.Add($"{entry.Name}: {warning}");
            }

            instances.Add(new MigrationInstance(entry.Name, entry.BasePath, metadata, readable, warning));
        }

        string archivesRoot = _fileSystem.Path.Combine(root.Value, ArchiveStore.ArchivesFolderName);

        if (_fileSystem.Directory.Exists(archivesRoot))
        {
            ReadArchives(archivesRoot, archives, warnings, blockers);
        }

        instances.Sort(static (left, right) => string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));
        archives.Sort(static (left, right) => string.Compare(left.Name, right.Name, StringComparison.OrdinalIgnoreCase));

        return OperationResult<MigrationPlan>.Ok(
            new MigrationPlan(root.Value, instances, archives, warnings, blockers));
    }

    public OperationResult<MigrationOutcome> Migrate(string? sourceRoot)
    {
        OperationResult<MigrationPlan> planResult = CreatePlan(sourceRoot);

        if (!planResult.IsSuccess)
        {
            return OperationFailure.From<MigrationOutcome>(planResult.Error!);
        }

        MigrationPlan plan = planResult.Value;

        if (plan.Blockers.Count > 0)
        {
            return OperationResult<MigrationOutcome>.Fail(
                ErrorCode.MigrationBlocked,
                "Legacy migration is blocked.",
                string.Join(" | ", plan.Blockers));
        }

        string backupsRoot = GetBackupsRoot();
        string timestamp = _clock.UtcNow.UtcDateTime.ToString(
            "yyyyMMdd'T'HHmmss'Z'",
            CultureInfo.InvariantCulture);
        string backupId = $"migration-{timestamp}-{Guid.NewGuid().ToString("N")[..8]}";
        string backupDirectory = _fileSystem.Path.Combine(backupsRoot, backupId);
        MigrationManifest manifest = new() { Id = backupId, SourceRoot = plan.SourceRoot };
        List<string> warnings = [.. plan.Warnings];

        try
        {
            _ = _fileSystem.Directory.CreateDirectory(backupDirectory);
            BackupLegacyState(plan, backupDirectory, manifest);
            WriteManifest(backupDirectory, manifest);

            foreach (MigrationInstance instance in plan.Instances)
            {
                MigrateInstance(instance, manifest);
            }

            foreach (MigrationArchive archive in plan.Archives)
            {
                MigrateArchive(archive, manifest);
            }

            OperationResult saved = _config.Save(new FleetConfig { FleetRoot = plan.SourceRoot });

            if (!saved.IsSuccess)
            {
                throw new MigrationStepException(saved.Error!);
            }

            manifest.Created.Add(_config.ConfigPath);
            WriteManifest(backupDirectory, manifest);

            return OperationResult<MigrationOutcome>.Ok(
                new MigrationOutcome(
                    plan.SourceRoot,
                    backupDirectory,
                    plan.Instances.Count,
                    plan.Archives.Count,
                    warnings));
        }
        catch (Exception exception) when (exception is MigrationStepException
                                              or IOException
                                              or UnauthorizedAccessException
                                              or JsonException)
        {
            RollbackManifest(manifest);

            ErrorCode code;
            string detail;

            if (exception is MigrationStepException step)
            {
                code = step.Error.Code;
                detail = step.Error.Message;
            }
            else if (exception is UnauthorizedAccessException)
            {
                code = ErrorCode.PermissionDenied;
                detail = exception.Message;
            }
            else
            {
                code = ErrorCode.IoError;
                detail = exception.Message;
            }

            return OperationResult<MigrationOutcome>.Fail(
                code,
                "Legacy migration failed and was rolled back.",
                detail);
        }
    }

    public OperationResult Rollback(string backupId)
    {
        if (!InstanceName.IsValid(backupId))
        {
            return OperationResult.Fail(ErrorCode.InvalidName, $"Backup id '{backupId}' is invalid.");
        }

        string backupsRoot = GetBackupsRoot();
        string backupDirectory = _fileSystem.Path.Combine(backupsRoot, backupId);

        if (!PathHelper.IsUnder(backupDirectory, backupsRoot))
        {
            return OperationResult.Fail(ErrorCode.Validation, $"Backup '{backupId}' is outside the backup root.");
        }

        string manifestPath = _fileSystem.Path.Combine(backupDirectory, "manifest.json");

        if (!_fileSystem.File.Exists(manifestPath))
        {
            return OperationResult.Fail(ErrorCode.NotFound, $"Backup '{backupId}' does not exist.");
        }

        try
        {
            MigrationManifest? manifest = JsonSerializer.Deserialize<MigrationManifest>(
                _fileSystem.File.ReadAllText(manifestPath),
                JsonDefaults.Options);

            if (manifest is null || !string.Equals(manifest.Id, backupId, StringComparison.Ordinal))
            {
                return OperationResult.Fail(ErrorCode.CorruptState, $"Backup '{backupId}' is invalid.");
            }

            RollbackManifest(manifest);

            return OperationResult.Ok();
        }
        catch (JsonException exception)
        {
            return OperationResult.Fail(
                ErrorCode.CorruptState,
                $"Backup '{backupId}' is not valid JSON.",
                exception.Message);
        }
        catch (IOException exception)
        {
            return OperationResult.Fail(
                ErrorCode.IoError,
                $"Backup '{backupId}' could not be restored.",
                exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            return OperationResult.Fail(
                ErrorCode.PermissionDenied,
                $"Backup '{backupId}' could not be restored.",
                exception.Message);
        }
    }

    private OperationResult<string> ResolveLegacyRoot(string? sourceRoot)
    {
        if (!string.IsNullOrWhiteSpace(sourceRoot))
        {
            return ValidateRoot(sourceRoot);
        }

        string rootFile = _fileSystem.Path.Combine(
            _platform.LocalApplicationData,
            _legacyConfigDirectory,
            _legacyRootFile);

        if (!_fileSystem.File.Exists(rootFile))
        {
            return OperationResult<string>.Fail(
                ErrorCode.NotFound,
                $"Legacy root file '{rootFile}' does not exist.");
        }

        try
        {
            string? line = Array.Find(
                _fileSystem.File.ReadAllLines(rootFile),
                candidate => !string.IsNullOrWhiteSpace(candidate));

            if (string.IsNullOrWhiteSpace(line))
            {
                return OperationResult<string>.Fail(
                    ErrorCode.CorruptState,
                    $"Legacy root file '{rootFile}' is empty.");
            }

            return ValidateRoot(line.Trim());
        }
        catch (IOException exception)
        {
            return OperationResult<string>.Fail(
                ErrorCode.IoError,
                $"Legacy root file '{rootFile}' could not be read.",
                exception.Message);
        }
    }

    private OperationResult<string> ValidateRoot(string root)
    {
        if (!PathHelper.IsFullyQualified(root))
        {
            return OperationResult<string>.Fail(
                ErrorCode.Validation,
                $"Legacy root '{root}' is not an absolute path.");
        }

        string normalized = PathHelper.NormalizeSeparators(root).TrimEnd('/');

        return _fileSystem.Directory.Exists(normalized)
            ? OperationResult<string>.Ok(normalized)
            : OperationResult<string>.Fail(
                ErrorCode.NotFound,
                $"Legacy root '{normalized}' does not exist.");
    }

    private void ReadArchives(
        string archivesRoot,
        List<MigrationArchive> archives,
        List<string> warnings,
        List<string> blockers)
    {
        foreach (string directory in _fileSystem.Directory.GetDirectories(archivesRoot))
        {
            string name = _fileSystem.Path.GetFileName(directory);

            if (_fileSystem.File.Exists(_fileSystem.Path.Combine(directory, ArchiveMetadata.FileName)))
            {
                warnings.Add($"{name}: already converted, skipped.");
                continue;
            }

            string metadataPath = _fileSystem.Path.Combine(directory, InstanceMetadata.FileName);

            if (!_fileSystem.File.Exists(metadataPath))
            {
                warnings.Add($"{name}: no instance.json, skipped.");
                continue;
            }

            LegacyMetadata metadata = ReadLegacyMetadataFile(metadataPath, out _, out string? warning);

            if (warning is not null)
            {
                warnings.Add($"{name}: {warning}");
            }

            List<string> tars =
            [
                .. _fileSystem.Directory.GetFiles(directory).Where(IsTarArchive),
            ];

            if (tars.Count != 1)
            {
                blockers.Add($"{name}: expected exactly one .tar/.tar.gz, found {tars.Count}.");
                continue;
            }

            string file = _fileSystem.Path.GetFileName(tars[0]);
            string format = file.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase) ? "tar.gz" : "tar";
            long size = _fileSystem.FileInfo.New(tars[0]).Length;

            archives.Add(new MigrationArchive(name, directory, file, format, size, metadata, warning));
        }
    }

    private static bool IsTarArchive(string path)
    {
        return path.EndsWith(".tar.gz", StringComparison.OrdinalIgnoreCase) ||
               path.EndsWith(".tar", StringComparison.OrdinalIgnoreCase);
    }

    private LegacyMetadata ReadLegacyMetadata(string directory, out bool readable, out string? warning)
    {
        return ReadLegacyMetadataFile(
            _fileSystem.Path.Combine(directory, InstanceMetadata.FileName),
            out readable,
            out warning);
    }

    private LegacyMetadata ReadLegacyMetadataFile(string path, out bool readable, out string? warning)
    {
        readable = false;
        warning = null;

        if (!_fileSystem.File.Exists(path))
        {
            warning = "instance.json is missing; defaults will be used.";

            return Defaults();
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(
                _fileSystem.File.ReadAllText(path),
                _documentOptions);
            JsonElement root = document.RootElement;
            string? font = null;
            string? colorScheme = null;
            string? iconFrom = null;
            string? iconText = null;
            string? iconTop = null;
            string? iconBottom = null;
            string? iconTextColor = null;

            if (root.ValueKind == JsonValueKind.Object &&
                root.TryGetProperty("Theme", out JsonElement theme) &&
                theme.ValueKind == JsonValueKind.Object)
            {
                font = GetString(theme, "Font");
                colorScheme = GetString(theme, "ColorScheme");
                iconFrom = GetString(theme, "IconFrom");
                iconText = GetString(theme, "IconText");
                iconTop = GetString(theme, "IconTop");
                iconBottom = GetString(theme, "IconBottom");
                iconTextColor = GetString(theme, "IconTextColor");
            }

            string docker = (GetString(root, "Docker") ?? InstanceMetadata.DockerUnknown).ToLowerInvariant();

            if (docker is not ("yes" or "no"))
            {
                docker = InstanceMetadata.DockerUnknown;
            }

            readable = true;

            return new LegacyMetadata(
                GetString(root, "DefaultUser") ?? InstanceMetadata.DefaultUserName,
                font,
                colorScheme,
                iconFrom,
                iconText,
                iconTop,
                iconBottom,
                iconTextColor,
                docker);
        }
        catch (JsonException exception)
        {
            warning = $"instance.json is invalid ({exception.Message}); defaults will be used.";

            return Defaults();
        }
        catch (IOException exception)
        {
            warning = $"instance.json could not be read ({exception.Message}); defaults will be used.";

            return Defaults();
        }
    }

    private static LegacyMetadata Defaults()
    {
        return new LegacyMetadata(
            InstanceMetadata.DefaultUserName,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            InstanceMetadata.DockerUnknown);
    }

    private static string? GetString(JsonElement element, string name)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String)
        {
            return value.GetString();
        }

        string camelCase = char.ToLowerInvariant(name[0]) + name[1..];

        return element.TryGetProperty(camelCase, out JsonElement camelValue) &&
               camelValue.ValueKind == JsonValueKind.String
            ? camelValue.GetString()
            : null;
    }

    private void MigrateInstance(MigrationInstance instance, MigrationManifest manifest)
    {
        string id = Guid.NewGuid().ToString("N");
        DateTimeOffset now = _clock.UtcNow;

        OperationResult marker = _markers.Write(instance.Directory, new FleetMarker
        {
            Id = id,
            Name = instance.Name,
            CreatedUtc = now,
            CreatedBy = "migrate",
        });

        if (!marker.IsSuccess)
        {
            throw new MigrationStepException(marker.Error!);
        }

        manifest.Created.Add(_fileSystem.Path.Combine(instance.Directory, FleetMarker.FileName));

        OperationResult saved = _metadata.Save(instance.Directory, new InstanceMetadata
        {
            Id = id,
            Name = instance.Name,
            CreatedUtc = now,
            DefaultUser = instance.Metadata.DefaultUser,
            DockerDesktop = instance.Metadata.Docker,
            Appearance = BuildAppearance(instance.Metadata),
            Source = new InstanceSource
            {
                Kind = "Migration",
                Reference = "wsl-fleet",
            },
        });

        if (!saved.IsSuccess)
        {
            throw new MigrationStepException(saved.Error!);
        }

        manifest.Created.Add(_fileSystem.Path.Combine(instance.Directory, InstanceMetadata.FileName));

        string legacyMarker = _fileSystem.Path.Combine(instance.Directory, FleetMarker.LegacyFileName);
        string migratedMarker = legacyMarker + ".migrated";

        if (_fileSystem.File.Exists(migratedMarker))
        {
            _fileSystem.File.Delete(migratedMarker);
        }

        _fileSystem.File.Move(legacyMarker, migratedMarker);
        manifest.Renamed.Add(new MigrationRenameEntry { From = legacyMarker, To = migratedMarker });
    }

    private void MigrateArchive(MigrationArchive archive, MigrationManifest manifest)
    {
        string tarPath = _fileSystem.Path.Combine(archive.Directory, archive.File);
        string sourceInstance = archive.Name.EndsWith("_archive", StringComparison.OrdinalIgnoreCase)
            ? archive.Name[..^"_archive".Length]
            : archive.Name;

        ArchiveMetadata metadata = new()
        {
            Name = archive.Name,
            SourceInstance = sourceInstance,
            CreatedUtc = _clock.UtcNow,
            Format = archive.Format,
            File = archive.File,
            SizeBytes = archive.SizeBytes,
            Sha256 = FileHash.Sha256(_fileSystem, tarPath),
            DefaultUser = archive.Metadata.DefaultUser,
            DockerDesktop = archive.Metadata.Docker,
            Appearance = BuildAppearance(archive.Metadata),
        };

        OperationResult saved = _archives.Write(archive.Directory, metadata);

        if (!saved.IsSuccess)
        {
            throw new MigrationStepException(saved.Error!);
        }

        manifest.Created.Add(_fileSystem.Path.Combine(archive.Directory, ArchiveMetadata.FileName));
    }

    private static InstanceAppearance? BuildAppearance(LegacyMetadata legacy)
    {
        bool hasIcon = !string.IsNullOrWhiteSpace(legacy.IconFrom) ||
                       !string.IsNullOrWhiteSpace(legacy.IconText) ||
                       !string.IsNullOrWhiteSpace(legacy.IconTop) ||
                       !string.IsNullOrWhiteSpace(legacy.IconBottom) ||
                       !string.IsNullOrWhiteSpace(legacy.IconTextColor);

        if (string.IsNullOrWhiteSpace(legacy.Font) &&
            string.IsNullOrWhiteSpace(legacy.ColorScheme) &&
            !hasIcon)
        {
            return null;
        }

        InstanceIcon? icon = null;

        if (hasIcon)
        {
            string mode = string.IsNullOrWhiteSpace(legacy.IconText) ? "auto" : "text";
            icon = new InstanceIcon
            {
                Mode = mode,
                Text = EmptyToNull(legacy.IconText),
                Top = EmptyToNull(legacy.IconTop),
                Bottom = EmptyToNull(legacy.IconBottom),
                TextColor = EmptyToNull(legacy.IconTextColor),
                Image = EmptyToNull(legacy.IconFrom),
            };
        }

        return new InstanceAppearance
        {
            Font = EmptyToNull(legacy.Font),
            ColorScheme = EmptyToNull(legacy.ColorScheme),
            Icon = icon,
        };
    }

    private static string? EmptyToNull(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private void BackupLegacyState(
        MigrationPlan plan,
        string backupDirectory,
        MigrationManifest manifest)
    {
        foreach (MigrationInstance instance in plan.Instances)
        {
            BackupFile(
                _fileSystem.Path.Combine(instance.Directory, FleetMarker.LegacyFileName),
                _fileSystem.Path.Combine(backupDirectory, "instances", instance.Name, FleetMarker.LegacyFileName),
                manifest);

            BackupFile(
                _fileSystem.Path.Combine(instance.Directory, InstanceMetadata.FileName),
                _fileSystem.Path.Combine(backupDirectory, "instances", instance.Name, InstanceMetadata.FileName),
                manifest,
                required: false);
        }

        foreach (MigrationArchive archive in plan.Archives)
        {
            BackupFile(
                _fileSystem.Path.Combine(archive.Directory, InstanceMetadata.FileName),
                _fileSystem.Path.Combine(backupDirectory, "archives", archive.Name, InstanceMetadata.FileName),
                manifest,
                required: false);
        }

        BackupFile(
            _config.ConfigPath,
            _fileSystem.Path.Combine(backupDirectory, "config.json"),
            manifest,
            required: false);
    }

    private void BackupFile(
        string original,
        string backup,
        MigrationManifest manifest,
        bool required = true)
    {
        if (!_fileSystem.File.Exists(original))
        {
            if (required)
            {
                throw new IOException($"Required legacy file '{original}' is missing.");
            }

            return;
        }

        string? directory = _fileSystem.Path.GetDirectoryName(backup);

        if (!string.IsNullOrEmpty(directory))
        {
            _ = _fileSystem.Directory.CreateDirectory(directory);
        }

        _fileSystem.File.Copy(original, backup, overwrite: true);
        manifest.Backups.Add(new MigrationBackupEntry { Original = original, Backup = backup });
    }

    private void WriteManifest(string backupDirectory, MigrationManifest manifest)
    {
        AtomicFileWriter.WriteAllText(
            _fileSystem,
            _fileSystem.Path.Combine(backupDirectory, "manifest.json"),
            JsonSerializer.Serialize(manifest, JsonDefaults.Options));
    }

    private void RollbackManifest(MigrationManifest manifest)
    {
        HashSet<string> restored = new(StringComparer.OrdinalIgnoreCase);

        foreach (MigrationBackupEntry backup in manifest.Backups)
        {
            if (!_fileSystem.File.Exists(backup.Backup))
            {
                continue;
            }

            string? directory = _fileSystem.Path.GetDirectoryName(backup.Original);

            if (!string.IsNullOrEmpty(directory))
            {
                _ = _fileSystem.Directory.CreateDirectory(directory);
            }

            _fileSystem.File.Copy(backup.Backup, backup.Original, overwrite: true);
            _ = restored.Add(backup.Original);
        }

        foreach (string created in manifest.Created)
        {
            if (!restored.Contains(created))
            {
                TryDeleteFile(created);
            }
        }

        foreach (MigrationRenameEntry rename in manifest.Renamed)
        {
            if (!_fileSystem.File.Exists(rename.To))
            {
                continue;
            }

            if (_fileSystem.File.Exists(rename.From))
            {
                TryDeleteFile(rename.To);
            }
            else
            {
                _fileSystem.File.Move(rename.To, rename.From);
            }
        }
    }

    private void TryDeleteFile(string path)
    {
        try
        {
            if (_fileSystem.File.Exists(path))
            {
                _fileSystem.File.Delete(path);
            }
        }
        catch (IOException)
        {
            // Best-effort rollback cleanup.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort rollback cleanup.
        }
    }

    private string GetBackupsRoot()
    {
        string? configDirectory = _fileSystem.Path.GetDirectoryName(_config.ConfigPath);

        return _fileSystem.Path.Combine(configDirectory ?? string.Empty, "backups");
    }

    public sealed class MigrationStepException : Exception
    {
        public MigrationStepException(OperationError error)
            : base(error.Message)
        {
            Error = error;
        }

        public OperationError Error { get; }
    }
}

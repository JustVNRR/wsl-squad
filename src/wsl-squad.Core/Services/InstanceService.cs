using WslSquad.Core.Exceptions;
using System.Diagnostics;
using System.Globalization;

using WslSquad.Core.Dtos;
using WslSquad.Core.Entities;
using WslSquad.Core.Mappers;
using WslSquad.Core.Repositories;
using WslSquad.Core.Infrastructure;
using WslSquad.Core.Formatting;

namespace WslSquad.Core.Services;

public sealed class InstanceService : IInstanceService
{
    private readonly IInstanceRepository _instanceRepository;
    private readonly IArchiveRepository _archiveRepository;
    private readonly IWslRepository _wslRepository;
    private readonly ITerminalRepository _terminalRepository;
    private readonly IDockerRepository _dockerRepository;
    private readonly IPackCatalogRepository _packCatalogRepository;
    private readonly IConfigRepository _configRepository;
    private readonly IInstanceMapper _instanceMapper;

    public InstanceService(
        IInstanceRepository instanceRepository,
        IArchiveRepository archiveRepository,
        IWslRepository wslRepository,
        ITerminalRepository terminalRepository,
        IDockerRepository dockerRepository,
        IPackCatalogRepository packCatalogRepository,
        IConfigRepository configRepository,
        IInstanceMapper instanceMapper)
    {
        _instanceRepository = instanceRepository;
        _archiveRepository = archiveRepository;
        _wslRepository = wslRepository;
        _terminalRepository = terminalRepository;
        _dockerRepository = dockerRepository;
        _packCatalogRepository = packCatalogRepository;
        _configRepository = configRepository;
        _instanceMapper = instanceMapper;
    }

    public IReadOnlyList<WslIndexDto> GetAll()
    {
        List<WslIndexDto> rows = [];
        IReadOnlyList<string>? dockerNames = _dockerRepository.GetDesktopNames();
        IReadOnlyList<WslPack> packs = _packCatalogRepository.GetAllPacks();

        foreach (WslInstance distro in _wslRepository.GetAllDistros())
        {
            if (!_instanceRepository.HasMarker(distro.Name))
            {
                continue;
            }

            WslInstance? stored = _instanceRepository.GetInstance(distro.Name);
            string family = stored?.Family ?? string.Empty;
            string docker = DockerState(dockerNames, distro.Name);

            distro.Size = _instanceRepository.GetVhdxSize(distro.Name);
            rows.Add(_instanceMapper.ToIndexDto(distro) with
            {
                Family = family,
                Docker = docker,
                Commands = LiveCommands(family, docker, packs),
                Path = _instanceRepository.GetFolder(distro.Name),
            });
        }

        foreach (WslInstance archive in _archiveRepository.GetAllArchives())
        {
            archive.Size = _archiveRepository.GetTarSize(archive.Name);
            rows.Add(_instanceMapper.ToIndexDto(archive) with
            {
                Commands = ArchiveCommands(),
                Path = _archiveRepository.GetFolder(archive.Name),
                Archived = _archiveRepository.GetCreatedAt(archive.Name)
                    .ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
            });
        }

        return rows;
    }

    public IReadOnlyList<WslOrphanDto> GetOrphans()
    {
        List<string> claimed = [];

        foreach (WslInstance distro in _wslRepository.GetAllDistros())
        {
            string folder = _wslRepository.GetFolder(distro.Name);

            if (folder.Length == 0 || !_instanceRepository.HasMarkerIn(folder))
            {
                continue;
            }

            claimed.Add(folder.TrimEnd('\\'));
        }

        List<WslOrphanDto> rows = [];

        foreach (string name in _instanceRepository.GetMarkedFolders())
        {
            if (claimed.Contains(_instanceRepository.GetFolder(name)))
            {
                continue;
            }

            rows.Add(new WslOrphanDto
            {
                Name = name,
                Path = _instanceRepository.GetFolder(name),
                Size = _instanceRepository.GetVhdxSize(name),
            });
        }

        return rows;
    }

    public IReadOnlyList<string> GetRunningNames()
    {
        return _wslRepository.GetRunningNames();
    }

    public GetBuildSourcesResponseDto GetBuildSources()
    {
        return new GetBuildSourcesResponseDto
        {
            Dockerfiles = [.. FormatSourceRows(_configRepository.GetDockerfiles())],
            Onboardings = [.. FormatSourceRows(_configRepository.GetOnboardings())],
            Recipes = [.. FormatSourceRows(_configRepository.GetRecipes())],
        };
    }

    public WslBuildSourceDto CreateRecipe(NewRecipeDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        string target = _configRepository.CreateRecipe((dto.Name ?? string.Empty).Trim());
        SourceRow row = new(
            Path.GetFileName(target),
            target,
            Directory.GetLastWriteTime(target));

        return FormatSourceRows([row])[0];
    }

    public void OpenInEditor(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo("code")
            {
                ArgumentList = { path },
                UseShellExecute = true,
            });
        }
        catch (System.ComponentModel.Win32Exception)
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
    }

    public RegisterDockerResponseDto RegisterDocker(RegisterDockerDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        string name = dto.InstanceName ?? string.Empty;
        DockerRestartOutcome restart = DockerRestartOutcome.NotRequested;

        try
        {
            _dockerRepository.SetDesktopState(name);
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or FileNotFoundException)
        {
            throw new WslSquadException(
                WslSquadErrorCode.DockerSettingsWriteFailed,
                name,
                exception.Message,
                exception);
        }

        if (dto.Restart)
        {
            restart = DockerRestartOutcome.Pending;

            if (_dockerRepository.TestRunning())
            {
                if (_dockerRepository.RestartDesktop())
                {
                    restart = DockerRestartOutcome.Restarted;
                }
            }
            else if (_dockerRepository.StartDesktop())
            {
                restart = DockerRestartOutcome.Started;
            }
        }

        return new RegisterDockerResponseDto { InstanceName = name, Restart = restart };
    }

    public UnregisterDockerResponseDto UnregisterDocker(UnregisterDockerDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        string name = dto.InstanceName ?? string.Empty;
        DockerRestartOutcome restart = DockerRestartOutcome.NotRequested;

        try
        {
            _dockerRepository.RemoveDesktopState(name);
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or FileNotFoundException)
        {
            throw new WslSquadException(
                WslSquadErrorCode.DockerSettingsWriteFailed,
                name,
                exception.Message,
                exception);
        }

        if (dto.Restart && _dockerRepository.TestRunning())
        {
            restart = _dockerRepository.RestartDesktop()
                ? DockerRestartOutcome.Restarted
                : DockerRestartOutcome.Pending;
        }

        return new UnregisterDockerResponseDto { InstanceName = name, Restart = restart };
    }

    public WslBuildSourceDto UploadRecipe(UploadRecipeDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        string kind = dto.Kind ?? string.Empty;
        string source = dto.Source ?? string.Empty;
        string fileName = dto.FileName ?? string.Empty;
        string target = _configRepository.CopyRecipe(kind, source, fileName);
        SourceRow row = new(
            Path.GetFileName(Path.GetDirectoryName(target) ?? string.Empty),
            target,
            File.GetLastWriteTime(target));

        return FormatSourceRows([row])[0];
    }

    private bool StartDocker()
    {
        return _dockerRepository.TestRunning() || _dockerRepository.StartDesktop();
    }

    public void DeleteRecipe(string path)
    {
        _configRepository.RemoveRecipe(path);
    }

    public void ExportRecipe(ExportRecipeDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        _configRepository.ExportRecipe(dto.Path ?? string.Empty, dto.Target ?? string.Empty);
    }

    public BuildResponseDto Build(BuildDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        string name = dto.Name ?? string.Empty;
        string? user = dto.User;

        foreach (WslInstance distro in _wslRepository.GetAllDistros())
        {
            if (distro.Name == name)
            {
                throw new WslSquadException(WslSquadErrorCode.InstanceAlreadyExists, name);
            }
        }

        if (_instanceRepository.IsFolderOccupied(name))
        {
            throw new WslSquadException(
                WslSquadErrorCode.InstallFolderNotEmpty,
                _instanceRepository.GetFolder(name));
        }

        if (!StartDocker())
        {
            throw new WslSquadException(WslSquadErrorCode.DockerUnavailable);
        }

        string tag = $"wsl-squad:{name}";
        string imageRef = dto.BuildPath ?? string.Empty;
        using AttachedConsoleScope console = new(dto.Interactive);

        if (dto.BuildType is WslBuildType.Dockerfile or WslBuildType.Recipe)
        {
            if (dto.BuildType == WslBuildType.Recipe && !File.Exists(imageRef))
            {
                throw new WslSquadException(WslSquadErrorCode.InvalidBuildSource, dto.Context);
            }

            _dockerRepository.Build(tag, imageRef, dto.Context ?? string.Empty, dto.Interactive);
            imageRef = tag;
        }
        else if (dto.BuildType == WslBuildType.Image)
        {
            imageRef = _dockerRepository.Load(imageRef, dto.Interactive);
        }

        string installPath = _instanceRepository.GetFolder(name);
        string tarPath = Path.Combine(Path.GetDirectoryName(installPath) ?? string.Empty, $"{name}-rootfs.tar");

        try
        {
            _dockerRepository.ExportRootfs(imageRef, tarPath, dto.Interactive);

            if (!Directory.Exists(installPath))
            {
                Directory.CreateDirectory(installPath);
            }

            _wslRepository.Import(name, installPath, tarPath, dto.Interactive);
        }
        catch
        {
            if (Directory.Exists(installPath))
            {
                Directory.Delete(installPath, recursive: true);
            }

            throw;
        }
        finally
        {
            TryDeleteFile(tarPath);
        }

        _instanceRepository.WriteMarker(name, "build");

        WslInstance instance = new()
        {
            Name = name,
            DefaultUser = user ?? "root",
            Family = _wslRepository.GetFamily(name) ?? string.Empty,
        };
        WslTheme theme = WslTheme.Default();

        if (!string.IsNullOrEmpty(dto.Font))
        {
            theme.FontName = dto.Font;
        }

        if (!string.IsNullOrEmpty(dto.ColourScheme))
        {
            theme.ColorScheme = dto.ColourScheme;
        }

        if (dto.Icon is not null)
        {
            theme.SetIcon(_instanceMapper.ToDrawing(dto.Icon));
        }

        instance.Theme = theme;

        if (!string.IsNullOrEmpty(dto.Onboarding))
        {
            _wslRepository.SaveOnboardingScript(name, dto.Onboarding, user);
        }
        else
        {
            string imageUser = string.Empty;

            foreach (string line in _wslRepository.GetInInstance(name, ["cat", "/etc/wsl.conf"]))
            {
                if (line.TrimStart().StartsWith("default", StringComparison.Ordinal))
                {
                    int separator = line.IndexOf('=', StringComparison.Ordinal);

                    if (separator >= 0)
                    {
                        imageUser = line[(separator + 1)..].Trim();
                    }
                }
            }

            instance.DefaultUser = imageUser.Length == 0 ? "root" : imageUser;
        }

        try
        {
            _instanceRepository.SaveInstance(instance, string.Empty);
        }
        catch (Exception exception)
        {
            try
            {
                _wslRepository.Unregister(name);
            }
            catch (Exception unregisterException) when (
                unregisterException is InvalidOperationException or IOException or UnauthorizedAccessException)
            {
                Trace.TraceWarning($"[InstanceService] Could not unregister '{name}' after the instance file failed: {unregisterException.Message}");
            }

            try
            {
                if (Directory.Exists(installPath))
                {
                    Directory.Delete(installPath, recursive: true);
                }
            }
            catch (Exception cleanupException) when (cleanupException is IOException or UnauthorizedAccessException)
            {
                Trace.TraceWarning($"[InstanceService] Could not remove '{installPath}' after the instance file failed: {cleanupException.Message}");
            }

            throw new WslSquadException(
                WslSquadErrorCode.InstanceWriteFailed,
                name,
                exception.Message,
                exception);
        }

        List<BuildNoticeDto> warnings = [];

        try
        {
            warnings.AddRange(ApplyLook(name, instance));
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException)
        {
            warnings.Add(new BuildNoticeDto(BuildNoticeKind.LookFailed, name, exception.Message));
        }

        if (dto.BuildType == WslBuildType.Dockerfile)
        {
            if (dto.KeepImage)
            {
                string savePath = _dockerRepository.GetSavedImagePath(name);

                if (!_dockerRepository.SaveImage(tag, savePath))
                {
                    warnings.Add(new BuildNoticeDto(BuildNoticeKind.ImageSaveFailed, savePath));
                }
            }

            _dockerRepository.RemoveImage(tag);
        }

        if (dto.RegisterDocker)
        {
            RegisterDockerDesktop(name);
            warnings.AddRange(WaitDockerClient(name));
        }

        WslRecipeStatus status = WslRecipeStatus.Ok;

        if (warnings.Count > 0)
        {
            status = WslRecipeStatus.Warning;
        }

        return new BuildResponseDto
        {
            InstanceName = name,
            Status = status,
            Notices = warnings,
        };
    }

    public OpenResponseDto Open(string instanceName)
    {
        bool onboardingPending =
            _wslRepository.RunInInstance(
                instanceName,
                "root",
                ["test", "-x", "/root/onboarding.sh"],
                string.Empty) == 0;
        bool hasProfile = _terminalRepository.HasWslProfile(instanceName);

        if (onboardingPending && !hasProfile)
        {
            _wslRepository.OpenBareShellAsRoot(instanceName);

            return new OpenResponseDto { InstanceName = instanceName, Profile = false };
        }

        if (hasProfile)
        {
            _terminalRepository.TouchSettings();
            _wslRepository.OpenProfile(instanceName);

            return new OpenResponseDto { InstanceName = instanceName, Profile = true };
        }

        _wslRepository.OpenBareShell(instanceName);

        return new OpenResponseDto { InstanceName = instanceName, Profile = false };
    }

    public RestartResponseDto Restart(string instanceName)
    {
        try
        {
            _wslRepository.Terminate(instanceName);
        }
        catch (InvalidOperationException exception)
        {
            throw new WslSquadException(
                WslSquadErrorCode.RestartStopFailed,
                instanceName,
                exception.Message,
                exception);
        }

        try
        {
            _wslRepository.Start(instanceName);
        }
        catch (InvalidOperationException exception)
        {
            throw new WslSquadException(
                WslSquadErrorCode.RestartStartFailed,
                instanceName,
                exception.Message,
                exception);
        }

        return new RestartResponseDto { InstanceName = instanceName };
    }

    public void Remove(RemoveDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        string name = dto.InstanceName ?? string.Empty;

        if (dto.ArchiveFirst)
        {
            try
            {
                Archive(ArchiveDtoFor(name));
            }
            catch (Exception exception) when (exception is InvalidOperationException or IOException)
            {
                throw new WslSquadException(
                    WslSquadErrorCode.ArchiveFailed,
                    null,
                    exception.Message,
                    exception);
            }
        }

        _wslRepository.TerminateQuietly(name);
        _wslRepository.Unregister(name);
        _instanceRepository.RemoveFolder(name);

        WslGuids fragments = _terminalRepository.GetWslGuids(name);
        _terminalRepository.PruneGhostEntries(name, fragments.Guids);
        _terminalRepository.DeleteFragment(name);
        _terminalRepository.RemoveStaleFragments(fragments.Guids);

        try
        {
            _dockerRepository.RemoveDesktopState(name);
        }
        catch (Exception ex) when (ex is IOException or InvalidOperationException)
        {
            Trace.TraceWarning($"[InstanceService] Could not remove Docker Desktop state for '{name}': {ex.Message}");
        }
    }

    public CompactResponseDto Compact(CompactDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        string name = dto.InstanceName ?? string.Empty;
        bool archived = false;

        if (dto.ArchiveFirst)
        {
            try
            {
                Archive(ArchiveDtoFor(name));
            }
            catch (Exception exception) when (exception is InvalidOperationException or IOException)
            {
                throw new WslSquadException(
                    WslSquadErrorCode.CompactFailed,
                    null,
                    exception.Message,
                    exception);
            }

            archived = true;
        }

        bool wasRunning = _wslRepository.GetRunningNames().Contains(name);
        long sizeBefore = _instanceRepository.GetVhdxSize(name);
        _wslRepository.Compact(name);
        long sizeAfter = _instanceRepository.GetVhdxSize(name);
        bool restarted = false;

        if (wasRunning)
        {
            try
            {
                _wslRepository.Start(name);
                restarted = true;
            }
            catch (InvalidOperationException ex)
            {
                Trace.TraceWarning($"[InstanceService] Failed to restart '{name}' after compact: {ex.Message}");
            }
        }

        return new CompactResponseDto
        {
            InstanceName = name,
            Archived = archived,
            Restarted = restarted,
            SizeBefore = sizeBefore,
            SizeAfter = sizeAfter,
            Gained = sizeBefore - sizeAfter,
        };
    }

    public IReadOnlyList<string> GetFonts()
    {
        List<string> names = [];

        foreach (string name in _configRepository.GetPromptFontNames())
        {
            if (!names.Contains(name))
            {
                names.Add(name);
            }
        }

        foreach (TerminalFont font in _terminalRepository.GetTerminalFonts())
        {
            if (!names.Contains(font.Name))
            {
                names.Add(font.Name);
            }
        }

        return names;
    }

    public IReadOnlyList<WslIconPairDto> GetIconPairs()
    {
        List<WslIconPairDto> rows = [];

        foreach (WslIconPairDto pair in _terminalRepository.GetIconPairs())
        {
            rows.Add(new WslIconPairDto
            {
                Name = pair.Name,
                Top = pair.Top,
                Bottom = pair.Bottom,
                TextColor = pair.TextColor,
            });
        }

        return rows;
    }

    public GetAppearanceResponseDto GetAppearance(string instanceName)
    {
        WslTheme theme = _terminalRepository.GetAppearance(instanceName);
        WslTheme? storedTheme = _instanceRepository.GetInstance(instanceName).Theme;

        if (storedTheme?.Icon is { Text.Length: > 0 } drawing)
        {
            theme.SetIcon(drawing);
        }

        if (_instanceRepository.HasIcon(instanceName))
        {
            theme.IconPath = _instanceRepository.GetIconPath(instanceName);
        }

        return _instanceMapper.ToAppearanceResponse(theme);
    }

    public SetAppearanceResponseDto SetAppearance(SetAppearanceDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        string name = dto.InstanceName ?? string.Empty;
        WslInstance model = _instanceRepository.GetInstance(name);
        WslTheme theme = _terminalRepository.GetAppearance(name);
        WslTheme? storedTheme = model.Theme;

        if (storedTheme?.Icon is { Text.Length: > 0 } storedIcon)
        {
            theme.SetIcon(storedIcon);
        }

        List<string> changes = [];
        bool rewriteProfile = false;

        if (!string.IsNullOrEmpty(dto.Font))
        {
            theme.FontName = dto.Font;
            changes.Add($"font '{dto.Font}'");
            rewriteProfile = true;
        }

        if (!string.IsNullOrEmpty(dto.ColourScheme))
        {
            theme.ColorScheme = dto.ColourScheme;
            changes.Add($"colours '{dto.ColourScheme}'");
            rewriteProfile = true;
        }

        if (dto.Icon is not null)
        {
            string iconPath = _instanceRepository.GetIconPath(name);
            theme.SetIcon(_terminalRepository.DrawIcon(name, iconPath, _instanceMapper.ToDrawing(dto.Icon)));
            theme.IconPath = iconPath;
            changes.Add("icon");
            rewriteProfile = true;
        }

        if (!string.IsNullOrEmpty(dto.IconImage))
        {
            _instanceRepository.SaveIcon(name, dto.IconImage);
            theme.IconPath = _instanceRepository.GetIconPath(name);
            changes.Add("icon image");
        }

        if (!rewriteProfile)
        {
            return new SetAppearanceResponseDto { InstanceName = name, Changes = changes };
        }

        string profileGuid = _terminalRepository.GetProfileGuid(name);

        if (profileGuid.Length == 0)
        {
            throw new WslSquadException(WslSquadErrorCode.TerminalProfileMissing, name);
        }

        _terminalRepository.SaveFragment(name, profileGuid, theme);
        model.Theme = theme;
        _instanceRepository.SaveInstance(model, string.Empty);
        _terminalRepository.TouchSettings();

        return new SetAppearanceResponseDto { InstanceName = name, Changes = changes };
    }

    public IReadOnlyList<WslColorSchemeDto> GetColorSchemes()
    {
        return _terminalRepository.GetColorSchemes();
    }

    public void OpenPackManager(string instanceName)
    {
        WslInstance? stored = _instanceRepository.GetInstance(instanceName);
        string family = stored?.Family ?? string.Empty;

        if (!CanManagePacks(family, _packCatalogRepository.GetAllPacks()))
        {
            throw new WslSquadException(WslSquadErrorCode.PackManagerUnavailable, instanceName);
        }

        string script = CatalogBootstrap.ReadText("ingredients/manage-packs.sh");
        string cleanup = CatalogBootstrap.ReadText("packs/cleanup_orphans.sh");
        string bootstrap = CatalogBootstrap.ReadText("ingredients/manage-packs-bootstrap.sh");
        bool hasProfile = _terminalRepository.HasWslProfile(instanceName);

        _wslRepository.SavePackManager(
            instanceName,
            script,
            cleanup,
            bootstrap,
            _packCatalogRepository.GetRoot());

        if (hasProfile)
        {
            _terminalRepository.TouchSettings();
        }

        _wslRepository.OpenPackManager(instanceName, hasProfile);
    }

    public IReadOnlyList<WslBuildSourceDto> GetAllSavedImages()
    {
        List<WslBuildSourceDto> rows = [];

        foreach (SavedImageRow image in _dockerRepository.GetAllSavedImages())
        {
            rows.Add(new WslBuildSourceDto
            {
                Name = $"{image.Name} ({image.Modified:dd'/'MM HH:mm})",
                Path = image.Path,
                Uploaded = true,
                ExportFileName = $"{image.Name}{Path.GetExtension(image.Path)}",
            });
        }

        return rows;
    }

    public IsRegistryImageValidResponseDto IsRegistryImageValid(string imageRef)
    {
        RegistryAnswer answer = _dockerRepository.TestRegistryImage(imageRef);

        return new IsRegistryImageValidResponseDto
        {
            Valid = answer.Error == RegistryValidationError.None,
            Error = answer.Error,
            TechnicalDetail = answer.TechnicalDetail,
        };
    }

    public ArchiveResponseDto Archive(ArchiveDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        string name = dto.InstanceName ?? string.Empty;
        string format = string.IsNullOrEmpty(dto.Format) ? "tar.gz" : dto.Format;
        string archiveName = $"{name}_archive";
        string archiveDir = _archiveRepository.GetFolder(archiveName);
        string archiveTar = Path.Combine(archiveDir, $"{archiveName}.{format}");

        long diskBytes = _instanceRepository.GetVhdxSize(name);
        long freeBytes = _instanceRepository.GetFreeSpace(archiveDir);

        if (freeBytes < diskBytes)
        {
            string drive = (Path.GetPathRoot(Path.GetFullPath(archiveDir)) ?? string.Empty).TrimEnd(':');

            throw new WslSquadException(
                WslSquadErrorCode.InsufficientDiskSpace,
                drive,
                $"needed {SizeFormatter.Format(diskBytes)}, free {SizeFormatter.Format(freeBytes)}");
        }

        bool stopped = false;

        if (_wslRepository.GetRunningNames().Contains(name))
        {
            if (!dto.Permit)
            {
                throw new WslSquadException(WslSquadErrorCode.InstanceRunning, name);
            }

            _wslRepository.Terminate(name);
            stopped = true;
        }

        WslInstance stored = _instanceRepository.GetInstance(name);
        WslTheme theme = stored.Theme ?? WslTheme.Default();

        try
        {
            if (_archiveRepository.HasArchive(archiveName))
            {
                _archiveRepository.DeleteArchive(archiveName);
            }

            _archiveRepository.EnsureFolder(archiveName);

            try
            {
                _wslRepository.Export(name, archiveTar, format);
            }
            catch
            {
                TryDeleteFile(archiveTar);
                throw;
            }

            if (_instanceRepository.HasIcon(name))
            {
                _archiveRepository.SaveIcon(archiveName, _instanceRepository.GetIconPath(name));
                theme.IconPath = _archiveRepository.GetIconPath(archiveName);
            }

            WslInstance archive = new()
            {
                Name = archiveName,
                IsArchive = true,
                DefaultUser = stored.DefaultUser,
                Family = stored.Family,
                Theme = theme,
            };

            _archiveRepository.SaveInstance(archive, _dockerRepository.GetDesktopState(name));
        }
        finally
        {
            if (stopped)
            {
                _wslRepository.Start(name);
            }
        }

        return new ArchiveResponseDto
        {
            InstanceName = name,
            ArchiveDir = archiveDir,
            Tar = archiveTar,
            LookFont = theme.FontName,
            LookColourScheme = theme.ColorScheme,
            Docker = _dockerRepository.GetDesktopState(name),
            Stopped = stopped,
        };
    }

    public RestoreResponseDto Restore(RestoreDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        string archiveName = dto.ArchiveName ?? string.Empty;
        string name = dto.InstanceName ?? string.Empty;

        foreach (WslInstance distro in _wslRepository.GetAllDistros())
        {
            if (distro.Name == name)
            {
                string registered = _wslRepository.GetFolder(name);

                throw new WslSquadException(
                    WslSquadErrorCode.InstanceAlreadyExists,
                    name,
                    $"registered at '{registered}'");
            }
        }

        string installPath = _instanceRepository.GetFolder(name);

        if (Directory.Exists(installPath))
        {
            throw new WslSquadException(WslSquadErrorCode.TargetFolderExists, installPath);
        }

        string tar = _archiveRepository.GetTar(archiveName);

        if (tar.Length == 0)
        {
            throw new WslSquadException(
                WslSquadErrorCode.ArchiveTarMissing,
                _archiveRepository.GetFolder(archiveName));
        }

        _wslRepository.Import(name, installPath, tar);
        _instanceRepository.WriteMarker(name, "restore");

        WslInstance stored = _archiveRepository.GetInstance(archiveName);
        WslTheme theme = stored.Theme ?? WslTheme.Default();

        if (_archiveRepository.HasIcon(archiveName))
        {
            _instanceRepository.SaveIcon(name, _archiveRepository.GetIconPath(archiveName));
            theme.IconPath = _instanceRepository.GetIconPath(name);
        }

        string profileGuid = _terminalRepository.GetProfileGuid(name);

        if (profileGuid.Length > 0)
        {
            _terminalRepository.SaveFragment(name, profileGuid, theme);
        }

        WslInstance instance = new()
        {
            Name = name,
            DefaultUser = stored.DefaultUser,
            Family = stored.Family,
            Theme = theme,
        };

        _instanceRepository.SaveInstance(instance, string.Empty);
        _terminalRepository.TouchSettings();

        string carried = _archiveRepository.GetDocker(archiveName);

        if (carried == "yes" && _dockerRepository.GetDesktopState(name) == "no")
        {
            _dockerRepository.SetDesktopState(name);
            _dockerRepository.RestartDesktop();
        }

        return new RestoreResponseDto { ArchiveName = archiveName, InstanceName = name };
    }

    public DeleteArchiveResponseDto DeleteArchive(string archive)
    {
        _archiveRepository.DeleteArchive(archive);

        return new DeleteArchiveResponseDto { ArchiveName = archive };
    }

    public DuplicateResponseDto Duplicate(DuplicateDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        string source = dto.SourceName ?? string.Empty;
        string copy = dto.CopyName ?? string.Empty;

        foreach (WslInstance distro in _wslRepository.GetAllDistros())
        {
            if (distro.Name == copy)
            {
                throw new WslSquadException(WslSquadErrorCode.InstanceAlreadyExists, copy);
            }
        }

        foreach (WslInstance archive in _archiveRepository.GetAllArchives())
        {
            if (archive.Name == copy)
            {
                throw new WslSquadException(WslSquadErrorCode.InstanceAlreadyExists, copy);
            }
        }

        string destination = _instanceRepository.GetFolder(copy);
        long diskBytes = _instanceRepository.GetVhdxSize(source);
        long needed = 2 * diskBytes;
        long freeBytes = _instanceRepository.GetFreeSpace(destination);

        if (freeBytes < needed)
        {
            string drive = (Path.GetPathRoot(Path.GetFullPath(destination)) ?? string.Empty).TrimEnd(':');

            throw new WslSquadException(
                WslSquadErrorCode.InsufficientDiskSpace,
                drive,
                $"needed {SizeFormatter.Format(needed)}, free {SizeFormatter.Format(freeBytes)}");
        }

        WslInstance stored = _instanceRepository.GetInstance(source);
        WslTheme theme = _terminalRepository.GetAppearance(source);
        WslTheme? storedTheme = stored.Theme;

        if (storedTheme?.Icon is { Text.Length: > 0 } storedIcon)
        {
            theme.SetIcon(storedIcon);
        }

        string dockerCarried = _dockerRepository.GetDesktopState(source);
        bool stopped = false;

        if (_wslRepository.GetRunningNames().Contains(source))
        {
            _wslRepository.Terminate(source);
            stopped = true;
        }

        string tempTar = Path.Combine(Path.GetDirectoryName(destination) ?? string.Empty, $"{copy}-export.tar.gz");

        try
        {
            _wslRepository.Export(source, tempTar, "tar.gz");
            _wslRepository.Import(copy, destination, tempTar);
            _instanceRepository.WriteMarker(copy, "duplicate");

            if (_instanceRepository.HasIcon(source))
            {
                _instanceRepository.SaveIcon(copy, _instanceRepository.GetIconPath(source));
                theme.IconPath = _instanceRepository.GetIconPath(copy);
            }

            string profileGuid = _terminalRepository.GetProfileGuid(copy);

            if (profileGuid.Length > 0)
            {
                _terminalRepository.SaveFragment(copy, profileGuid, theme);
            }

            WslInstance instance = new()
            {
                Name = copy,
                DefaultUser = stored.DefaultUser,
                Family = stored.Family,
                Theme = theme,
            };

            _instanceRepository.SaveInstance(instance, string.Empty);
            _terminalRepository.TouchSettings();

            if (dockerCarried == "yes" && _dockerRepository.GetDesktopState(copy) == "no")
            {
                _dockerRepository.SetDesktopState(copy);
                _dockerRepository.RestartDesktop();
            }
        }
        finally
        {
            TryDeleteFile(tempTar);

            if (stopped)
            {
                _wslRepository.Start(source);
            }
        }

        return new DuplicateResponseDto { SourceName = source, CopyName = copy };
    }

    private static ArchiveDto ArchiveDtoFor(string name)
    {
        return new ArchiveDto
        {
            InstanceName = name,
            Format = "tar.gz",
            Permit = true,
        };
    }

    private List<BuildNoticeDto> ApplyLook(string name, WslInstance instance)
    {
        List<BuildNoticeDto> warnings = [];
        WslTheme theme = instance.Theme ?? WslTheme.Default();

        if (!_terminalRepository.IsFontInstalled(theme.FontName ?? string.Empty))
        {
            try
            {
                _terminalRepository.InstallFont(theme.FontName ?? string.Empty, null);
            }
            catch (Exception exception) when (exception is IOException or InvalidOperationException or PlatformNotSupportedException or HttpRequestException)
            {
                warnings.Add(new BuildNoticeDto(BuildNoticeKind.FontInstallFailed, theme.FontName, exception.Message));
            }
        }

        string iconPath = _instanceRepository.GetIconPath(name);
        bool iconDrawn = false;

        try
        {
            theme.SetIcon(_terminalRepository.DrawIcon(name, iconPath, null));
            iconDrawn = true;
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or PlatformNotSupportedException)
        {
            TryDeleteFile(iconPath);
            warnings.Add(new BuildNoticeDto(BuildNoticeKind.NoIconGenerated, null, exception.Message));
        }

        WslGuids fragments = _terminalRepository.GetWslGuids(name);

        if (fragments.Guids.Count > 0)
        {
            PruneReport ghosts = _terminalRepository.PruneGhostEntries(name, fragments.Guids);

            foreach (string path in ghosts.Unreadable)
            {
                warnings.Add(new BuildNoticeDto(BuildNoticeKind.GhostEntriesNotPruned, path));
            }
        }

        _terminalRepository.RemoveStaleFragments(fragments.Guids);

        if (!string.IsNullOrEmpty(fragments.ProfileGuid))
        {
            theme.IconPath = iconDrawn ? iconPath : string.Empty;
            _terminalRepository.SaveFragment(name, fragments.ProfileGuid, theme);
        }
        else
        {
            warnings.Add(new BuildNoticeDto(BuildNoticeKind.NoWslFragment, name));
        }

        instance.Theme = theme;
        _instanceRepository.SaveInstance(instance, string.Empty);
        _terminalRepository.TouchSettings();

        return warnings;
    }

    private void RegisterDockerDesktop(string name)
    {
        try
        {
            _dockerRepository.SetDesktopState(name);
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException)
        {
            throw new WslSquadException(
                WslSquadErrorCode.DockerSettingsWriteFailed,
                name,
                exception.Message,
                exception);
        }

        if (!_dockerRepository.RestartDesktop())
        {
            throw new WslSquadException(WslSquadErrorCode.DockerRestartFailed, name);
        }
    }

    private IReadOnlyList<BuildNoticeDto> WaitDockerClient(string name)
    {
        bool usable = false;

        for (int attempt = 1; attempt <= 5 && !usable; attempt++)
        {
            usable = _wslRepository.TestInInstance(name, ["docker", "version"]);

            if (!usable)
            {
                Thread.Sleep(2000);
            }
        }

        return usable
            ? [new BuildNoticeDto(BuildNoticeKind.DockerReady)]
            : [new BuildNoticeDto(BuildNoticeKind.DockerNotAnswering)];
    }

    private static IReadOnlyList<WslCommandDto> LiveCommands(
        string family,
        string docker,
        IReadOnlyList<WslPack> packs)
    {
        List<WslCommandKind> kinds = [WslCommandKind.Shell, WslCommandKind.Restart];

        if (CanManagePacks(family, packs))
        {
            kinds.Add(WslCommandKind.ManagePacks);
        }

        kinds.Add(WslCommandKind.Appearance);
        kinds.Add(WslCommandKind.Duplicate);
        kinds.Add(WslCommandKind.Archive);
        kinds.Add(WslCommandKind.Compact);

        if (docker == "yes")
        {
            kinds.Add(WslCommandKind.UnregisterDocker);
        }
        else if (docker == "no")
        {
            kinds.Add(WslCommandKind.RegisterDocker);
        }

        kinds.Add(WslCommandKind.Remove);

        return [.. kinds.Select(kind => new WslCommandDto { Kind = kind })];
    }

    private static IReadOnlyList<WslCommandDto> ArchiveCommands()
    {
        return
        [
            new WslCommandDto { Kind = WslCommandKind.Restore },
            new WslCommandDto { Kind = WslCommandKind.DeleteArchive },
        ];
    }

    private static bool CanManagePacks(string family, IReadOnlyList<WslPack> packs)
    {
        return !string.IsNullOrEmpty(family) &&
               packs.Any(pack =>
                   pack.Offered &&
                   string.Equals(pack.Family, family, StringComparison.OrdinalIgnoreCase));
    }

    private static List<WslBuildSourceDto> FormatSourceRows(IReadOnlyList<SourceRow> rows)
    {
        List<WslBuildSourceDto> output = [];

        foreach (SourceRow row in rows.OrderBy(item => item.Name, StringComparer.Ordinal))
        {
            bool uploaded = HasFingerprint(row.Name);
            string name = uploaded ? row.Name[..^9] : row.Name;

            output.Add(new WslBuildSourceDto
            {
                Name = $"{name} ({row.Modified.ToString("dd'/'MM HH:mm", CultureInfo.InvariantCulture)})",
                Path = row.Path,
                Uploaded = uploaded,
                ExportFileName = $"{name}{Path.GetExtension(row.Path)}",
            });
        }

        return output;
    }

    private static string DockerState(IReadOnlyList<string>? dockerNames, string name)
    {
        if (dockerNames is null)
        {
            return "unknown";
        }

        return dockerNames.Contains(name) ? "yes" : "no";
    }

    private static bool HasFingerprint(string name)
    {
        if (name.Length < 9 || name[^9] != '-')
        {
            return false;
        }

        return name[^8..].All(char.IsAsciiHexDigitLower);
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Trace.TraceWarning($"[InstanceService] Could not delete '{path}': {ex.Message}");
        }
    }


}

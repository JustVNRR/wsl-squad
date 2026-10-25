namespace WslSquad.Core.Dtos;

public enum WslCommandKind
{
    Shell,
    Restart,
    ManagePacks,
    Appearance,
    Duplicate,
    Archive,
    Compact,
    RegisterDocker,
    UnregisterDocker,
    Remove,
    Restore,
    DeleteArchive,
}

public sealed record WslCommandDto
{
    public WslCommandKind Kind { get; init; }
}

public sealed record WslIndexDto
{
    public string? Name { get; init; }

    public long Size { get; init; }

    public bool IsArchive { get; init; }

    public string? Family { get; init; }

    public string? Docker { get; init; }

    public IReadOnlyList<WslCommandDto> Commands { get; init; } = [];

    public string Path { get; init; } = string.Empty;

    public string Archived { get; init; } = string.Empty;
}

public sealed record WslOrphanDto
{
    public string? Name { get; init; }

    public string? Path { get; init; }

    public long Size { get; init; }
}

public sealed record WslBuildSourceDto
{
    public string? Name { get; init; }

    public string? Path { get; init; }

    public bool Uploaded { get; init; }

    public string ExportFileName { get; init; } = string.Empty;
}

public sealed record GetBuildSourcesResponseDto
{
    public List<WslBuildSourceDto> Dockerfiles { get; init; } = [];

    public List<WslBuildSourceDto> Onboardings { get; init; } = [];

    public List<WslBuildSourceDto> Recipes { get; init; } = [];
}

public sealed record NewRecipeDto
{
    public string? Name { get; init; }
}

public sealed record UploadRecipeDto
{
    public string? Kind { get; init; }

    public string? Source { get; init; }

    public string? FileName { get; init; }
}

public sealed record ExportRecipeDto
{
    public string? Path { get; init; }

    public string? Target { get; init; }
}

public sealed record MigrateResponseDto
{
    public List<string> Moved { get; init; } = [];

    public string? Target { get; init; }
}

public sealed record WslIconDto
{
    public string? Text { get; init; }

    public string? Top { get; init; }

    public string? Bottom { get; init; }

    public string? TextColor { get; init; }
}

public enum WslBuildType
{
    Dockerfile,
    Image,
    Recipe,
}

public sealed record BuildDto
{
    public string? Name { get; init; }

    public string? User { get; init; }

    public WslBuildType BuildType { get; init; }

    public string? BuildPath { get; init; }

    public string? Context { get; init; }

    public string? Onboarding { get; init; }

    public string? Font { get; init; }

    public string? ColourScheme { get; init; }

    public WslIconDto? Icon { get; init; }

    public bool KeepImage { get; init; }

    public bool RegisterDocker { get; init; }

    public bool Interactive { get; init; }

    public static BuildDto FromSource(
        string name,
        string user,
        WslBuildType type,
        string source,
        string onboarding = "",
        bool keepImage = false,
        bool registerDocker = false,
        bool interactive = true)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(onboarding);

        bool recipe = type == WslBuildType.Recipe;
        (string buildPath, string context) = type switch
        {
            WslBuildType.Recipe => (Path.Combine(source, "Dockerfile"), source),
            WslBuildType.Dockerfile => (source, Path.GetDirectoryName(source) ?? string.Empty),
            WslBuildType.Image => (source, string.Empty),
            _ => (source, string.Empty),
        };

        return new BuildDto
        {
            Name = name,
            User = recipe ? string.Empty : user,
            BuildType = type,
            BuildPath = buildPath,
            Context = context,
            Onboarding = recipe ? string.Empty : onboarding,
            KeepImage = keepImage,
            RegisterDocker = !recipe && registerDocker,
            Interactive = interactive,
        };
    }
}

public enum WslRecipeStatus
{
    Ok,
    Warning,
    Nok,
}

public enum BuildNoticeKind
{
    LookFailed,
    ImageSaveFailed,
    DockerReady,
    DockerNotAnswering,
    FontInstallFailed,
    NoIconGenerated,
    GhostEntriesNotPruned,
    NoWslFragment,
    ShellOpenFailed,
}

public sealed record BuildNoticeDto(
    BuildNoticeKind Kind,
    string? Subject = null,
    string? TechnicalDetail = null);

public sealed record BuildResponseDto
{
    public string? InstanceName { get; init; }

    public WslRecipeStatus Status { get; init; }

    public List<BuildNoticeDto> Notices { get; init; } = [];
}

public sealed record RemoveDto
{
    public string? InstanceName { get; init; }

    public bool ArchiveFirst { get; init; }
}

public sealed record CompactDto
{
    public string? InstanceName { get; init; }

    public bool ArchiveFirst { get; init; }
}

public sealed record CompactResponseDto
{
    public string? InstanceName { get; init; }

    public bool Archived { get; init; }

    public bool Restarted { get; init; }

    public long SizeBefore { get; init; }

    public long SizeAfter { get; init; }

    public long Gained { get; init; }
}

public sealed record GetAppearanceResponseDto
{
    public string? Font { get; init; }

    public string? ColourScheme { get; init; }

    public WslIconDto? Icon { get; init; }

    public string? IconPath { get; init; }
}

public sealed record SetAppearanceDto
{
    public string? InstanceName { get; init; }

    public string? Font { get; init; }

    public string? ColourScheme { get; init; }

    public WslIconDto? Icon { get; init; }

    public string? IconImage { get; init; }
}

public sealed record AppearanceContextDto
{
    public GetAppearanceResponseDto Appearance { get; init; } = new();

    public IReadOnlyList<string> Fonts { get; init; } = [];

    public IReadOnlyList<WslColorSchemeDto> ColorSchemes { get; init; } = [];

    public IReadOnlyList<WslIconPairDto> IconPairs { get; init; } = [];
}

public sealed record BuildContextDto
{
    public GetBuildSourcesResponseDto Sources { get; init; } = new();

    public IReadOnlyList<WslBuildSourceDto> Images { get; init; } = [];

    public IReadOnlyList<string> TakenNames { get; init; } = [];
}

public sealed record WslIconPairDto
{
    public string? Name { get; init; }

    public string? Top { get; init; }

    public string? Bottom { get; init; }

    public string? TextColor { get; init; }
}

public sealed record WslColorSchemeDto
{
    public string? Name { get; init; }

    public string? Background { get; init; }

    public string? Foreground { get; init; }

    public string? Black { get; init; }

    public string? DarkRed { get; init; }

    public string? DarkGreen { get; init; }

    public string? DarkYellow { get; init; }

    public string? DarkBlue { get; init; }

    public string? DarkMagenta { get; init; }

    public string? DarkCyan { get; init; }

    public string? Gray { get; init; }

    public string? DarkGray { get; init; }

    public string? Red { get; init; }

    public string? Green { get; init; }

    public string? Yellow { get; init; }

    public string? Blue { get; init; }

    public string? Magenta { get; init; }

    public string? Cyan { get; init; }

    public string? White { get; init; }
}

public sealed record ArchiveDto
{
    public string? InstanceName { get; init; }

    public string? Format { get; init; }

    public bool Permit { get; init; }
}

public sealed record ArchiveResponseDto
{
    public string? InstanceName { get; init; }

    public string? ArchiveDir { get; init; }

    public string? Tar { get; init; }

    public string? LookFont { get; init; }

    public string? LookColourScheme { get; init; }

    public string? Docker { get; init; }

    public bool Stopped { get; init; }
}

public sealed record RestoreDto
{
    public string? ArchiveName { get; init; }

    public string? InstanceName { get; init; }
}

public sealed record DuplicateDto
{
    public string? SourceName { get; init; }

    public string? CopyName { get; init; }
}

public sealed record DeleteArchiveResponseDto
{
    public string? ArchiveName { get; init; }
}

public sealed record RestoreResponseDto
{
    public string? ArchiveName { get; init; }

    public string? InstanceName { get; init; }
}

public sealed record DuplicateResponseDto
{
    public string? SourceName { get; init; }

    public string? CopyName { get; init; }
}

public sealed record RestartResponseDto
{
    public string? InstanceName { get; init; }
}

public sealed record OpenResponseDto
{
    public string? InstanceName { get; init; }

    public bool Profile { get; init; }
}

public enum AppearanceChangeKind
{
    Font,
    ColourScheme,
    Icon,
    IconImage,
}

public sealed record AppearanceChangeDto(AppearanceChangeKind Kind, string? Value = null);

public sealed record SetAppearanceResponseDto
{
    public string? InstanceName { get; init; }

    public List<AppearanceChangeDto> Changes { get; init; } = [];
}

public sealed record WslConfigResponseDto
{
    public string? Path { get; init; }

    public bool Created { get; init; }
}

public sealed record RegisterDockerDto
{
    public string? InstanceName { get; init; }

    public bool Restart { get; init; } = true;
}

public enum DockerRestartOutcome
{
    NotRequested,
    Restarted,
    Started,
    Pending,
}

public sealed record RegisterDockerResponseDto
{
    public string? InstanceName { get; init; }

    public DockerRestartOutcome Restart { get; init; }
}

public sealed record UnregisterDockerDto
{
    public string? InstanceName { get; init; }

    public bool Restart { get; init; } = true;
}

public sealed record UnregisterDockerResponseDto
{
    public string? InstanceName { get; init; }

    public DockerRestartOutcome Restart { get; init; }
}

public sealed record LogEntryDto
{
    public DateTime When { get; init; }

    public string? Level { get; init; }

    public string? Message { get; init; }
}

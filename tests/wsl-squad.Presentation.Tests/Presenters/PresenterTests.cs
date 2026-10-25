using FluentAssertions;

using Moq;

using WslSquad.Core.Dtos;
using WslSquad.Core.Services;
using WslSquad.Presentation.Cli.Presenters;
using WslSquad.Presentation.Cli.Presenters.Fleet;
using WslSquad.Presentation.Cli.Presenters.Instance;
using WslSquad.Presentation.Cli.Ports;
using WslSquad.Presentation.Localization;
using WslSquad.Presentation.Workflows;

namespace WslSquad.Presentation.Tests.Presenters;

public class PresenterTests
{
    [Fact]
    public void Manage_packs_presenter_opens_the_distro_manager()
    {
        Mock<IInstanceService> service = new();
        service.Setup(s => s.GetAll()).Returns([new WslIndexDto { Name = "alpha" }]);
        TestInstanceView view = new();

        IWorkflowResult result = new ManagePacksPresenter(service.Object, view).Run("alpha");

        result.Status.Should().Be(WorkflowStatus.Success);
        service.Verify(s => s.OpenPackManager("alpha"), Times.Once);
        view.ManagedPacks.Should().Equal("alpha");
    }

    [Fact]
    public void Manage_packs_presenter_reports_a_manager_failure()
    {
        Mock<IInstanceService> service = new();
        service.Setup(s => s.GetAll()).Returns([new WslIndexDto { Name = "alpha" }]);
        service.Setup(s => s.OpenPackManager("alpha")).Throws(new InvalidOperationException("no manager"));
        TestInstanceView view = new();

        IWorkflowResult result = new ManagePacksPresenter(service.Object, view).Run("alpha");

        result.Status.Should().Be(WorkflowStatus.Failed);
        view.Errors.Should().ContainSingle(message => message.Contains("no manager", StringComparison.Ordinal));
    }

    [Fact]
    public void Appearance_presenter_applies_each_selected_change()
    {
        Mock<IInstanceService> service = new();
        service.Setup(s => s.GetAppearance("alpha")).Returns(
            new GetAppearanceResponseDto { Font = "VT323", ColourScheme = "amber" });
        service.Setup(s => s.GetFonts()).Returns(["VT323", "Cascadia"]);
        service.Setup(s => s.GetColorSchemes()).Returns(
            [new WslColorSchemeDto { Name = "amber" }, new WslColorSchemeDto { Name = "ice" }]);
        service.Setup(s => s.GetIconPairs()).Returns([]);
        service.Setup(s => s.SetAppearance(It.IsAny<SetAppearanceDto>()))
            .Returns((SetAppearanceDto dto) => new SetAppearanceResponseDto
            {
                InstanceName = dto.InstanceName,
                Changes =
                [
                    dto.Font is not null
                        ? new AppearanceChangeDto(AppearanceChangeKind.Font)
                        : new AppearanceChangeDto(AppearanceChangeKind.ColourScheme),
                ],
            });
        TestAppearanceView view = new();
        view.Themes.Enqueue("font");
        view.Themes.Enqueue("color");
        view.Themes.Enqueue(null);

        IWorkflowResult result = new SetAppearancePresenter(service.Object, view).Run("alpha");

        result.Status.Should().Be(WorkflowStatus.Success);
        service.Verify(s => s.SetAppearance(It.Is<SetAppearanceDto>(dto =>
            dto.Font == "Cascadia" && dto.ColourScheme == null)), Times.Once);
        service.Verify(s => s.SetAppearance(It.Is<SetAppearanceDto>(dto =>
            dto.Font == null && dto.ColourScheme == "ice")), Times.Once);
        view.Shown.Should().ContainSingle();
        view.Shown[0].Changes.Select(change => change.Kind).Should().Equal(
            AppearanceChangeKind.Font,
            AppearanceChangeKind.ColourScheme);
    }

    [Fact]
    public void Archive_presenter_reports_a_missing_instance_as_no_target()
    {
        Mock<IInstanceService> service = new();
        service.Setup(s => s.GetAll()).Returns([]);
        TestInstanceView view = new();

        IWorkflowResult result = new ArchivePresenter(service.Object, view).Run(null);

        result.Status.Should().Be(WorkflowStatus.NoTarget);
        result.Message.Should().Contain("No instance is available.");
    }

    [Fact]
    public void Browse_filters_live_instances_and_archives()
    {
        Mock<IInstanceService> service = new();
        service.Setup(s => s.GetAll()).Returns(
        [
            new WslIndexDto { Name = "alpha", Docker = "yes" },
            new WslIndexDto { Name = "old", IsArchive = true },
        ]);
        service.Setup(s => s.GetRunningNames()).Returns(["alpha"]);
        service.Setup(s => s.GetOrphans()).Returns([]);

        TestFleetView view = new() { PickedInstance = "alpha" };
        PresenterAction unregister = new("unregister", "Unregister", _ => WorkflowResults.Success("u"));
        PresenterAction archive = new("archive", "Archive", _ => WorkflowResults.Success("a"));
        Dictionary<WslCommandKind, PresenterAction> actions = new()
        {
            [WslCommandKind.UnregisterDocker] = unregister,
            [WslCommandKind.Archive] = archive,
        };
        BrowsePresenter presenter = new(service.Object, view, actions);

        presenter.AskInstances()!.Name.Should().Be("alpha");
        presenter.AskArchives()!.Name.Should().Be("old");

        WslIndexDto live = new()
        {
            Name = "alpha",
            Docker = "yes",
            Commands = [new WslCommandDto { Kind = WslCommandKind.UnregisterDocker }],
        };
        presenter.AskAction(live)!.Key.Should().Be("unregister");

        view.PickedInstance = null;
        presenter.AskInstances().Should().BeNull();
        presenter.Miss.Should().Be("cancelled");
    }

    [Fact]
    public void Browse_only_offers_the_commands_the_row_carries()
    {
        Mock<IInstanceService> service = new();
        TestFleetView view = new();
        PresenterAction shell = new("shell", "Shell", _ => WorkflowResults.Success("s"));
        PresenterAction manage = new("manage_packs", "Manage packs", _ => WorkflowResults.Success("m"));
        Dictionary<WslCommandKind, PresenterAction> actions = new()
        {
            [WslCommandKind.Shell] = shell,
            [WslCommandKind.ManagePacks] = manage,
        };
        BrowsePresenter presenter = new(service.Object, view, actions);
        WslIndexDto row = new()
        {
            Name = "alpha",
            Commands = [new WslCommandDto { Kind = WslCommandKind.Shell }],
        };

        presenter.AskAction(row)!.Key.Should().Be("shell");
    }

    [Fact]
    public void Archive_presenter_builds_the_reference_request()
    {
        Mock<IInstanceService> service = new();
        service.Setup(s => s.GetAll()).Returns([new WslIndexDto { Name = "alpha" }]);
        service.Setup(s => s.GetAll()).Returns([new WslIndexDto { Name = "alpha" }]);
        service.Setup(s => s.Archive(It.IsAny<ArchiveDto>())).Returns(new ArchiveResponseDto
        {
            InstanceName = "alpha",
            ArchiveDir = "/archives/alpha_archive",
            Tar = "/archives/alpha_archive/alpha_archive.tar.gz",
        });
        TestInstanceView view = new() { ConfirmAnswer = true };

        IWorkflowResult result = new ArchivePresenter(service.Object, view).Run("alpha");

        result.Status.Should().Be(WorkflowStatus.Success);
        service.Verify(s => s.Archive(It.Is<ArchiveDto>(dto =>
            dto.InstanceName == "alpha" && dto.Format == "tar.gz" && dto.Permit)), Times.Once);
        view.StopWarnings.Should().Equal("alpha");
    }

    [Fact]
    public void Archive_presenter_stops_on_refused_confirmation()
    {
        Mock<IInstanceService> service = new();
        service.Setup(s => s.GetAll()).Returns([new WslIndexDto { Name = "alpha" }]);
        TestInstanceView view = new() { ConfirmAnswer = false };

        IWorkflowResult result = new ArchivePresenter(service.Object, view).Run("alpha");

        result.Status.Should().Be(WorkflowStatus.Cancelled);
        service.Verify(s => s.Archive(It.IsAny<ArchiveDto>()), Times.Never);
    }

    [Fact]
    public void Remove_presenter_requires_the_exact_typed_name()
    {
        Mock<IInstanceService> service = new();
        service.Setup(s => s.GetAll()).Returns([new WslIndexDto { Name = "alpha" }]);
        TestInstanceView view = new() { TypedName = "wrong" };

        IWorkflowResult result = new RemovePresenter(service.Object, view).Run("alpha");

        result.Status.Should().Be(WorkflowStatus.Cancelled);
        service.Verify(s => s.Remove(It.IsAny<RemoveDto>()), Times.Never);

        view.TypedName = "alpha";
        view.ConfirmAnswer = false;

        result = new RemovePresenter(service.Object, view).Run("alpha");

        result.Status.Should().Be(WorkflowStatus.Success);
        service.Verify(s => s.Remove(It.Is<RemoveDto>(dto =>
            dto.InstanceName == "alpha" && !dto.ArchiveFirst)), Times.Once);
    }

    [Fact]
    public void Build_presenter_walks_the_dockerfile_road()
    {
        Mock<IInstanceService> service = new();
        service.Setup(s => s.GetBuildSources()).Returns(new GetBuildSourcesResponseDto
        {
            Dockerfiles =
            [
                new WslBuildSourceDto
                {
                    Name = "ubuntu",
                    Path = "/catalog/ubuntu/Dockerfile",
                },
            ],
            Onboardings = [],
            Recipes = [],
        });
        service.Setup(s => s.GetAllSavedImages()).Returns([]);
        service.Setup(s => s.Build(It.IsAny<BuildDto>())).Returns(new BuildResponseDto
        {
            InstanceName = "alpha",
            Status = WslRecipeStatus.Ok,
        });
        service.Setup(s => s.Open("alpha")).Returns(new OpenResponseDto { InstanceName = "alpha" });
        TestBuildView view = new();

        IWorkflowResult result = new BuildPresenter(service.Object, view).Run();

        result.Status.Should().Be(WorkflowStatus.Success);
        result.Subject.Should().Be("alpha");
        service.Verify(s => s.Build(It.Is<BuildDto>(dto =>
            dto.Name == "alpha" &&
            dto.BuildType == WslBuildType.Dockerfile &&
            dto.BuildPath == "/catalog/ubuntu/Dockerfile" &&
            dto.Context == Path.GetDirectoryName(dto.BuildPath) &&
            dto.KeepImage &&
            !dto.RegisterDocker &&
            dto.Interactive)), Times.Once);
    }

    [Fact]
    public void Build_presenter_survives_a_shell_open_failure()
    {
        Mock<IInstanceService> service = new();
        service.Setup(s => s.GetBuildSources()).Returns(new GetBuildSourcesResponseDto
        {
            Dockerfiles =
            [
                new WslBuildSourceDto
                {
                    Name = "ubuntu",
                    Path = "/catalog/ubuntu/Dockerfile",
                },
            ],
            Onboardings = [],
            Recipes = [],
        });
        service.Setup(s => s.GetAllSavedImages()).Returns([]);
        service.Setup(s => s.Build(It.IsAny<BuildDto>())).Returns(new BuildResponseDto
        {
            InstanceName = "alpha",
            Status = WslRecipeStatus.Ok,
        });
        service.Setup(s => s.Open("alpha")).Throws(new InvalidOperationException("shell failed"));
        TestBuildView view = new();

        IWorkflowResult result = new BuildPresenter(service.Object, view).Run();

        result.Status.Should().Be(WorkflowStatus.Success);
        result.Subject.Should().Be("alpha");
    }

    [Fact]
    public void Settings_presenter_deletes_after_confirmation()
    {
        Mock<IPackCatalogService> catalogue = new();
        TestFleetView view = new() { ConfirmAnswer = true };
        SettingsPresenter presenter = new(catalogue.Object, view, []);
        WslPackDto pack = new() { Name = "tools", Dependents = ["shell"] };

        IWorkflowResult? result = presenter.Delete(pack);

        result!.Status.Should().Be(WorkflowStatus.Success);
        catalogue.Verify(c => c.DeletePack("tools"), Times.Once);
        view.Dependents.Should().ContainKey("tools");
    }

    [Fact]
    public void Settings_presenter_reports_an_invalid_pack_copy_name()
    {
        Mock<IPackCatalogService> catalogue = new();
        catalogue.Setup(c => c.CreatePack(It.IsAny<CreatePackDto>()))
            .Throws(new ArgumentException("'__under' is not usable as a pack name."));
        TestFleetView view = new();
        SettingsPresenter presenter = new(catalogue.Object, view, []);
        WslPackDto pack = new() { Name = "tools" };

        IWorkflowResult? result = presenter.Duplicate(pack);

        result!.Status.Should().Be(WorkflowStatus.Failed);
        view.Errors.Should().ContainSingle(message => message.Contains("not usable", StringComparison.Ordinal));
    }

    [Fact]
    public void Delete_archive_presenter_cancels_without_confirmation()
    {
        Mock<IInstanceService> service = new();
        TestInstanceView view = new() { ConfirmAnswer = false };
        DeleteArchivePresenter presenter = new(service.Object, view);

        IWorkflowResult result = presenter.Run("old");

        result.Status.Should().Be(WorkflowStatus.Cancelled);
        result.Subject.Should().Be("old");
        service.Verify(s => s.DeleteArchive(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Delete_archive_presenter_deletes_after_confirmation()
    {
        Mock<IInstanceService> service = new();
        service.Setup(s => s.DeleteArchive("old"))
            .Returns(new DeleteArchiveResponseDto { ArchiveName = "old" });
        TestInstanceView view = new() { ConfirmAnswer = true };
        DeleteArchivePresenter presenter = new(service.Object, view);

        IWorkflowResult result = presenter.Run("old");

        result.Status.Should().Be(WorkflowStatus.Success);
        result.Subject.Should().Be("old");
        service.Verify(s => s.DeleteArchive("old"), Times.Once);
    }

    private sealed class TestFleetView : IFleetView
    {
        public string? PickedInstance { get; set; } = "alpha";

        public string? PickedArchive { get; set; } = "old";

        public bool? ConfirmAnswer { get; set; } = true;

        public Dictionary<string, IReadOnlyList<string>> Dependents { get; } = [];

        public List<string> Errors { get; } = [];

        public string? PickInstance(IReadOnlyList<WslIndexDto> instances, IReadOnlyList<WslOrphanDto> orphans, IReadOnlyList<string> running)
        {
            return PickedInstance;
        }

        public UiLanguage? PickLanguage(IReadOnlyList<UiLanguage> languages)
        {
            return languages.FirstOrDefault();
        }

        public void ShowLanguage(string displayName)
        {
        }

        public void ShowNoInstances(IReadOnlyList<WslOrphanDto> orphans)
        {
        }

        public string? PickArchive(IReadOnlyList<WslIndexDto> archives)
        {
            return PickedArchive;
        }

        public void ShowNoArchives()
        {
        }

        public PresenterAction? PickAction(string title, IReadOnlyList<PresenterAction> actions)
        {
            return actions.FirstOrDefault(action => action.Key == "unregister")
                ?? actions.FirstOrDefault();
        }

        public WslPackDto? PickPack(IReadOnlyList<WslPackDto> packs)
        {
            return packs.FirstOrDefault();
        }

        public void ShowNoPacks()
        {
        }

        public string? PickPackAction(string packName)
        {
            return "delete";
        }

        public string? AskName(string sourceName)
        {
            return $"{sourceName}-copy";
        }

        public void ShowOpening(string name)
        {
        }

        public void ShowDuplicated(string name)
        {
        }

        public void ShowRemoved(string name)
        {
        }

        public void ShowDependents(string name, IReadOnlyList<string> dependents)
        {
            Dependents[name] = dependents;
        }

        public bool? Confirm(string question, bool defaultNo = true)
        {
            return ConfirmAnswer;
        }

        public string? AskTarget(string root)
        {
            return null;
        }

        public void ShowCancelled(string noun)
        {
        }

        public void ShowSameFolder()
        {
        }

        public void ShowTargetProblem(string problem)
        {
        }

        public void ShowLead()
        {
        }

        public void Show(MigrateResponseDto response)
        {
        }

        public void Show(WslConfigResponseDto response)
        {
        }

        public void ShowError(string message)
        {
            Errors.Add(message);
        }

        public void Clear()
        {
        }
    }

    private sealed class TestInstanceView : IInstanceView
    {
        public bool? ConfirmAnswer { get; set; } = true;

        public string? TypedName { get; set; }

        public string? CopyName { get; set; } = "alpha-copy";

        public string? Offered { get; set; } = "alpha";

        public string? NewName { get; set; } = "alpha";

        public string? ArchiveName { get; set; } = "alpha_archive";

        public List<string> StopWarnings { get; } = [];

        public List<string> ManagedPacks { get; } = [];

        public List<string> Errors { get; } = [];

        public bool? AskDockerAction(string name, bool shouldRegister) => true;

        public bool? AskRestart() => true;

        public string? AskCopyName() => CopyName;

        public string? AskOffered(IReadOnlyList<WslIndexDto> instances) => Offered;

        public string? AskNewName() => NewName;

        public string? AskArchive(IReadOnlyList<WslIndexDto> archives) => ArchiveName;

        public bool? Confirm(string question, bool defaultNo = true) => ConfirmAnswer;

        public void ShowNoInstances() { }

        public bool ConfirmDestruction(string instanceName) => TypedName == instanceName;

        public void ShowStopWarning(string name) => StopWarnings.Add(name);

        public void ShowCancelled(string noun) { }

        public void ShowReplacing(string archiveName) { }

        public void ShowBackingUp(string name) { }

        public void Show(ArchiveResponseDto response) { }

        public void ShowBeforeQuestions() { }

        public void ShowNoArchive() { }

        public void ShowReplacingArchive(string name) { }

        public void ShowCompacting(string name, bool archiveFirst) { }

        public void Show(CompactResponseDto response) { }

        public void ShowDeleting(string target) { }

        public void Show(DeleteArchiveResponseDto response) { }

        public void ShowNameProblem(string message) { }

        public void ShowDiskWarning(string source) { }

        public void ShowDuplicating(string source, string copy) { }

        public void Show(DuplicateResponseDto response) { }

        public void ShowManagingPacks(string name) => ManagedPacks.Add(name);

        public void ShowOpening(string name) { }

        public void Show(OpenResponseDto response) { }


        public void Show(RegisterDockerResponseDto response) { }

        public void Show(UnregisterDockerResponseDto response) { }

        public void ShowRemovingSteps() { }

        public void Show() { }

        public void ShowStoppingWarning(string name) { }

        public void ShowRestarting(string name) { }

        public void Show(RestartResponseDto response) { }

        public void ShowNoArchives() { }

        public void ShowRestoring(string archiveName) { }

        public void Show(RestoreResponseDto response) { }

        public void ShowError(string message) => Errors.Add(message);
    }

    private sealed class TestAppearanceView : IAppearanceView
    {
        public Queue<string?> Themes { get; } = new();

        public void Clear() { }

        public List<SetAppearanceResponseDto> Shown { get; } = [];

        public List<string> Errors { get; } = [];

        public string? AskTheme(string name) => Themes.TryDequeue(out string? theme) ? theme : null;

        public string? AskFont(string name, IReadOnlyList<string> fonts, string current) => "Cascadia";

        public string? AskColour(string name, IReadOnlyList<WslColorSchemeDto> schemes, string current) => "ice";

        public string? AskIconType(string name) => null;

        public string? AskLetters(string question) => null;

        public void ShowLettersHint() { }

        public WslIconPairDto? AskIconPair(
            string name,
            IReadOnlyList<WslIconPairDto> pairs,
            string suggestion,
            string? top,
            string? bottom) => null;

        public string? AskImagePath() => null;

        public void ShowNoFile() { }

        public void ShowBadExtension() { }

        public void Show(SetAppearanceResponseDto response) => Shown.Add(response);

        public void ShowNoInstances() { }

        public string? AskOffered(IReadOnlyList<WslIndexDto> instances) => "alpha";

        public bool? Confirm(string question, bool defaultNo = true) => true;

        public void ShowCancelled(string noun) { }

        public void ShowError(string message) => Errors.Add(message);
    }

    private sealed class TestBuildView : IBuildView
    {
        public List<string> Errors { get; } = [];

        public string? AskInstanceName(string prompt) => "alpha";

        public BuildRoad? AskRoad() => new BuildRoad(WslBuildType.Dockerfile, "Dockerfile");

        public BuildSourceChoice? AskSource(string label, IReadOnlyList<BuildSourceChoice> rows) => rows.FirstOrDefault();

        public bool? AskRunOnboarding() => false;

        public BuildSourceChoice? AskOnboarding(IReadOnlyList<BuildSourceChoice> rows) => rows.FirstOrDefault();

        public string? AskUserName(string prompt) => "alice";

        public bool? AskKeepImage() => true;

        public bool? AskRegisterDocker(string name) => false;

        public void ShowNameProblem(string message) { }

        public void ShowAbort(IReadOnlyList<string> lines) { }

        public void ShowNoOnboarding() { }

        public void ShowBeforeAccount() { }

        public void ShowUserHint() { }

        public bool? Confirm(string question, bool defaultNo = true) => true;

        public void ShowCancelled(string noun) { }

        public void Show(BuildResponseDto response) { }

        public void ShowError(string message) => Errors.Add(message);
    }
}

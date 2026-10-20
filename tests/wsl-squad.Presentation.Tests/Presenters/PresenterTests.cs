using FluentAssertions;

using Moq;

using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters;
using wsl_squad.Presentation.Presenters.Fleet;
using wsl_squad.Presentation.Presenters.Instance;
using wsl_squad.Presentation.Presenters.Views;

namespace wsl_squad.Presentation.Tests.Presenters;

public class PresenterTests
{
    [Fact]
    public void ActionResult_exposes_ok_and_nok()
    {
        ActionResult.Ok("done").Status.Should().Be(ActionResultStatus.Ok);
        ActionResult.Nok("boom").Status.Should().Be(ActionResultStatus.Nok);
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
        PresenterAction register = new("register", "Register", _ => ActionResult.Ok("r"));
        PresenterAction unregister = new("unregister", "Unregister", _ => ActionResult.Ok("u"));
        PresenterAction archive = new("archive", "Archive", _ => ActionResult.Ok("a"));
        PresenterAction unregisterSlot = new("unregister", "Unregister", _ => ActionResult.Ok("u"));
        BrowsePresenter presenter = new(service.Object, view, [archive, unregisterSlot], register, unregister, [archive]);

        presenter.AskInstances()!.Name.Should().Be("alpha");
        presenter.AskArchives()!.Name.Should().Be("old");

        WslIndexDto live = new() { Name = "alpha", Docker = "yes" };
        presenter.AskAction(live)!.Key.Should().Be("unregister");

        view.PickedInstance = null;
        presenter.AskInstances().Should().BeNull();
        presenter.Miss.Should().Be("cancelled");
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

        ActionResult result = new ArchivePresenter(service.Object, view).Run("alpha");

        result.Status.Should().Be(ActionResultStatus.Ok);
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

        ActionResult result = new ArchivePresenter(service.Object, view).Run("alpha");

        result.Status.Should().Be(ActionResultStatus.Ok);
        result.Message.Should().Contain("cancelled");
        service.Verify(s => s.Archive(It.IsAny<ArchiveDto>()), Times.Never);
    }

    [Fact]
    public void Remove_presenter_requires_the_exact_typed_name()
    {
        Mock<IInstanceService> service = new();
        service.Setup(s => s.GetAll()).Returns([new WslIndexDto { Name = "alpha" }]);
        TestInstanceView view = new() { TypedName = "wrong" };

        ActionResult result = new RemovePresenter(service.Object, view).Run("alpha");

        result.Message.Should().Contain("cancelled");
        service.Verify(s => s.Remove(It.IsAny<RemoveDto>()), Times.Never);

        view.TypedName = "alpha";
        view.ConfirmAnswer = false;

        result = new RemovePresenter(service.Object, view).Run("alpha");

        result.Status.Should().Be(ActionResultStatus.Ok);
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
        service.Setup(s => s.StartDocker()).Returns(true);
        service.Setup(s => s.Build(It.IsAny<BuildDto>())).Returns(new BuildResponseDto
        {
            InstanceName = "alpha",
            Status = WslRecipeStatus.Ok,
        });
        service.Setup(s => s.Open("alpha")).Returns(new OpenResponseDto { InstanceName = "alpha" });
        TestBuildView view = new();

        ActionResult result = new BuildPresenter(service.Object, view).Run();

        result.Status.Should().Be(ActionResultStatus.Ok);
        result.Message.Should().Be("Build : alpha");
        service.Verify(s => s.Build(It.Is<BuildDto>(dto =>
            dto.Name == "alpha" &&
            dto.BuildType == WslBuildType.Dockerfile &&
            dto.BuildPath == "/catalog/ubuntu/Dockerfile" &&
            dto.Context == "/catalog/ubuntu" &&
            dto.KeepImage &&
            !dto.RegisterDocker &&
            dto.Interactive)), Times.Once);
    }

    [Fact]
    public void Settings_presenter_deletes_after_confirmation()
    {
        Mock<IPackCatalogService> catalogue = new();
        TestFleetView view = new() { ConfirmAnswer = true };
        SettingsPresenter presenter = new(catalogue.Object, view, []);
        WslPackDto pack = new() { Name = "tools", Dependents = ["shell"] };

        ActionResult? result = presenter.Delete(pack);

        result!.Status.Should().Be(ActionResultStatus.Ok);
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

        ActionResult? result = presenter.Duplicate(pack);

        result!.Status.Should().Be(ActionResultStatus.Nok);
        view.Errors.Should().ContainSingle(message => message.Contains("not usable", StringComparison.Ordinal));
    }

    [Fact]
    public void Delete_archive_presenter_cancels_without_confirmation()
    {
        Mock<IInstanceService> service = new();
        TestInstanceView view = new() { ConfirmAnswer = false };
        DeleteArchivePresenter presenter = new(service.Object, view);

        ActionResult result = presenter.Run("old");

        result.Status.Should().Be(ActionResultStatus.Ok);
        result.Message.Should().Be("DeleteArchive : cancelled");
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

        ActionResult result = presenter.Run("old");

        result.Status.Should().Be(ActionResultStatus.Ok);
        result.Message.Should().Be("DeleteArchive : old");
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

        public bool? Confirm(string question, bool defaultNo)
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

        public IReadOnlyList<string>? Packs { get; set; } = [];

        public List<string> StopWarnings { get; } = [];

        public List<string> Errors { get; } = [];

        public bool? AskRestartDocker() => true;

        public bool? AskRestart() => true;

        public bool? AskRegister(string name) => true;

        public bool? AskUnregister(string name) => true;

        public string? AskTypedName() => TypedName;

        public string? AskCopyName() => CopyName;

        public string? AskOffered(IReadOnlyList<WslIndexDto> instances) => Offered;

        public string? AskNewName() => NewName;

        public string? AskArchive(IReadOnlyList<WslIndexDto> archives) => ArchiveName;

        public IReadOnlyList<string>? AskPacks(string name, IReadOnlyList<PackChoice> rows, IReadOnlyList<int> checkedIndexes) => Packs;

        public bool? Confirm(string question, bool defaultNo) => ConfirmAnswer;

        public void ShowNoInstances() { }

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

        public void ShowNoName() { }

        public void ShowDeleting(string target) { }

        public void Show(DeleteArchiveResponseDto response) { }

        public void ShowNameHint() { }

        public void ShowDiskWarning(string source) { }

        public void ShowDuplicating(string source, string copy) { }

        public void Show(DuplicateResponseDto response) { }

        public void ShowNothingToChoose(string name) { }

        public void ShowNothingToDo(string name) { }

        public void ShowPlan(IReadOnlyList<string> toAdd, IReadOnlyList<string> toRemove) { }

        public void Show(ManagePacksResponseDto response) { }

        public void ShowOpening(string name) { }

        public void Show(OpenResponseDto response) { }

        public void ShowAlreadyRegistered(string name) { }

        public void ShowNotRegistered(string name) { }

        public void ShowSettingsUnreadable() { }

        public void ShowNoCandidates() { }

        public void Show(RegisterDockerResponseDto response) { }

        public void Show(UnregisterDockerResponseDto response) { }

        public void ShowDestructionGate(string name, string question) { }

        public void ShowGateClose() { }

        public void ShowGateRefused() { }

        public void ShowGateOpened() { }

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

    private sealed class TestBuildView : IBuildView
    {
        public List<string> Errors { get; } = [];

        public string? AskInstanceName(string prompt) => "alpha";

        public BuildRoad? AskRoad() => new BuildRoad(WslBuildType.Dockerfile, "Dockerfile");

        public string? AskRegistryReference(string prompt) => null;

        public BuildSourceChoice? AskSource(string label, IReadOnlyList<BuildSourceChoice> rows) => rows.FirstOrDefault();

        public bool? AskRunOnboarding() => false;

        public BuildSourceChoice? AskOnboarding(IReadOnlyList<BuildSourceChoice> rows) => rows.FirstOrDefault();

        public string? AskUserName(string prompt) => "alice";

        public bool? AskKeepImage() => true;

        public bool? AskRegisterDocker(string name) => false;

        public void ShowNameHint() { }

        public void ShowAbort(IReadOnlyList<string> lines) { }

        public void ShowUnknownRegistry(string reference, string why) { }

        public void ShowNoOnboarding() { }

        public void ShowBeforeAccount() { }

        public void ShowUserHint() { }

        public void ShowCancelled(string noun) { }

        public void Show(BuildResponseDto response) { }

        public void ShowError(string message) => Errors.Add(message);
    }
}

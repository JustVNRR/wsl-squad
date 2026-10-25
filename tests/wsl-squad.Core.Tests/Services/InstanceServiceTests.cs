using WslSquad.Core.Exceptions;
using FluentAssertions;

using Moq;

using WslSquad.Core.Dtos;
using WslSquad.Core.Entities;
using WslSquad.Core.Mappers;
using WslSquad.Core.Repositories;
using WslSquad.Core.Services;
using WslSquad.Core.Tests.Support;

namespace WslSquad.Core.Tests.Services;

public class InstanceServiceTests
{
    private readonly Mock<IInstanceRepository> _instances = new();
    private readonly Mock<IArchiveRepository> _archives = new();
    private readonly Mock<IWslRepository> _wsl = new();
    private readonly Mock<ITerminalRepository> _terminal = new();
    private readonly Mock<IDockerRepository> _docker = new();
    private readonly Mock<IPackCatalogRepository> _packs = new();
    private readonly Mock<IConfigRepository> _config = new();
    private readonly InstanceService _service;

    public InstanceServiceTests()
    {
        _service = new InstanceService(
            _instances.Object,
            _archives.Object,
            _wsl.Object,
            _terminal.Object,
            _docker.Object,
            _packs.Object,
            _config.Object,
            new InstanceMapper());
    }

    [Fact]
    public void Lists_marked_distributions_with_their_docker_state()
    {
        WslInstance distro = new() { Name = "alpha" };
        _wsl.Setup(wsl => wsl.GetAllDistros()).Returns([distro]);
        _instances.Setup(instances => instances.HasMarker("alpha")).Returns(true);
        _instances.Setup(instances => instances.GetVhdxSize("alpha")).Returns(1024);
        _instances.Setup(instances => instances.GetFolder("alpha")).Returns("/fleet/alpha");
        _docker.Setup(docker => docker.GetDesktopNames()).Returns(["alpha"]);
        _archives.Setup(archives => archives.GetAllArchives()).Returns([]);
        _instances.Setup(instances => instances.GetInstance("alpha")).Returns(new WslInstance
        {
            Name = "alpha",
            Family = "debian",
        });
        _packs.Setup(repository => repository.GetAllPacks()).Returns([new WslPack { Name = "base", Family = "debian" }]);

        WslIndexDto row = _service.GetAll().Should().ContainSingle().Subject;

        row.Name.Should().Be("alpha");
        row.Size.Should().Be(1024);
        row.Docker.Should().Be("yes");
        row.Path.Should().Be("/fleet/alpha");
    }

    [Fact]
    public void Recipes_are_returned_with_their_path()
    {
        _config.Setup(config => config.GetRecipes()).Returns(
        [
            new SourceRow("default", "/catalog/default"),
        ]);

        WslBuildSourceDto recipe = _service.GetRecipes().Should().ContainSingle().Subject;

        recipe.Name.Should().StartWith("default");
        recipe.BaseName.Should().Be("default");
        recipe.Path.Should().Be("/catalog/default");
    }

    [Fact]
    public void Duplicates_a_recipe()
    {
        _config.Setup(config => config.DuplicateRecipe("/catalog/default", "default-copy"))
            .Returns("/catalog/default-copy");
        _config.Setup(config => config.GetRecipes()).Returns([]);

        WslBuildSourceDto recipe = _service.DuplicateRecipe(new DuplicateRecipeDto
        {
            SourcePath = "/catalog/default",
            NewName = "default-copy",
        });

        recipe.Name.Should().StartWith("default-copy");
        recipe.BaseName.Should().Be("default-copy");
        recipe.Path.Should().Be("/catalog/default-copy");
    }

    [Fact]
    public void Offers_manage_packs_when_the_family_has_a_catalog_pack()
    {
        SetupListedInstance("debian");
        _packs.Setup(repository => repository.GetAllPacks()).Returns([new WslPack { Name = "base", Family = "debian" }]);

        WslIndexDto row = _service.GetAll().Should().ContainSingle().Subject;

        row.Family.Should().Be("debian");
        row.Commands.Select(command => command.Kind).Should().Contain(WslCommandKind.ManagePacks);
    }

    [Fact]
    public void Hides_manage_packs_when_the_family_has_no_catalog_pack()
    {
        SetupListedInstance("alpine");
        _packs.Setup(repository => repository.GetAllPacks()).Returns([new WslPack { Name = "base", Family = "debian" }]);

        WslIndexDto row = _service.GetAll().Should().ContainSingle().Subject;

        row.Family.Should().Be("alpine");
        row.Commands.Select(command => command.Kind).Should().NotContain(WslCommandKind.ManagePacks);
    }

    [Fact]
    public void Hides_manage_packs_when_the_family_is_unknown()
    {
        SetupListedInstance(string.Empty);
        _packs.Setup(repository => repository.GetAllPacks()).Returns([new WslPack { Name = "base", Family = "debian" }]);

        WslIndexDto row = _service.GetAll().Should().ContainSingle().Subject;

        row.Commands.Select(command => command.Kind).Should().NotContain(WslCommandKind.ManagePacks);
    }

    [Fact]
    public void Lists_only_restore_and_delete_for_an_archive()
    {
        _wsl.Setup(wsl => wsl.GetAllDistros()).Returns([]);
        _docker.Setup(docker => docker.GetDesktopNames()).Returns([]);
        _packs.Setup(repository => repository.GetAllPacks()).Returns([]);
        _archives.Setup(archives => archives.GetAllArchives()).Returns(
        [
            new WslInstance { Name = "alpha_archive", IsArchive = true },
        ]);
        _archives.Setup(archives => archives.GetTarSize("alpha_archive")).Returns(1024);
        _archives.Setup(archives => archives.GetFolder("alpha_archive")).Returns("/archives/alpha_archive");
        _archives.Setup(archives => archives.GetCreatedAt("alpha_archive")).Returns(DateTime.Now);

        WslIndexDto row = _service.GetAll().Should().ContainSingle().Subject;

        row.Commands.Select(command => command.Kind).Should().Equal(
            WslCommandKind.Restore,
            WslCommandKind.DeleteArchive);
    }

    private void SetupListedInstance(string family)
    {
        _wsl.Setup(wsl => wsl.GetAllDistros()).Returns([new WslInstance { Name = "alpha" }]);
        _instances.Setup(instances => instances.HasMarker("alpha")).Returns(true);
        _instances.Setup(instances => instances.GetVhdxSize("alpha")).Returns(1024);
        _instances.Setup(instances => instances.GetFolder("alpha")).Returns("/fleet/alpha");
        _instances.Setup(instances => instances.GetInstance("alpha")).Returns(new WslInstance
        {
            Name = "alpha",
            Family = family,
        });
        _docker.Setup(docker => docker.GetDesktopNames()).Returns([]);
        _archives.Setup(archives => archives.GetAllArchives()).Returns([]);
    }

    [Fact]
    public void Refuses_to_build_over_a_registered_instance()
    {
        _wsl.Setup(wsl => wsl.GetAllDistros()).Returns([new WslInstance { Name = "alpha" }]);

        Action act = () => _service.Build(new BuildDto { Name = "alpha" });

        act.Should().Throw<WslSquadException>().Which.Code.Should().Be(WslSquadErrorCode.InstanceAlreadyExists);
        _docker.VerifyNoOtherCalls();
    }

    [Fact]
    public void Refuses_to_build_over_an_occupied_folder()
    {
        _wsl.Setup(wsl => wsl.GetAllDistros()).Returns([]);
        _instances.Setup(instances => instances.IsFolderOccupied("alpha")).Returns(true);

        Action act = () => _service.Build(new BuildDto { Name = "alpha" });

        act.Should().Throw<WslSquadException>().Which.Code.Should().Be(WslSquadErrorCode.InstallFolderNotEmpty);
    }

    [Fact]
    public void Opens_a_bare_root_shell_while_the_onboarding_is_pending_and_no_profile_exists()
    {
        _wsl.Setup(wsl => wsl.RunInInstance(
                "alpha",
                "root",
                It.IsAny<IReadOnlyList<string>>(),
                string.Empty))
            .Returns(0);

        OpenResponseDto response = _service.Open("alpha");

        response.Profile.Should().BeFalse();
        _wsl.Verify(wsl => wsl.OpenBareShellAsRoot("alpha"), Times.Once);
        _wsl.Verify(wsl => wsl.OpenProfile(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Opens_the_themed_profile_while_the_onboarding_is_pending()
    {
        _wsl.Setup(wsl => wsl.RunInInstance(
                "alpha",
                "root",
                It.IsAny<IReadOnlyList<string>>(),
                string.Empty))
            .Returns(0);
        _terminal.Setup(terminal => terminal.HasWslProfile("alpha")).Returns(true);

        OpenResponseDto response = _service.Open("alpha");

        response.Profile.Should().BeTrue();
        _terminal.Verify(terminal => terminal.TouchSettings(), Times.Once);
        _wsl.Verify(wsl => wsl.OpenProfile("alpha"), Times.Once);
        _wsl.Verify(wsl => wsl.OpenBareShellAsRoot(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Opens_the_terminal_profile_once_the_onboarding_is_done()
    {
        _wsl.Setup(wsl => wsl.RunInInstance(
                "alpha",
                "root",
                It.IsAny<IReadOnlyList<string>>(),
                string.Empty))
            .Returns(1);
        _terminal.Setup(terminal => terminal.HasWslProfile("alpha")).Returns(true);

        OpenResponseDto response = _service.Open("alpha");

        response.Profile.Should().BeTrue();
        _terminal.Verify(terminal => terminal.TouchSettings(), Times.Once);
        _wsl.Verify(wsl => wsl.OpenProfile("alpha"), Times.Once);
        _wsl.Verify(wsl => wsl.OpenBareShellAsRoot(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Refuses_to_archive_without_room()
    {
        _archives.Setup(archives => archives.GetFolder("alpha_archive")).Returns("/archives/alpha_archive");
        _instances.Setup(instances => instances.GetVhdxSize("alpha")).Returns(10L * 1024 * 1024 * 1024);
        _instances.Setup(instances => instances.GetFreeSpace("/archives/alpha_archive")).Returns(1024);

        Action act = () => _service.Archive(new ArchiveDto { InstanceName = "alpha", Format = "tar.gz", Permit = true });

        act.Should().Throw<WslSquadException>().Which.Code.Should().Be(WslSquadErrorCode.InsufficientDiskSpace);
    }

    [Fact]
    public void Refuses_to_apply_a_look_without_a_terminal_profile()
    {
        _instances.Setup(instances => instances.GetInstance("alpha")).Returns(new WslInstance { Name = "alpha" });
        _terminal.Setup(terminal => terminal.GetAppearance("alpha")).Returns(WslTheme.Default());
        _terminal.Setup(terminal => terminal.GetProfileGuid("alpha")).Returns(string.Empty);

        Action act = () => _service.SetAppearance(new SetAppearanceDto
        {
            InstanceName = "alpha",
            Font = "Consolas",
        });

        act.Should().Throw<WslSquadException>().Which.Code.Should().Be(WslSquadErrorCode.TerminalProfileMissing);
    }

    [Fact]
    public void An_icon_image_alone_rewrites_the_profile_and_clears_the_text_icon()
    {
        WslTheme theme = WslTheme.Default();
        theme.SetIcon(new IconDrawing("AL", "#000000", "#111111", "#FFFFFF"));
        WslInstance model = new() { Name = "alpha", Theme = theme };
        _instances.Setup(instances => instances.GetInstance("alpha")).Returns(model);
        _terminal.Setup(terminal => terminal.GetAppearance("alpha")).Returns(WslTheme.Default());
        _instances.Setup(instances => instances.GetIconPath("alpha")).Returns("/fleet/alpha/terminal-icon.png");
        _terminal.Setup(terminal => terminal.GetProfileGuid("alpha")).Returns("guid");

        SetAppearanceResponseDto response = _service.SetAppearance(new SetAppearanceDto
        {
            InstanceName = "alpha",
            IconImage = "/icons/alpha.png",
        });

        response.Changes.Should().ContainSingle()
            .Which.Kind.Should().Be(AppearanceChangeKind.IconImage);
        _instances.Verify(instances => instances.SaveIcon("alpha", "/icons/alpha.png"), Times.Once);
        _terminal.Verify(
            terminal => terminal.DrawIcon(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IconDrawing>()),
            Times.Never);
        _terminal.Verify(terminal => terminal.SaveFragment(
            "alpha",
            "guid",
            It.Is<WslTheme>(theme =>
                theme.Icon == null &&
                theme.IconPath == "/fleet/alpha/terminal-icon.png")), Times.Once);
        _instances.Verify(instances => instances.SaveInstance(
            It.Is<WslInstance>(instance => instance.Theme != null && instance.Theme.Icon == null),
            string.Empty), Times.Once);
        _terminal.Verify(terminal => terminal.TouchSettings(), Times.Once);
    }

    [Fact]
    public void A_font_change_keeps_the_existing_icon_image()
    {
        _instances.Setup(instances => instances.GetInstance("alpha")).Returns(new WslInstance { Name = "alpha" });
        _terminal.Setup(terminal => terminal.GetAppearance("alpha")).Returns(WslTheme.Default());
        _instances.Setup(instances => instances.HasIcon("alpha")).Returns(true);
        _instances.Setup(instances => instances.GetIconPath("alpha")).Returns("/fleet/alpha/terminal-icon.png");
        _terminal.Setup(terminal => terminal.GetProfileGuid("alpha")).Returns("guid");

        _service.SetAppearance(new SetAppearanceDto
        {
            InstanceName = "alpha",
            Font = "Consolas",
        });

        _terminal.Verify(terminal => terminal.SaveFragment(
            "alpha",
            "guid",
            It.Is<WslTheme>(theme => theme.IconPath == "/fleet/alpha/terminal-icon.png")), Times.Once);
    }

    [Fact]
    public void Opens_the_pack_manager_from_the_catalog()
    {
        _packs.Setup(repository => repository.GetRoot()).Returns("/catalog/packs");
        _packs.Setup(repository => repository.GetAllPacks()).Returns([new WslPack { Name = "base", Family = "debian" }]);
        _instances.Setup(instances => instances.GetInstance("alpha")).Returns(new WslInstance
        {
            Name = "alpha",
            Family = "debian",
        });
        _terminal.Setup(terminal => terminal.HasWslProfile("alpha")).Returns(true);

        _service.OpenPackManager("alpha");

        _wsl.Verify(wsl => wsl.SavePackManager(
            "alpha",
            It.Is<string>(script => script.Contains("Wsl Squad - pack manager", StringComparison.Ordinal)),
            It.Is<string>(script => script.Contains("autoremove", StringComparison.Ordinal)),
            It.Is<string>(script => script.Contains("command -v bash", StringComparison.Ordinal)),
            "/catalog/packs"), Times.Once);
        _wsl.Verify(wsl => wsl.OpenPackManager("alpha", true), Times.Once);
        _terminal.Verify(terminal => terminal.TouchSettings(), Times.Once);
    }

    [Fact]
    public void Opens_the_pack_manager_without_a_terminal_profile()
    {
        _packs.Setup(repository => repository.GetRoot()).Returns("/catalog/packs");
        _packs.Setup(repository => repository.GetAllPacks()).Returns([new WslPack { Name = "base", Family = "debian" }]);
        _instances.Setup(instances => instances.GetInstance("alpha")).Returns(new WslInstance
        {
            Name = "alpha",
            Family = "debian",
        });
        _terminal.Setup(terminal => terminal.HasWslProfile("alpha")).Returns(false);

        _service.OpenPackManager("alpha");

        _wsl.Verify(wsl => wsl.OpenPackManager("alpha", false), Times.Once);
        _terminal.Verify(terminal => terminal.TouchSettings(), Times.Never);
    }

    [Fact]
    public void Refuses_to_open_the_pack_manager_without_a_known_family()
    {
        _instances.Setup(instances => instances.GetInstance("alpha")).Returns(new WslInstance { Name = "alpha" });
        _packs.Setup(repository => repository.GetAllPacks()).Returns([new WslPack { Name = "base", Family = "debian" }]);

        Action act = () => _service.OpenPackManager("alpha");

        act.Should().Throw<WslSquadException>().Which.Code.Should().Be(WslSquadErrorCode.PackManagerUnavailable);
        _wsl.Verify(wsl => wsl.SavePackManager(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
        _wsl.Verify(wsl => wsl.OpenPackManager(It.IsAny<string>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void Refuses_to_open_the_pack_manager_without_a_pack_for_the_family()
    {
        _instances.Setup(instances => instances.GetInstance("alpha")).Returns(new WslInstance
        {
            Name = "alpha",
            Family = "alpine",
        });
        _packs.Setup(repository => repository.GetAllPacks()).Returns([new WslPack { Name = "base", Family = "debian" }]);

        Action act = () => _service.OpenPackManager("alpha");

        act.Should().Throw<WslSquadException>().Which.Code.Should().Be(WslSquadErrorCode.PackManagerUnavailable);
    }

    [Fact]
    public void Refuses_to_restore_an_archive_without_a_tar()
    {
        _wsl.Setup(wsl => wsl.GetAllDistros()).Returns([]);
        _instances.Setup(instances => instances.GetFolder("alpha")).Returns("/fleet/alpha");
        _archives.Setup(archives => archives.GetTar("alpha_archive")).Returns(string.Empty);
        _archives.Setup(archives => archives.GetFolder("alpha_archive")).Returns("/archives/alpha_archive");

        Action act = () => _service.Restore(new RestoreDto
        {
            ArchiveName = "alpha_archive",
            InstanceName = "alpha",
        });

        act.Should().Throw<WslSquadException>().Which.Code.Should().Be(WslSquadErrorCode.ArchiveTarMissing);
    }

    [Fact]
    public void Remove_survives_a_docker_desktop_cleanup_failure()
    {
        _instances.Setup(instances => instances.GetFolder("alpha")).Returns("/fleet/alpha");
        _terminal.Setup(terminal => terminal.GetWslGuids("alpha")).Returns(new WslGuids([], null));
        _terminal.Setup(terminal => terminal.PruneGhostEntries("alpha", It.IsAny<IReadOnlyList<string>>()))
            .Returns(new PruneReport(0, []));
        _terminal.Setup(terminal => terminal.RemoveStaleFragments(It.IsAny<IReadOnlyList<string>>()))
            .Returns([]);
        _docker.Setup(docker => docker.RemoveDesktopState("alpha")).Throws(new IOException("locked"));

        Action act = () => _service.Remove(new RemoveDto { InstanceName = "alpha" });

        act.Should().NotThrow();
    }

    [Fact]
    public void Compact_survives_a_failed_restart()
    {
        _instances.Setup(instances => instances.GetVhdxSize("alpha")).Returns(100);
        _wsl.Setup(wsl => wsl.GetRunningNames()).Returns(["alpha"]);
        _wsl.Setup(wsl => wsl.Start("alpha")).Throws(new InvalidOperationException("no start"));

        CompactResponseDto response = _service.Compact(new CompactDto { InstanceName = "alpha" });

        response.Restarted.Should().BeFalse();
        response.Gained.Should().Be(0);
    }

    [Fact]
    public void Build_reports_a_look_failure_without_failing_the_build()
    {
        using TemporaryDirectory temp = new();
        string folder = temp.Combine("alpha");
        string recipe = temp.Write("recipe/Dockerfile", "FROM scratch");
        _wsl.Setup(wsl => wsl.GetAllDistros()).Returns([]);
        _wsl.Setup(wsl => wsl.GetFamily("alpha")).Returns("debian");
        _wsl.Setup(wsl => wsl.GetInInstance("alpha", It.IsAny<IReadOnlyList<string>>())).Returns([]);
        _instances.Setup(instances => instances.IsFolderOccupied("alpha")).Returns(false);
        _instances.Setup(instances => instances.GetFolder("alpha")).Returns(folder);
        _instances.Setup(instances => instances.GetIconPath("alpha")).Returns(Path.Combine(folder, "icon.png"));
        _docker.Setup(docker => docker.TestRunning()).Returns(true);
        _terminal.Setup(terminal => terminal.IsFontInstalled(It.IsAny<string>())).Returns(true);
        _terminal.Setup(terminal => terminal.DrawIcon("alpha", It.IsAny<string>(), null))
            .Returns(new IconDrawing("AL", "#000000", "#111111", "#FFFFFF"));
        _terminal.Setup(terminal => terminal.GetWslGuids("alpha")).Returns(new WslGuids([], "guid"));
        _terminal.Setup(terminal => terminal.PruneGhostEntries("alpha", It.IsAny<IReadOnlyList<string>>()))
            .Returns(new PruneReport(0, []));
        _terminal.Setup(terminal => terminal.RemoveStaleFragments(It.IsAny<IReadOnlyList<string>>()))
            .Returns([]);
        _terminal.Setup(terminal => terminal.SaveFragment("alpha", "guid", It.IsAny<WslTheme>()))
            .Throws(new InvalidOperationException("no save"));

        BuildResponseDto response = _service.Build(new BuildDto
        {
            Name = "alpha",
            BuildPath = recipe,
            Context = Path.GetDirectoryName(recipe),
            KeepImage = false,
        });

        response.Status.Should().Be(WslRecipeStatus.Warning);
        response.Notices.Should().Contain(notice => notice.Kind == BuildNoticeKind.LookFailed);
        _instances.Verify(instances => instances.SaveInstance(
            It.Is<WslInstance>(instance => instance.Family == "debian"),
            It.IsAny<string>()), Times.AtLeastOnce);
    }

    [Fact]
    public void Build_saves_the_recipe_image_when_asked()
    {
        using TemporaryDirectory temp = new();
        string folder = temp.Combine("alpha");
        string recipe = temp.Write("recipe/Dockerfile", "FROM scratch");
        SetupBuildTarget(folder);
        _docker.Setup(docker => docker.GetSavedImagePath("base")).Returns("/images/base.tar");
        _docker.Setup(docker => docker.SaveImage("wsl-squad:alpha", "/images/base.tar")).Returns(true);

        _service.Build(new BuildDto
        {
            Name = "alpha",
            RecipeName = "base",
            BuildPath = recipe,
            Context = Path.GetDirectoryName(recipe),
            KeepImage = true,
        });

        _docker.Verify(
            docker => docker.SaveImage("wsl-squad:alpha", "/images/base.tar"),
            Times.Once);
        _docker.Verify(docker => docker.RemoveImage("wsl-squad:alpha"), Times.Once);
    }

    [Fact]
    public void Build_reuses_the_saved_recipe_image()
    {
        using TemporaryDirectory temp = new();
        string folder = temp.Combine("alpha");
        string saved = temp.Write("images/base.tar", "image");
        SetupBuildTarget(folder);
        _docker.Setup(docker => docker.GetSavedImagePath("base")).Returns(saved);
        _docker.Setup(docker => docker.Load(saved, true)).Returns("ubuntu:24.04");

        _service.Build(new BuildDto
        {
            Name = "alpha",
            RecipeName = "base",
            UseSavedImage = true,
            Interactive = true,
        });

        _docker.Verify(docker => docker.Load(saved, true), Times.Once);
        _docker.Verify(docker => docker.Build(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>()), Times.Never);
        _docker.Verify(docker => docker.ExportRootfs("ubuntu:24.04", It.IsAny<string>(), true), Times.Once);
        _docker.Verify(docker => docker.SaveImage(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _docker.Verify(docker => docker.RemoveImage(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Build_refuses_a_missing_saved_recipe_image()
    {
        using TemporaryDirectory temp = new();
        string saved = temp.Combine("images/base.tar");
        _wsl.Setup(wsl => wsl.GetAllDistros()).Returns([]);
        _instances.Setup(instances => instances.IsFolderOccupied("alpha")).Returns(false);
        _docker.Setup(docker => docker.TestRunning()).Returns(true);
        _docker.Setup(docker => docker.GetSavedImagePath("base")).Returns(saved);

        Action act = () => _service.Build(new BuildDto
        {
            Name = "alpha",
            RecipeName = "base",
            UseSavedImage = true,
        });

        act.Should().Throw<WslSquadException>()
            .Which.Code.Should().Be(WslSquadErrorCode.InvalidBuildSource);
        _docker.Verify(docker => docker.Build(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public void Reports_whether_a_recipe_image_is_saved()
    {
        using TemporaryDirectory temp = new();
        string saved = temp.Write("images/base.tar", "image");
        _docker.Setup(docker => docker.GetSavedImagePath("base")).Returns(saved);
        _docker.Setup(docker => docker.GetSavedImagePath("missing")).Returns(temp.Combine("images/missing.tar"));

        _service.GetSavedRecipeImage("base").Should().Be(saved);
        _service.GetSavedRecipeImage("missing").Should().BeNull();
    }

    [Fact]
    public void Deletes_the_saved_recipe_image()
    {
        _service.DeleteSavedRecipeImage("base");

        _docker.Verify(docker => docker.DeleteSavedImage("base"), Times.Once);
    }

    [Fact]
    public void Build_unregisters_the_instance_when_its_file_cannot_be_written()
    {
        using TemporaryDirectory temp = new();
        string folder = temp.Combine("alpha");
        string recipe = temp.Write("recipe/Dockerfile", "FROM scratch");
        Directory.CreateDirectory(folder);
        _wsl.Setup(wsl => wsl.GetAllDistros()).Returns([]);
        _wsl.Setup(wsl => wsl.GetFamily("alpha")).Returns("debian");
        _wsl.Setup(wsl => wsl.GetInInstance("alpha", It.IsAny<IReadOnlyList<string>>())).Returns([]);
        _instances.Setup(instances => instances.IsFolderOccupied("alpha")).Returns(false);
        _instances.Setup(instances => instances.GetFolder("alpha")).Returns(folder);
        _instances.Setup(instances => instances.SaveInstance(It.IsAny<WslInstance>(), It.IsAny<string>()))
            .Throws(new IOException("disk full"));
        _docker.Setup(docker => docker.TestRunning()).Returns(true);

        Action act = () => _service.Build(new BuildDto
        {
            Name = "alpha",
            BuildPath = recipe,
            Context = Path.GetDirectoryName(recipe),
            KeepImage = false,
        });

        act.Should().Throw<WslSquadException>().Which.Code.Should().Be(WslSquadErrorCode.InstanceWriteFailed);
        _wsl.Verify(wsl => wsl.Unregister("alpha"), Times.Once);
        Directory.Exists(folder).Should().BeFalse();
        _terminal.Verify(terminal => terminal.SaveFragment(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<WslTheme>()), Times.Never);
    }

    [Fact]
    public void Build_survives_a_rootfs_tar_that_cannot_be_deleted()
    {
        using TemporaryDirectory temp = new();
        string folder = temp.Combine("alpha");
        string tar = temp.Combine("alpha-rootfs.tar");
        string recipe = temp.Write("recipe/Dockerfile", "FROM scratch");
        _wsl.Setup(wsl => wsl.GetAllDistros()).Returns([]);
        _wsl.Setup(wsl => wsl.GetInInstance("alpha", It.IsAny<IReadOnlyList<string>>())).Returns([]);
        _instances.Setup(instances => instances.IsFolderOccupied("alpha")).Returns(false);
        _instances.Setup(instances => instances.GetFolder("alpha")).Returns(folder);
        _instances.Setup(instances => instances.GetIconPath("alpha")).Returns(Path.Combine(folder, "icon.png"));
        _docker.Setup(docker => docker.TestRunning()).Returns(true);
        _docker.Setup(docker => docker.ExportRootfs(It.IsAny<string>(), tar, It.IsAny<bool>()))
            .Callback<string, string, bool>((_, _, _) => Directory.CreateDirectory(tar));
        _terminal.Setup(terminal => terminal.IsFontInstalled(It.IsAny<string>())).Returns(true);
        _terminal.Setup(terminal => terminal.DrawIcon("alpha", It.IsAny<string>(), null))
            .Returns(new IconDrawing("AL", "#000000", "#111111", "#FFFFFF"));
        _terminal.Setup(terminal => terminal.GetWslGuids("alpha")).Returns(new WslGuids([], "guid"));
        _terminal.Setup(terminal => terminal.PruneGhostEntries("alpha", It.IsAny<IReadOnlyList<string>>()))
            .Returns(new PruneReport(0, []));
        _terminal.Setup(terminal => terminal.RemoveStaleFragments(It.IsAny<IReadOnlyList<string>>()))
            .Returns([]);

        Action act = () => _service.Build(new BuildDto
        {
            Name = "alpha",
            BuildPath = recipe,
            Context = Path.GetDirectoryName(recipe),
            KeepImage = false,
        });

        act.Should().NotThrow();
    }

    private void SetupBuildTarget(string folder)
    {
        _wsl.Setup(wsl => wsl.GetAllDistros()).Returns([]);
        _wsl.Setup(wsl => wsl.GetFamily("alpha")).Returns("debian");
        _wsl.Setup(wsl => wsl.GetInInstance("alpha", It.IsAny<IReadOnlyList<string>>())).Returns([]);
        _instances.Setup(instances => instances.IsFolderOccupied("alpha")).Returns(false);
        _instances.Setup(instances => instances.GetFolder("alpha")).Returns(folder);
        _instances.Setup(instances => instances.GetIconPath("alpha")).Returns(Path.Combine(folder, "icon.png"));
        _docker.Setup(docker => docker.TestRunning()).Returns(true);
        _terminal.Setup(terminal => terminal.IsFontInstalled(It.IsAny<string>())).Returns(true);
        _terminal.Setup(terminal => terminal.DrawIcon("alpha", It.IsAny<string>(), null))
            .Returns(new IconDrawing("AL", "#000000", "#111111", "#FFFFFF"));
        _terminal.Setup(terminal => terminal.GetWslGuids("alpha")).Returns(new WslGuids([], "guid"));
        _terminal.Setup(terminal => terminal.PruneGhostEntries("alpha", It.IsAny<IReadOnlyList<string>>()))
            .Returns(new PruneReport(0, []));
        _terminal.Setup(terminal => terminal.RemoveStaleFragments(It.IsAny<IReadOnlyList<string>>()))
            .Returns([]);
    }
}

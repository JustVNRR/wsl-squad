using FluentAssertions;

using Moq;

using wsl_squad.Core.Dtos;
using wsl_squad.Core.Entities;
using wsl_squad.Core.Mappers;
using wsl_squad.Core.Repositories;
using wsl_squad.Core.Services;
using wsl_squad.Core.Tests.Support;

namespace wsl_squad.Core.Tests.Services;

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

        WslIndexDto row = _service.GetAll().Should().ContainSingle().Subject;

        row.Name.Should().Be("alpha");
        row.Size.Should().Be(1024);
        row.Docker.Should().Be("yes");
        row.Path.Should().Be("/fleet/alpha");
    }

    [Fact]
    public void Refuses_to_build_over_a_registered_instance()
    {
        _wsl.Setup(wsl => wsl.GetAllDistros()).Returns([new WslInstance { Name = "alpha" }]);

        Action act = () => _service.Build(new BuildDto { Name = "alpha" });

        act.Should().Throw<InvalidOperationException>().WithMessage("*registered on this machine*");
        _docker.VerifyNoOtherCalls();
    }

    [Fact]
    public void Refuses_to_build_over_an_occupied_folder()
    {
        _wsl.Setup(wsl => wsl.GetAllDistros()).Returns([]);
        _instances.Setup(instances => instances.IsFolderOccupied("alpha")).Returns(true);

        Action act = () => _service.Build(new BuildDto { Name = "alpha" });

        act.Should().Throw<InvalidOperationException>().WithMessage("*already exists*");
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

        act.Should().Throw<InvalidOperationException>().WithMessage("*Not enough room*");
    }

    [Fact]
    public void Refuses_to_apply_a_look_without_a_terminal_profile()
    {
        _instances.Setup(instances => instances.GetInstance("alpha")).Returns(new WslInstance { Name = "alpha" });
        _terminal.Setup(terminal => terminal.GetAppearance("alpha")).Returns(WslTheme.Default("alpha"));
        _terminal.Setup(terminal => terminal.GetProfileGuid("alpha")).Returns(string.Empty);

        Action act = () => _service.SetAppearance(new SetAppearanceDto
        {
            InstanceName = "alpha",
            Font = "Consolas",
        });

        act.Should().Throw<InvalidOperationException>().WithMessage("*no profile*");
    }

    [Fact]
    public void Filters_pack_candidates_and_removable_packs()
    {
        _packs.Setup(repository => repository.GetAllPacks()).Returns(
        [
            new WslPack { Name = "new-pack", Family = "debian" },
            new WslPack { Name = "other-family", Family = "arch" },
            new WslPack { Name = "installed", Offered = false },
        ]);
        _wsl.Setup(wsl => wsl.GetInstalledPacks("alpha")).Returns(["installed"]);
        _wsl.Setup(wsl => wsl.GetFamily("alpha")).Returns("debian");

        GetPacksResponseDto response = _service.GetPacks("alpha");

        response.Candidate.Select(pack => pack.Name).Should().Equal("new-pack");
        response.Removable.Should().BeEmpty();
    }

    [Fact]
    public void Offers_all_packs_when_none_are_installed()
    {
        _packs.Setup(repository => repository.GetAllPacks()).Returns(
        [
            new WslPack { Name = "shell", Family = "debian" },
            new WslPack { Name = "tools", Family = "debian" },
        ]);
        _wsl.Setup(wsl => wsl.GetInstalledPacks("alpha")).Returns([]);
        _wsl.Setup(wsl => wsl.GetFamily("alpha")).Returns("debian");

        GetPacksResponseDto response = _service.GetPacks("alpha");

        response.Installed.Should().BeEmpty();
        response.Removable.Should().BeEmpty();
        response.Candidate.Select(pack => pack.Name).Should().Equal("shell", "tools");
    }

    [Fact]
    public void Installs_a_pack_in_staging_before_promoting_it()
    {
        _packs.Setup(repository => repository.GetAllPacks()).Returns([new WslPack { Name = "base" }]);
        _packs.Setup(repository => repository.GetFolder("base")).Returns("/catalog/base");
        _instances.Setup(instances => instances.GetFolder("alpha")).Returns("/fleet/alpha");
        _wsl.Setup(wsl => wsl.GetInstalledPacks("alpha")).Returns([]);
        _wsl.Setup(wsl => wsl.CopyPackIntoStaging("alpha", "/catalog/base", "base")).Returns(0);
        _wsl.Setup(wsl => wsl.StagingPackHasFile("alpha", "base", "install_root.sh")).Returns(false);
        _wsl.Setup(wsl => wsl.RunStagingPackScript("alpha", "base", "install.sh", false, It.IsAny<string?>(), false))
            .Returns(0);
        _wsl.Setup(wsl => wsl.PromotePack("alpha", "base")).Returns(0);

        ManagePacksResponseDto response = _service.ManagePacks(new ManagePacksDto
        {
            InstanceName = "alpha",
            ToAdd = ["base"],
        });

        response.Outcome.Should().Be("ok");
        _wsl.Verify(wsl => wsl.CleanPackStaging("alpha"), Times.Once);
        _wsl.Verify(wsl => wsl.CopyPackIntoStaging("alpha", "/catalog/base", "base"), Times.Once);
        _wsl.Verify(wsl => wsl.RunStagingPackScript("alpha", "base", "install.sh", false, It.IsAny<string?>(), false), Times.Once);
        _wsl.Verify(wsl => wsl.PromotePack("alpha", "base"), Times.Once);
        _wsl.Verify(wsl => wsl.CopyPackIntoInstance(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _wsl.Verify(wsl => wsl.RemovePackFromInstance(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Discards_the_staging_folder_when_a_pack_fails_to_install()
    {
        _packs.Setup(repository => repository.GetAllPacks()).Returns([new WslPack { Name = "base" }]);
        _packs.Setup(repository => repository.GetFolder("base")).Returns("/catalog/base");
        _instances.Setup(instances => instances.GetFolder("alpha")).Returns("/fleet/alpha");
        _wsl.Setup(wsl => wsl.GetInstalledPacks("alpha")).Returns([]);
        _wsl.Setup(wsl => wsl.CopyPackIntoStaging("alpha", "/catalog/base", "base")).Returns(0);
        _wsl.Setup(wsl => wsl.StagingPackHasFile("alpha", "base", "install_root.sh")).Returns(true);
        _wsl.Setup(wsl => wsl.RunStagingPackScript("alpha", "base", "install_root.sh", true, It.IsAny<string?>(), false))
            .Returns(7);

        ManagePacksResponseDto response = _service.ManagePacks(new ManagePacksDto
        {
            InstanceName = "alpha",
            ToAdd = ["base"],
        });

        response.Outcome.Should().Be("failed");
        response.Failed.Should().Equal("base");
        _wsl.Verify(wsl => wsl.DiscardPackStaging("alpha", "base"), Times.Once);
        _wsl.Verify(wsl => wsl.PromotePack(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Opens_the_pack_manager_from_the_catalog()
    {
        _packs.Setup(repository => repository.GetRoot()).Returns("/catalog/packs");
        _terminal.Setup(terminal => terminal.HasWslProfile("alpha")).Returns(true);

        _service.OpenPackManager("alpha");

        _wsl.Verify(wsl => wsl.SavePackManager(
            "alpha",
            It.Is<string>(script => script.Contains("Wsl Squad - pack manager", StringComparison.Ordinal)),
            It.Is<string>(script => script.Contains("autoremove", StringComparison.Ordinal)),
            "/catalog/packs"), Times.Once);
        _wsl.Verify(wsl => wsl.OpenPackManager("alpha", true), Times.Once);
        _terminal.Verify(terminal => terminal.TouchSettings(), Times.Once);
        _wsl.Verify(wsl => wsl.GetInstalledPacks(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Opens_the_pack_manager_without_a_terminal_profile()
    {
        _packs.Setup(repository => repository.GetRoot()).Returns("/catalog/packs");
        _terminal.Setup(terminal => terminal.HasWslProfile("alpha")).Returns(false);

        _service.OpenPackManager("alpha");

        _wsl.Verify(wsl => wsl.OpenPackManager("alpha", false), Times.Once);
        _terminal.Verify(terminal => terminal.TouchSettings(), Times.Never);
    }

    [Fact]
    public void Refuses_to_remove_a_pack_required_by_another()
    {
        _packs.Setup(repository => repository.GetAllPacks()).Returns(
        [
            new WslPack { Name = "shell", Offered = false },
            new WslPack { Name = "tools", Requires = ["shell"] },
        ]);
        _wsl.Setup(wsl => wsl.GetInstalledPacks("alpha")).Returns(["shell", "tools"]);
        _instances.Setup(instances => instances.GetFolder("alpha")).Returns("/fleet/alpha");

        Action act = () => _service.ManagePacks(new ManagePacksDto
        {
            InstanceName = "alpha",
            ToRemove = ["shell"],
        });

        act.Should().Throw<InvalidOperationException>().WithMessage("*required by tools*");
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

        act.Should().Throw<InvalidOperationException>().WithMessage("*No tar*");
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
        _wsl.Setup(wsl => wsl.GetAllDistros()).Returns([]);
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
            BuildType = WslBuildType.Dockerfile,
            BuildPath = "Dockerfile",
            KeepImage = false,
        });

        response.Status.Should().Be(WslRecipeStatus.Warning);
        response.Messages.Should().Contain(message => message.Contains("the look failed", StringComparison.Ordinal));
    }

    [Fact]
    public void Build_survives_a_rootfs_tar_that_cannot_be_deleted()
    {
        using TemporaryDirectory temp = new();
        string folder = temp.Combine("alpha");
        string tar = temp.Combine("alpha-rootfs.tar");
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
            BuildType = WslBuildType.Dockerfile,
            BuildPath = "Dockerfile",
            KeepImage = false,
        });

        act.Should().NotThrow();
    }
}

using FluentAssertions;

using Moq;

using wsl_squad.Core.Dtos;
using wsl_squad.Core.Entities;
using wsl_squad.Core.Mappers;
using wsl_squad.Core.Repositories;
using wsl_squad.Core.Services;

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
        _wsl.Setup(wsl => wsl.GetHome("alpha")).Returns("/home/alpha");
        _wsl.Setup(wsl => wsl.GetInstalledPacks("alpha", "/home/alpha/.config/packs")).Returns(["installed"]);
        _wsl.Setup(wsl => wsl.GetFamily("alpha")).Returns("debian");

        GetPacksResponseDto response = _service.GetPacks("alpha");

        response.Candidate.Select(pack => pack.Name).Should().Equal("new-pack");
        response.Removable.Should().BeEmpty();
    }

    [Fact]
    public void Refuses_to_remove_a_pack_required_by_another()
    {
        _packs.Setup(repository => repository.GetAllPacks()).Returns(
        [
            new WslPack { Name = "shell", Offered = false },
            new WslPack { Name = "tools", Requires = ["shell"] },
        ]);
        _wsl.Setup(wsl => wsl.GetHome("alpha")).Returns("/home/alpha");
        _wsl.Setup(wsl => wsl.GetInstalledPacks("alpha", "/home/alpha/.config/packs")).Returns(["shell", "tools"]);
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
}

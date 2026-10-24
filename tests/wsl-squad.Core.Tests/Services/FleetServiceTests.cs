using FluentAssertions;

using Moq;

using WslSquad.Core.Dtos;
using WslSquad.Core.Entities;
using WslSquad.Core.Mappers;
using WslSquad.Core.Repositories;
using WslSquad.Core.Services;
using WslSquad.Core.Tests.Support;

namespace WslSquad.Core.Tests.Services;

public class FleetServiceTests
{
    private readonly Mock<IConfigRepository> _config = new();
    private readonly Mock<IWslRepository> _wsl = new();
    private readonly Mock<IInstanceRepository> _instances = new();
    private readonly Mock<IArchiveRepository> _archives = new();
    private readonly Mock<IInstanceService> _instanceService = new();
    private readonly FleetService _service;

    public FleetServiceTests()
    {
        _service = new FleetService(
            _config.Object,
            _wsl.Object,
            _instances.Object,
            _archives.Object,
            _instanceService.Object);
    }

    [Fact]
    public void Uses_the_saved_root_when_it_exists()
    {
        using TemporaryDirectory temp = new();
        _config.Setup(config => config.GetSavedRoot()).Returns(temp.Path);

        _service.GetRoot().Should().Be(temp.Path);
    }

    [Fact]
    public void Lists_only_marked_distributions_under_the_root()
    {
        using TemporaryDirectory temp = new();
        string folder = temp.Combine("alpha");
        Directory.CreateDirectory(folder);
        _config.Setup(config => config.GetSavedRoot()).Returns(temp.Path);
        _wsl.Setup(wsl => wsl.GetAllDistros()).Returns([new WslInstance { Name = "alpha" }]);
        _wsl.Setup(wsl => wsl.GetFolder("alpha")).Returns(folder);
        _instances.Setup(instances => instances.HasMarkerIn(folder)).Returns(true);

        _service.GetFleet().Should().Equal("alpha");
    }

    [Fact]
    public void Moves_the_fleet_root()
    {
        using TemporaryDirectory temp = new();
        string target = temp.Combine("new-root");

        string result = _service.SetRoot(target);

        result.Should().Be(target);
        _instances.Verify(instances => instances.EnsureFolder(target), Times.Once);
        _archives.Verify(archives => archives.MoveTo(target), Times.Once);
        _config.Verify(config => config.SaveRoot(target), Times.Once);
    }

    [Fact]
    public void Restore_after_root_move_writes_into_the_new_root()
    {
        using TemporaryDirectory temp = new();
        string oldRoot = temp.Combine("old");
        string newRoot = temp.Combine("new");
        string currentRoot = oldRoot;
        Mock<IWslRepository> wsl = new();
        Mock<ITerminalRepository> terminal = new();
        Mock<IDockerRepository> docker = new();
        Mock<IPackCatalogRepository> packs = new();
        Mock<IConfigRepository> config = new();
        Mock<IInstanceMapper> map = new();
        config.Setup(item => item.GetSavedRoot()).Returns(() => currentRoot);
        config.Setup(item => item.SaveRoot(It.IsAny<string>()))
            .Callback<string>(root => currentRoot = root);
        InstanceRepository instances = new(config.Object);
        ArchiveRepository archives = new(config.Object);
        terminal.Setup(item => item.GetProfileGuid(It.IsAny<string>())).Returns(string.Empty);
        wsl.Setup(item => item.GetAllDistros()).Returns([]);
        InstanceService instance = new(
            instances,
            archives,
            wsl.Object,
            terminal.Object,
            docker.Object,
            packs.Object,
            config.Object,
            map.Object);
        FleetService fleet = new(config.Object, wsl.Object, instances, archives, instance);
        archives.EnsureFolder("saved");
        archives.SaveInstance(new WslInstance { Name = "saved", IsArchive = true }, string.Empty);
        File.WriteAllBytes(Path.Combine(archives.GetFolder("saved"), "saved.tar.gz"), [1, 2, 3]);

        fleet.SetRoot(newRoot);

        string? importedFolder = null;
        string? importedTar = null;
        wsl.Setup(item => item.Import("restored", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .Callback<string, string, string, bool>((instanceName, folder, tar, interactive) =>
            {
                importedFolder = folder;
                importedTar = tar;
                _ = instanceName;
                _ = interactive;
                Directory.CreateDirectory(folder);
            });

        instance.Restore(new RestoreDto { ArchiveName = "saved", InstanceName = "restored" });

        importedFolder.Should().Be(Path.Combine(newRoot, "restored"));
        importedTar.Should().Be(Path.Combine(newRoot, "archives", "saved", "saved.tar.gz"));
        File.Exists(Path.Combine(newRoot, "restored", "instance.json")).Should().BeTrue();
        File.Exists(Path.Combine(newRoot, "restored", ".wsl-squad")).Should().BeTrue();
        File.Exists(Path.Combine(oldRoot, "restored", "instance.json")).Should().BeFalse();
    }
}

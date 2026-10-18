using FluentAssertions;

using wsl_squad.Core.Entities;
using wsl_squad.Core.Repositories;
using wsl_squad.Core.Tests.Support;

namespace wsl_squad.Core.Tests.Repositories;

public class InstanceRepositoryTests
{
    [Fact]
    public void Saves_and_reloads_an_instance()
    {
        using TemporaryDirectory temp = new();
        string root = temp.Combine("fleet");
        _ = Directory.CreateDirectory(temp.Combine("fleet", "alpha"));
        InstanceRepository repository = new(root);
        WslInstance instance = new()
        {
            Name = "alpha",
            DefaultUser = "alice",
            Theme = WslTheme.Default("alpha"),
        };
        instance.Theme!.SetIcon(new IconDrawing("AL", "#000000", "#111111", "#FFFFFF"));

        repository.SaveInstance(instance, "yes");

        WslInstance reloaded = repository.GetInstance("alpha");
        reloaded.Name.Should().Be("alpha");
        reloaded.DefaultUser.Should().Be("alice");
        reloaded.Theme!.FontName.Should().Be("MesloLGS NF");
        reloaded.Theme.Icon!.Text.Should().Be("AL");
        repository.GetSavedDocker("alpha").Should().Be("yes");
        File.ReadAllText(temp.Combine("fleet", "alpha", "instance.json"))
            .Should().Contain("\"DefaultUser\": \"alice\"");
    }

    [Fact]
    public void Writes_and_lists_the_marker()
    {
        using TemporaryDirectory temp = new();
        _ = Directory.CreateDirectory(temp.Combine("fleet", "alpha"));
        InstanceRepository repository = new(temp.Combine("fleet"));

        repository.WriteMarker("alpha", "build");

        repository.HasMarker("alpha").Should().BeTrue();
        repository.GetMarkedFolders().Should().Equal("alpha");
    }

    [Fact]
    public void Measures_the_disk_and_occupancy()
    {
        using TemporaryDirectory temp = new();
        _ = Directory.CreateDirectory(temp.Combine("fleet", "alpha"));
        File.WriteAllBytes(temp.Combine("fleet", "alpha", "ext4.vhdx"), new byte[1536]);
        InstanceRepository repository = new(temp.Combine("fleet"));

        repository.GetVhdxSize("alpha").Should().Be(1536);
        repository.IsFolderOccupied("alpha").Should().BeTrue();
        repository.GetFreeSpace(temp.Combine("fleet")).Should().BePositive();
    }

    [Fact]
    public void Removes_an_instance_folder()
    {
        using TemporaryDirectory temp = new();
        _ = Directory.CreateDirectory(temp.Combine("fleet", "alpha"));
        InstanceRepository repository = new(temp.Combine("fleet"));

        repository.RemoveFolder("alpha");

        Directory.Exists(temp.Combine("fleet", "alpha")).Should().BeFalse();
    }
}

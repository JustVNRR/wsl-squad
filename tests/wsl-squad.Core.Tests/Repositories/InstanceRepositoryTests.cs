using FluentAssertions;

using WslSquad.Core.Entities;
using WslSquad.Core.Repositories;
using WslSquad.Core.Tests.Support;

namespace WslSquad.Core.Tests.Repositories;

public class InstanceRepositoryTests
{
    [Fact]
    public void Saves_and_reloads_an_instance()
    {
        using TemporaryDirectory temp = new();
        string root = temp.Combine("fleet");
        Directory.CreateDirectory(temp.Combine("fleet", "alpha"));
        InstanceRepository repository = new(root);
        WslInstance instance = new()
        {
            Name = "alpha",
            Family = "debian",
            Theme = WslTheme.Default(),
        };
        instance.Theme!.SetIcon(new IconDrawing("AL", "#000000", "#111111", "#FFFFFF"));

        repository.SaveInstance(instance, "yes");

        WslInstance reloaded = repository.GetInstance("alpha");
        reloaded.Name.Should().Be("alpha");
        reloaded.Family.Should().Be("debian");
        reloaded.Theme!.FontName.Should().Be("MesloLGS NF");
        reloaded.Theme.Icon!.Text.Should().Be("AL");
        repository.GetSavedDocker("alpha").Should().Be("yes");
        File.ReadAllText(temp.Combine("fleet", "alpha", "instance.json"))
            .Should().Contain("\"Family\": \"debian\"");
    }

    [Fact]
    public void Writes_and_lists_the_marker()
    {
        using TemporaryDirectory temp = new();
        Directory.CreateDirectory(temp.Combine("fleet", "alpha"));
        InstanceRepository repository = new(temp.Combine("fleet"));

        repository.WriteMarker("alpha", "build");

        repository.HasMarker("alpha").Should().BeTrue();
        repository.GetMarkedFolders().Should().Equal("alpha");
    }

    [Fact]
    public void Measures_the_disk_and_occupancy()
    {
        using TemporaryDirectory temp = new();
        Directory.CreateDirectory(temp.Combine("fleet", "alpha"));
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
        Directory.CreateDirectory(temp.Combine("fleet", "alpha"));
        InstanceRepository repository = new(temp.Combine("fleet"));

        repository.RemoveFolder("alpha");

        Directory.Exists(temp.Combine("fleet", "alpha")).Should().BeFalse();
    }

    [Fact]
    public void RemoveFolder_survives_a_folder_that_cannot_be_deleted()
    {
        using TemporaryDirectory temp = new();
        string folder = temp.Combine("fleet", "alpha");
        Directory.CreateDirectory(folder);
        string file = Path.Combine(folder, "ext4.vhdx");
        File.WriteAllText(file, "x");
        FileStream? holder = null;
        InstanceRepository repository = new(temp.Combine("fleet"));

        try
        {
            if (OperatingSystem.IsWindows())
            {
                holder = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None);
            }
            else
            {
                File.SetUnixFileMode(
                    folder,
                    UnixFileMode.UserRead | UnixFileMode.UserExecute);
            }

            Action act = () => repository.RemoveFolder("alpha");

            act.Should().NotThrow();
        }
        finally
        {
            holder?.Dispose();

            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(
                    folder,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            }
        }
    }
}

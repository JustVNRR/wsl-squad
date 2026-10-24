using FluentAssertions;

using WslSquad.Core.Entities;
using WslSquad.Core.Repositories;
using WslSquad.Core.Tests.Support;

namespace WslSquad.Core.Tests.Repositories;

public class ArchiveRepositoryTests
{
    [Fact]
    public void Lists_archives_that_hold_a_tar()
    {
        using TemporaryDirectory temp = new();
        _ = Directory.CreateDirectory(temp.Combine("fleet", "archives", "alpha_archive"));
        File.WriteAllBytes(temp.Combine("fleet", "archives", "alpha_archive", "alpha_archive.tar.gz"), new byte[2048]);
        ArchiveRepository repository = new(temp.Combine("fleet", "archives"));

        IReadOnlyList<WslInstance> archives = repository.GetAllArchives();

        archives.Should().ContainSingle();
        archives[0].Name.Should().Be("alpha_archive");
        archives[0].IsArchive.Should().BeTrue();
        repository.GetTarSize("alpha_archive").Should().Be(2048);
    }

    [Fact]
    public void Saves_and_loads_archive_metadata()
    {
        using TemporaryDirectory temp = new();
        _ = Directory.CreateDirectory(temp.Combine("fleet", "archives", "alpha_archive"));
        ArchiveRepository repository = new(temp.Combine("fleet", "archives"));
        WslInstance archive = new()
        {
            Name = "alpha_archive",
            IsArchive = true,
            DefaultUser = "alice",
            Theme = WslTheme.Default(),
        };

        repository.SaveInstance(archive, "yes");

        WslInstance loaded = repository.GetInstance("alpha_archive");
        loaded.IsArchive.Should().BeTrue();
        loaded.DefaultUser.Should().Be("alice");
        repository.GetDocker("alpha_archive").Should().Be("yes");
    }

    [Fact]
    public void Deletes_an_archive_folder()
    {
        using TemporaryDirectory temp = new();
        _ = Directory.CreateDirectory(temp.Combine("fleet", "archives", "alpha_archive"));
        ArchiveRepository repository = new(temp.Combine("fleet", "archives"));

        repository.DeleteArchive("alpha_archive");

        Directory.Exists(temp.Combine("fleet", "archives", "alpha_archive")).Should().BeFalse();
    }

    [Fact]
    public void Moves_the_archive_folder_to_a_new_root()
    {
        using TemporaryDirectory temp = new();
        _ = Directory.CreateDirectory(temp.Combine("fleet", "archives", "alpha_archive"));
        File.WriteAllText(temp.Combine("fleet", "archives", "alpha_archive", "keep.txt"), "x");
        ArchiveRepository repository = new(temp.Combine("fleet", "archives"));
        string target = temp.Combine("moved");

        repository.MoveTo(target);

        File.Exists(temp.Combine("moved", "archives", "alpha_archive", "keep.txt")).Should().BeTrue();
        Directory.Exists(temp.Combine("fleet", "archives")).Should().BeFalse();
    }
}

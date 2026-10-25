using FluentAssertions;

using WslSquad.Core.Repositories;
using WslSquad.Core.Tests.Support;

namespace WslSquad.Core.Tests.Repositories;

public class DockerRepositoryTests
{
    [Fact]
    public void Deletes_a_saved_image_and_its_empty_folder()
    {
        using TemporaryDirectory temp = new();
        string image = temp.Write("dockerimages/base/base.tar", "image");
        string folder = temp.Combine("dockerimages", "base");
        DockerRepository repository = new(temp.Path);

        repository.DeleteSavedImage("base").Should().BeTrue();

        File.Exists(image).Should().BeFalse();
        Directory.Exists(folder).Should().BeFalse();
    }

    [Fact]
    public void Refuses_to_delete_a_file_outside_the_saved_image_root()
    {
        using TemporaryDirectory temp = new();
        string outside = temp.Write("outside.tar", "image");
        DockerRepository repository = new(temp.Path);

        repository.DeleteSavedImage("../outside").Should().BeFalse();

        File.Exists(outside).Should().BeTrue();
    }
}

using FluentAssertions;

using wsl_squad.Core.Dtos;
using wsl_squad.Core.Repositories;

namespace wsl_squad.Core.Tests.Repositories;

public class TerminalRepositoryTests
{
    [Fact]
    public void Exposes_the_reference_icon_pairs()
    {
        TerminalRepository repository = new(string.Empty);

        IReadOnlyList<WslIconPairDto> pairs = repository.GetIconPairs();

        pairs.Should().HaveCount(8);
        pairs.Should().Contain(pair => pair.Name == "orange" && pair.Top == "#CF7040");
    }

    [Fact]
    public void Builds_the_fragment_path_from_the_instance_name()
    {
        TerminalRepository repository = new(string.Empty);

        repository.GetFragmentPath("alpha").Should().EndWith(Path.Combine("wsl-squad", "alpha.json"));
    }

    [Fact]
    public void DeleteFragment_survives_a_directory_at_the_fragment_path()
    {
        TerminalRepository repository = new(string.Empty);
        string name = $"catch-{Guid.NewGuid():N}";
        string path = repository.GetFragmentPath(name);
        _ = Directory.CreateDirectory(path);

        try
        {
            Action act = () => repository.DeleteFragment(name);

            act.Should().NotThrow();
        }
        finally
        {
            Directory.Delete(path, recursive: true);
        }
    }
}

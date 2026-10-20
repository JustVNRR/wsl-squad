using FluentAssertions;

using wsl_squad.Core.Dtos;
using wsl_squad.Core.Entities;
using wsl_squad.Core.Repositories;
using wsl_squad.Core.Tests.Support;

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
    public void Delegates_icon_drawing_to_the_interface_layer()
    {
        using TemporaryDirectory temp = new();
        string? seen = null;
        TerminalRepository repository = new(
            temp.Path,
            (name, path, drawing) =>
            {
                seen = path;

                return new IconDrawing("AL", "#000000", "#111111", "#FFFFFF");
            });

        IconDrawing result = repository.DrawIcon("alpha", temp.Combine("icon.png"), null);

        result.Text.Should().Be("AL");
        seen.Should().Be(temp.Combine("icon.png"));
    }

    [Fact]
    public void Refuses_to_draw_without_a_painter()
    {
        TerminalRepository repository = new(string.Empty);

        Action act = () => repository.DrawIcon("alpha", "/tmp/icon.png", null);

        act.Should().Throw<PlatformNotSupportedException>();
    }

    [Fact]
    public void Builds_the_fragment_path_from_the_instance_name()
    {
        TerminalRepository repository = new(string.Empty);

        repository.GetFragmentPath("alpha").Should().EndWith(Path.Combine("wsl-squad", "alpha.json"));
    }
}

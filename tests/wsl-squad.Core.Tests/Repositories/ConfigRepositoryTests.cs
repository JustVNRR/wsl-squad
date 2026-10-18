using FluentAssertions;

using wsl_squad.Core.Repositories;
using wsl_squad.Core.Tests.Support;

namespace wsl_squad.Core.Tests.Repositories;

public class ConfigRepositoryTests
{
    [Fact]
    public void Lists_themes()
    {
        using TemporaryDirectory temp = new();
        _ = temp.Write("themes/phosphor.xaml", "<ResourceDictionary/>");
        ConfigRepository repository = new(temp.Path);

        repository.GetThemeNames().Should().Equal("phosphor");
    }

    [Fact]
    public void Prompt_fonts_keep_only_bundled_fonts_with_prompt_icons()
    {
        using TemporaryDirectory temp = new();
        ExtractFont("fonts/MesloLGS NF Regular-7c66a6d9/MesloLGS NF Regular.ttf", temp.Combine("fonts", "meslo", "Meslo.ttf"));
        ConfigRepository repository = new(temp.Path);

        IReadOnlyList<string> fonts = repository.GetPromptFontNames();

        FontGlyphReader.HasGlyph(temp.Combine("fonts", "meslo", "Meslo.ttf"), 'A').Should().BeTrue();
        FontGlyphReader.HasIconGlyph(temp.Combine("fonts", "meslo", "Meslo.ttf")).Should().BeTrue();
        _ = fonts.Should().Contain("MesloLGS NF");
    }

    private static void ExtractFont(string resource, string target)
    {
        _ = Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        using Stream source = typeof(ConfigRepository).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Missing resource {resource}.");
        using FileStream output = File.Create(target);
        source.CopyTo(output);
    }

    [Fact]
    public void Reads_dockerfile_sources_and_their_family()
    {
        using TemporaryDirectory temp = new();
        _ = temp.Write("dockerfiles/ubuntu/Dockerfile", "FROM --platform=linux/amd64 ubuntu:24.04\n");
        ConfigRepository repository = new(temp.Path);

        SourceRow row = repository.GetDockerfiles().Should().ContainSingle().Subject;
        row.Name.Should().Be("ubuntu");
        row.Family.Should().Be("debian");
    }

    [Fact]
    public void Reads_recipes()
    {
        using TemporaryDirectory temp = new();
        _ = temp.Write("recipes/alpine/Dockerfile", "FROM alpine:3.20\n");
        ConfigRepository repository = new(temp.Path);

        SourceRow row = repository.GetRecipes().Should().ContainSingle().Subject;
        row.Name.Should().Be("alpine");
        row.Family.Should().Be("alpine");
    }

    [Fact]
    public void Copies_and_removes_a_source_within_the_catalogue()
    {
        using TemporaryDirectory temp = new();
        string source = temp.Write("source.Dockerfile", "FROM debian:12\n");
        ConfigRepository repository = new(temp.Path);

        string target = repository.CopyRecipe("dockerfiles", source, "Dockerfile");
        File.Exists(target).Should().BeTrue();

        repository.RemoveRecipe(Path.GetDirectoryName(target)!);
        Directory.Exists(Path.GetDirectoryName(target)).Should().BeFalse();
    }

    [Fact]
    public void Refuses_to_remove_a_folder_outside_the_catalogue()
    {
        using TemporaryDirectory temp = new();
        _ = temp.Write("outside/file.txt", "x");
        ConfigRepository repository = new(temp.Combine("assets"));

        Action act = () => repository.RemoveRecipe(temp.Combine("outside", "file.txt"));

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Copies_and_deletes_a_theme()
    {
        using TemporaryDirectory temp = new();
        string source = temp.Write("amber.xaml", "<ResourceDictionary/>");
        ConfigRepository repository = new(temp.Path);

        string target = repository.CopyTheme(source);
        File.Exists(target).Should().BeTrue();
        repository.GetThemeNames().Should().Contain("amber");

        repository.DeleteTheme("amber");
        File.Exists(target).Should().BeFalse();
    }
}

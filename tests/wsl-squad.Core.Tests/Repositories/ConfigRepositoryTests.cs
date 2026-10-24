using System.Text.Json;

using FluentAssertions;

using WslSquad.Core.Repositories;
using WslSquad.Core.Tests.Support;

namespace WslSquad.Core.Tests.Repositories;

public class ConfigRepositoryTests
{
    [Fact]
    public void Prompt_fonts_keep_only_bundled_fonts_with_prompt_icons()
    {
        using TemporaryDirectory temp = new();
        ExtractFont("fonts/MesloLGS NF Regular-7c66a6d9/MesloLGS NF Regular.ttf", temp.Combine("fonts", "meslo", "Meslo.ttf"));
        ConfigRepository repository = new(temp.Path);

        IReadOnlyList<string> fonts = repository.GetPromptFontNames();

        FontGlyphReader.HasGlyph(temp.Combine("fonts", "meslo", "Meslo.ttf"), 'A').Should().BeTrue();
        FontGlyphReader.HasIconGlyph(temp.Combine("fonts", "meslo", "Meslo.ttf")).Should().BeTrue();
        fonts.Should().Contain("MesloLGS NF");
    }

    [Fact]
    public void The_language_preference_round_trips_and_keeps_other_settings()
    {
        using TemporaryDirectory temp = new();
        ConfigRepository repository = new(temp.Path);

        repository.GetLanguage().Should().BeEmpty();

        temp.Write("config.json", """
            {
              "schemaVersion": 1,
              "language": "fr",
              "fleetRoot": "D:\\WS"
            }
            """);

        repository.GetLanguage().Should().Be("fr");

        repository.SaveLanguage("de");

        using JsonDocument document = JsonDocument.Parse(File.ReadAllText(temp.Combine("config.json")));
        document.RootElement.GetProperty("schemaVersion").GetInt32().Should().Be(1);
        document.RootElement.GetProperty("fleetRoot").GetString().Should().Be("D:\\WS");
        document.RootElement.GetProperty("language").GetString().Should().Be("de");
        repository.GetLanguage().Should().Be("de");
    }

    private static void ExtractFont(string resource, string target)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        string normalized = resource.Replace('\\', '/');
        string[] names = typeof(ConfigRepository).Assembly.GetManifestResourceNames();
        string? name = Array.Find(names, candidate => candidate.Replace('\\', '/') == normalized)
            ?? throw new InvalidOperationException($"Missing resource {resource}.");
        using Stream source = typeof(ConfigRepository).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException($"Missing resource {resource}.");
        using FileStream output = File.Create(target);
        source.CopyTo(output);
    }

    [Fact]
    public void Reads_dockerfile_sources()
    {
        using TemporaryDirectory temp = new();
        temp.Write("dockerfiles/ubuntu/Dockerfile", "FROM --platform=linux/amd64 ubuntu:24.04\n");
        ConfigRepository repository = new(temp.Path);

        SourceRow row = repository.GetDockerfiles().Should().ContainSingle().Subject;
        row.Name.Should().Be("ubuntu");
    }

    [Fact]
    public void Seeds_dockerfiles_and_onboardings_from_ingredients()
    {
        using TemporaryDirectory temp = new();
        temp.Write("ingredients/Dockerfile", "FROM alpine:3.20\n");
        temp.Write("ingredients/onboarding.sh", "#!/bin/sh\n");
        ConfigRepository repository = new(temp.Path, temp.Path);

        SourceRow dockerfile = repository.GetDockerfiles().Should().ContainSingle().Subject;
        SourceRow onboarding = repository.GetOnboardings().Should().ContainSingle().Subject;

        dockerfile.Name.Should().Be("Dockerfile");
        dockerfile.Path.Should().Be(temp.Combine("dockerfiles", "Dockerfile", "Dockerfile"));
        onboarding.Name.Should().Be("onboarding");
        onboarding.Path.Should().Be(temp.Combine("onboardings", "onboarding", "onboarding.sh"));
        File.ReadAllText(dockerfile.Path).Should().Be("FROM alpine:3.20\n");
    }

    [Fact]
    public void Reads_recipes()
    {
        using TemporaryDirectory temp = new();
        temp.Write("recipes/alpine/Dockerfile", "FROM alpine:3.20\n");
        ConfigRepository repository = new(temp.Path);

        SourceRow row = repository.GetRecipes().Should().ContainSingle().Subject;
        row.Name.Should().Be("alpine");
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
        temp.Write("outside/file.txt", "x");
        ConfigRepository repository = new(temp.Combine("assets"));

        Action act = () => repository.RemoveRecipe(temp.Combine("outside", "file.txt"));

        act.Should().Throw<InvalidOperationException>();
    }

}

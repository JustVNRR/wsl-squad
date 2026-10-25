using System.Text.Json;

using FluentAssertions;

using WslSquad.Core.Exceptions;
using WslSquad.Core.Repositories;
using WslSquad.Core.Tests.Support;
using WslSquad.Core.Infrastructure;

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
    public void The_root_is_stored_under_the_assets_directory()
    {
        using TemporaryDirectory temp = new();
        ConfigRepository repository = new(temp.Path);

        repository.SaveRoot("C:\\fleet");

        repository.GetSavedRoot().Should().Be("C:\\fleet");
        File.Exists(temp.Combine("root.txt")).Should().BeTrue();
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
    public void Reads_recipes()
    {
        using TemporaryDirectory temp = new();
        temp.Write("recipes/alpine/Dockerfile", "FROM alpine:3.20\n");
        ConfigRepository repository = new(temp.Path);

        SourceRow row = repository.GetRecipes().Should().ContainSingle().Subject;
        row.Name.Should().Be("alpine");
    }

    [Fact]
    public void Creates_a_recipe_with_its_exact_name()
    {
        using TemporaryDirectory temp = new();
        ConfigRepository repository = new(temp.Path);

        string target = repository.CreateRecipe("debian");

        target.Should().Be(temp.Combine("recipes", "debian"));
        Directory.Exists(target).Should().BeTrue();
    }

    [Fact]
    public void Duplicates_a_recipe_with_its_files()
    {
        using TemporaryDirectory temp = new();
        string source = temp.Write("recipes/default/Dockerfile", "FROM alpine:3.20\n");
        temp.Write("recipes/default/onboarding.sh", "#!/bin/sh\n");
        ConfigRepository repository = new(temp.Path);

        string target = repository.DuplicateRecipe(
            Path.GetDirectoryName(source)!,
            "default-copy");

        target.Should().Be(temp.Combine("recipes", "default-copy"));
        File.ReadAllText(Path.Combine(target, "Dockerfile")).Should().Be("FROM alpine:3.20\n");
        File.Exists(Path.Combine(target, "onboarding.sh")).Should().BeTrue();
    }

    [Fact]
    public void Refuses_to_create_a_recipe_that_already_exists()
    {
        using TemporaryDirectory temp = new();
        temp.Write("recipes/default/Dockerfile", "FROM alpine:3.20\n");
        ConfigRepository repository = new(temp.Path);

        Action act = () => repository.CreateRecipe("default");

        act.Should().Throw<WslSquadException>()
            .Which.Code.Should().Be(WslSquadErrorCode.RecipeAlreadyExists);
    }

    [Fact]
    public void Refuses_to_duplicate_a_recipe_onto_an_existing_folder()
    {
        using TemporaryDirectory temp = new();
        string source = temp.Write("recipes/default/Dockerfile", "FROM alpine:3.20\n");
        temp.Write("recipes/default-copy/Dockerfile", "FROM debian:12\n");
        ConfigRepository repository = new(temp.Path);

        Action act = () => repository.DuplicateRecipe(
            Path.GetDirectoryName(source)!,
            "default-copy");

        act.Should().Throw<WslSquadException>()
            .Which.Code.Should().Be(WslSquadErrorCode.RecipeAlreadyExists);
    }

    [Fact]
    public void Refuses_to_duplicate_a_folder_outside_the_catalogue()
    {
        using TemporaryDirectory temp = new();
        temp.Write("outside/Dockerfile", "FROM alpine:3.20\n");
        ConfigRepository repository = new(temp.Path);

        Action act = () => repository.DuplicateRecipe(temp.Combine("outside"), "copy");

        act.Should().Throw<InvalidOperationException>();
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

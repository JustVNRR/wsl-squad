using System.Text.Json;

using FluentAssertions;

using WslSquad.Core.Services;
using WslSquad.Core.Tests.Support;

namespace WslSquad.Core.Tests.Architecture;

public class CatalogBootstrapTests
{
    [Fact]
    public void Ensure_extracts_the_language_catalogue_and_preserves_same_version_edits()
    {
        using TemporaryDirectory temp = new();

        CatalogBootstrap.Ensure(temp.Path);

        string english = temp.Combine("languages", "en.json");
        File.Exists(english).Should().BeTrue();

        using (JsonDocument document = JsonDocument.Parse(File.ReadAllText(english)))
        {
            document.RootElement.GetProperty("code").GetString().Should().Be("en");
        }

        File.WriteAllText(english, "custom");
        string french = temp.Write("languages/fr.json", """{ "code": "fr" }""");

        CatalogBootstrap.Ensure(temp.Path);

        File.ReadAllText(english).Should().Be("custom");
        File.Exists(french).Should().BeTrue();
    }

    [Fact]
    public void Ensure_refreshes_embedded_assets_after_a_version_change()
    {
        using TemporaryDirectory temp = new();

        CatalogBootstrap.Ensure(temp.Path);

        string english = temp.Combine("languages", "en.json");
        string original = File.ReadAllText(english);
        string version = temp.Combine("assets.version");
        string customPack = temp.Write("packs/my-pack/file.txt", "mine");

        File.WriteAllText(english, "custom");
        File.WriteAllText(version, "0.0.0.0");

        CatalogBootstrap.Ensure(temp.Path);

        File.ReadAllText(english).Should().Be(original);
        File.ReadAllText(version).Trim().Should().NotBe("0.0.0.0");
        File.ReadAllText(temp.Combine("backup-0.0.0.0", "languages", "en.json"))
            .Should().Be("custom");
        File.ReadAllText(customPack).Should().Be("mine");
    }

    [Fact]
    public void Bundled_language_catalogues_cover_the_same_keys()
    {
        using JsonDocument english = JsonDocument.Parse(CatalogBootstrap.ReadText("languages/en.json"));
        using JsonDocument french = JsonDocument.Parse(CatalogBootstrap.ReadText("languages/fr.json"));

        french.RootElement.GetProperty("code").GetString().Should().Be("fr");
        french.RootElement.GetProperty("displayName").GetString().Should().Be("Français");

        HashSet<string> englishKeys = [.. english.RootElement
            .GetProperty("strings")
            .EnumerateObject()
            .Select(property => property.Name)];
        HashSet<string> frenchKeys = [.. french.RootElement
            .GetProperty("strings")
            .EnumerateObject()
            .Select(property => property.Name)];

        frenchKeys.Should().BeEquivalentTo(englishKeys);
    }
}

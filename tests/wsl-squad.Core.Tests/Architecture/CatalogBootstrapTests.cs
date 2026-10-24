using System.Text.Json;

using FluentAssertions;

using WslSquad.Core.Services;
using WslSquad.Core.Tests.Support;

namespace WslSquad.Core.Tests.Architecture;

public class CatalogBootstrapTests
{
    [Fact]
    public void Ensure_extracts_the_language_catalogue_without_overwriting_it()
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

using FluentAssertions;

using WslSquad.Core.Repositories;
using WslSquad.Presentation.Localization;
using WslSquad.Presentation.Tests.Support;

namespace WslSquad.Presentation.Tests.Localization;

public class LocalizerTests
{
    [Fact]
    public void Bundled_languages_are_available_by_default()
    {
        Localizer localizer = new();

        localizer.CurrentLanguage.Should().Be(Localizer.English);
        localizer["Language"].Should().Be("Language");
        localizer.Languages.Should().Contain(language => language.Code == Localizer.English);
        localizer.Languages.Should().Contain(language => language.Code == "fr");
    }

    [Fact]
    public void An_unknown_key_is_returned_unchanged()
    {
        Localizer localizer = new();

        localizer["Missing"].Should().Be("Missing");
    }

    [Fact]
    public void Switching_language_updates_the_text_and_notifies()
    {
        Localizer localizer = new();
        localizer.RegisterLanguage(
            new UiLanguage("fr", "Français"),
            new Dictionary<string, string>
            {
                ["Language"] = "Langue",
                ["LanguageChanged"] = "Langue : {0}",
            });
        List<string?> changed = [];
        localizer.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        localizer.SetLanguage("fr");

        localizer.CurrentLanguage.Should().Be("fr");
        localizer["Language"].Should().Be("Langue");
        localizer.Format("LanguageChanged", "Français").Should().Be("Langue : Français");
        localizer.Languages.Should().Contain(language => language.Code == "fr");
        changed.Should().Contain("Item[]").And.Contain(nameof(Localizer.CurrentLanguage));
    }

    [Fact]
    public void A_key_missing_from_a_catalogue_falls_back_to_english()
    {
        Localizer localizer = new();
        localizer.RegisterLanguage(
            new UiLanguage("fr", "Français"),
            new Dictionary<string, string>
            {
                ["Language"] = "Langue",
            });

        localizer.SetLanguage("fr");

        localizer["Language"].Should().Be("Langue");
        localizer["SettingsTitle"].Should().Be("Settings");
    }

    [Fact]
    public void External_language_files_override_bundled_values_and_keep_missing_keys()
    {
        using TemporaryDirectory temp = new();
        string directory = temp.Combine("languages");
        temp.Write("languages/fr.json", """
            {
              "code": "fr",
              "displayName": "Français",
              "strings": {
                "Language": "Langue client"
              }
            }
            """);
        Localizer localizer = new();

        localizer.LoadLanguages(directory);
        localizer.SetLanguage("fr");

        localizer["Language"].Should().Be("Langue client");
        localizer["SettingsTitle"].Should().Be("Paramètres");
        localizer["CliChoiceIcon"].Should().Be("icône");
        localizer.Languages.Should().ContainEquivalentOf(new UiLanguage("fr", "Français"));
    }

    [Fact]
    public void A_malformed_language_file_is_ignored()
    {
        using TemporaryDirectory temp = new();
        temp.Write("languages/broken.json", "{ not json");

        Localizer localizer = new();
        Action load = () => localizer.LoadLanguages(temp.Combine("languages"));

        load.Should().NotThrow();
        localizer.Languages.Should().Contain(language => language.Code == Localizer.English);
        localizer.Languages.Should().Contain(language => language.Code == "fr");
    }

    [Fact]
    public void A_bad_placeholder_falls_back_to_the_english_string()
    {
        using TemporaryDirectory temp = new();
        temp.Write("languages/fr.json", """
            {
              "code": "fr",
              "displayName": "Français",
              "strings": {
                "LanguageChanged": "Langue {oops"
              }
            }
            """);
        Localizer localizer = new();
        localizer.LoadLanguages(temp.Combine("languages"));
        localizer.SetLanguage("fr");

        localizer.Format("LanguageChanged", "Français").Should().Be("Language: Français");
    }

    [Fact]
    public void A_format_missing_its_argument_returns_the_raw_english_template()
    {
        Localizer localizer = new();

        localizer.Format("LanguageChanged").Should().Be("Language: {0}");
    }

    [Fact]
    public void The_selected_language_is_saved_and_restored()
    {
        using TemporaryDirectory temp = new();
        temp.Write("languages/fr.json", """
            {
              "code": "fr",
              "displayName": "Français",
              "strings": {
                "Language": "Langue"
              }
            }
            """);
        string directory = temp.Combine("languages");
        Localizer first = new();
        first.LoadLanguages(directory);
        first.UsePreferences(new ConfigRepository(temp.Path));

        first.SetLanguage("fr");

        File.ReadAllText(temp.Combine("config.json")).Should().Contain("\"language\": \"fr\"");

        Localizer second = new();
        second.LoadLanguages(directory);
        second.UsePreferences(new ConfigRepository(temp.Path));

        second.CurrentLanguage.Should().Be("fr");
        second["Language"].Should().Be("Langue");
    }
}

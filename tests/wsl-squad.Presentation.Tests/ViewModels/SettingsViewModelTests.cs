using FluentAssertions;

using Moq;

using WslSquad.Core.Dtos;
using WslSquad.Core.Services;
using WslSquad.Presentation.Gui.Ports;
using WslSquad.Presentation.Localization;
using WslSquad.Presentation.Tests.Support;
using WslSquad.Presentation.Gui.ViewModels;

namespace WslSquad.Presentation.Tests.ViewModels;

[Collection("Localizer state")]
public class SettingsViewModelTests
{
    private readonly TestThemeService _ui = new()
    {
        Theme = new UiThemeSettings
        {
            FontFamily = "Cascadia",
            FontSize = 13,
            ColourSet = "amber",
            ColourVariant = "Dark",
        },
        ColorThemes = ["amber", "ice"],
        FontChoices = ["VT323"],
    };

    private readonly Mock<IPackCatalogService> _catalogue = new();
    private readonly Mock<IFleetService> _fleet = new();
    private readonly TestLanguageService _languages = new();
    private readonly TestDialogService _dialogs = new();

    public SettingsViewModelTests()
    {
        _fleet.Setup(fleet => fleet.GetRoot()).Returns("/fleet");
        _catalogue.Setup(catalogue => catalogue.GetAllPacks()).Returns(
        [
            new WslPackDto { Name = "tools", Family = "shell", Description = "Tools" },
        ]);
    }

    [Fact]
    public void Shows_the_current_theme_root_and_packs()
    {
        SettingsViewModel viewModel = Create();

        viewModel.Root.Should().Be("/fleet");
        viewModel.WslConfigPath.Should().EndWith(".wslconfig");
        viewModel.Fonts.Select(row => row.Label).Should().Equal("Cascadia", "VT323");
        viewModel.SelectedFont.Should().Be("Cascadia");
        viewModel.SelectedSize.Should().Be(13);
        viewModel.SelectedColour.Should().Be("amber");
        viewModel.Packs.Select(row => row.Label).Should().Equal("tools (shell)");
        viewModel.CanPack.Should().BeTrue();
        viewModel.CanTheme.Should().BeTrue();
    }

    [Fact]
    public void Changing_a_choice_previews_it_immediately()
    {
        SettingsViewModel viewModel = Create();

        viewModel.ColourIndex = 1;
        viewModel.FontIndex = 1;
        viewModel.SizeIndex = 2;
        viewModel.Variant = "Light";

        _ui.Previewed.Should().NotBeEmpty();
        _ui.Previewed[^1].Should().Be(("VT323", 15, "ice", "Light"));
    }

    [Fact]
    public void Close_returns_the_selected_settings()
    {
        SettingsViewModel viewModel = Create();
        bool closed = false;
        viewModel.CloseRequested += (_, _) => closed = true;

        viewModel.CloseCommand.Execute(null);

        viewModel.Result.Should().Be(new SettingsDto(
            new SettingsAppearanceDto
            {
                Font = "Cascadia",
                FontSize = 13,
                ColourScheme = "amber",
                Variant = "Dark",
            },
            null));
        closed.Should().BeTrue();
    }

    [Fact]
    public void Uploading_a_font_adds_and_selects_it()
    {
        SettingsViewModel viewModel = Create();
        List<string?> changed = [];
        viewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        string? family = viewModel.UploadFont("/tmp/font.ttf");

        family.Should().Be("Cascadia Mono");
        viewModel.Fonts.Select(row => row.Label).Should()
            .Contain($"Cascadia Mono{Localizer.Instance["JustUploadedSuffix"]}");
        viewModel.SelectedFont.Should().Be("Cascadia Mono");
        changed.Should().Contain(nameof(SettingsViewModel.Fonts));
    }

    [Fact]
    public void Deleting_the_selected_theme_asks_first_and_keeps_a_visible_selection()
    {
        SettingsViewModel viewModel = Create();
        _dialogs.ConfirmAnswer = true;

        viewModel.DeleteTheme("amber");

        _dialogs.LastConfirm!.Value.Title.Should().Be("Delete theme");
        _dialogs.LastConfirm.Value.Question.Should().Contain("amber");
        _ui.DeletedThemes.Should().Contain("amber");
        viewModel.Colours.Should().NotBeEmpty();
        viewModel.SelectedColour.Should().NotBeNullOrEmpty();
        viewModel.ColourIndex.Should().BeInRange(0, viewModel.Colours.Count - 1);
    }

    [Fact]
    public void Deleting_a_theme_can_be_cancelled()
    {
        SettingsViewModel viewModel = Create();
        _dialogs.ConfirmAnswer = false;

        viewModel.DeleteTheme("amber");

        _ui.DeletedThemes.Should().BeEmpty();
        viewModel.Colours.Should().Contain(row => row.Name == "amber");
    }

    [Fact]
    public void Duplicating_a_pack_creates_it_and_reloads_the_catalogue()
    {
        SettingsViewModel viewModel = Create();
        _dialogs.PackDuplicateAnswer = ("copy", "Copy");
        _catalogue.Setup(catalogue => catalogue.GetAllPacks()).Returns(
        [
            new WslPackDto { Name = "tools", Family = "shell" },
            new WslPackDto { Name = "copy", Family = "shell" },
        ]);

        viewModel.DuplicatePack();

        _catalogue.Verify(catalogue => catalogue.CreatePack(It.Is<CreatePackDto>(dto =>
            dto.SourceName == "tools" && dto.NewName == "copy" && dto.Description == "Copy")), Times.Once);
        viewModel.Packs.Select(row => row.Label).Should().Contain("copy (shell)");
    }

    [Fact]
    public void Deleting_a_pack_reloads_the_catalogue()
    {
        SettingsViewModel viewModel = Create();
        _catalogue.Setup(catalogue => catalogue.GetAllPacks()).Returns([]);
        _dialogs.ConfirmAnswer = true;

        viewModel.DeletePack();

        _catalogue.Verify(catalogue => catalogue.DeletePack("tools"), Times.Once);
        viewModel.Packs.Should().BeEmpty();
        viewModel.CanPack.Should().BeFalse();
    }

    [Fact]
    public void Deleting_a_pack_asks_first_and_names_the_dependents()
    {
        _catalogue.Setup(catalogue => catalogue.GetAllPacks()).Returns(
        [
            new WslPackDto
            {
                Name = "tools",
                Family = "shell",
                Description = "Tools",
                Dependents = ["other", "another"],
            },
        ]);
        SettingsViewModel viewModel = Create();
        _dialogs.ConfirmAnswer = false;

        viewModel.DeletePack();

        _dialogs.LastConfirm.Should().NotBeNull();
        _dialogs.LastConfirm!.Value.Title.Should().Be("Remove the pack");
        _dialogs.LastConfirm.Value.Question.Should().Contain("- other");
        _dialogs.LastConfirm.Value.Question.Should().Contain("- another");
        _dialogs.LastConfirm.Value.Danger.Should().Be("Removing it may break them.");
        _catalogue.Verify(catalogue => catalogue.DeletePack(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Editing_the_root_returns_the_new_root()
    {
        SettingsViewModel viewModel = Create();
        _dialogs.RootMoveAnswer = "/new-fleet";

        viewModel.EditRoot();

        viewModel.Root.Should().Be("/new-fleet");
        viewModel.Result!.NewRoot.Should().Be("/new-fleet");
    }

    [Fact]
    public void Wsl_config_errors_are_reported()
    {
        SettingsViewModel viewModel = Create();
        _fleet.Setup(fleet => fleet.WslConfig()).Throws(new InvalidOperationException("no profile"));

        viewModel.WslConfig();

        viewModel.ConfigError.Should().Contain("no profile");
    }

    [Fact]
    public void Uploading_a_font_through_the_file_dialog_adds_it()
    {
        SettingsViewModel viewModel = Create();
        _dialogs.OpenFileAnswer = "/tmp/font.ttf";

        viewModel.UploadFontCommand.Execute(null);

        viewModel.SelectedFont.Should().Be("Cascadia Mono");
        _dialogs.LastOpenFile!.Value.Filter.Should().Contain("*.ttf");
    }

    [Fact]
    public void Uploading_a_theme_through_the_file_dialog_adds_it()
    {
        SettingsViewModel viewModel = Create();
        _dialogs.OpenFileAnswer = "/tmp/theme.xaml";

        viewModel.UploadThemeCommand.Execute(null);

        viewModel.SelectedColour.Should().Be("mine");
        _dialogs.LastOpenFile!.Value.Filter.Should().Contain("*.xaml");
    }

    [Fact]
    public void Exporting_a_theme_uses_the_save_dialog()
    {
        SettingsViewModel viewModel = Create();
        _dialogs.SaveFileAnswer = "/tmp/out.xaml";

        viewModel.ExportThemeCommand.Execute(null);

        _dialogs.LastSaveFile!.Value.DefaultFileName.Should().Be("amber.xaml");
        _ui.ExportedThemes.Should().Contain(("/themes/amber.xaml", "/tmp/out.xaml"));
    }

    [Fact]
    public void Uploading_a_theme_adds_it_to_the_list()
    {
        SettingsViewModel viewModel = Create();

        viewModel.UploadTheme("/tmp/theme.xaml");

        viewModel.Colours.Select(row => row.Label).Should()
            .Contain($"mine{Localizer.Instance["JustUploadedSuffix"]}");
        viewModel.SelectedColour.Should().Be("mine");
    }

    [Fact]
    public void Replacing_an_existing_theme_asks_first()
    {
        SettingsViewModel viewModel = Create();
        _dialogs.ConfirmAnswer = false;

        viewModel.UploadTheme("/tmp/amber.xaml");

        _dialogs.LastConfirm!.Value.Title.Should().Be("Replace theme");
        _dialogs.LastConfirm.Value.Question.Should().Contain("amber");
        viewModel.Colours.Select(row => row.Label).Should()
            .NotContain($"mine{Localizer.Instance["JustUploadedSuffix"]}");
    }

    [Fact]
    public void Uploading_a_language_through_the_file_dialog_reloads_it()
    {
        SettingsViewModel viewModel = Create();
        _dialogs.OpenFileAnswer = "/tmp/english.json";
        _dialogs.ConfirmAnswer = true;
        _languages.Code = "en";
        _languages.UploadedCode = "en";

        viewModel.UploadLanguageCommand.Execute(null);

        _dialogs.LastOpenFile!.Value.Filter.Should().Contain("*.json");
        _dialogs.LastConfirm!.Value.Title.Should().Be("Replace language");
        _languages.Uploaded.Should().Contain("/tmp/english.json");
        viewModel.SelectedLanguageCode.Should().Be("en");
        viewModel.Languages.Should().Contain(row =>
            row.Label == $"English{Localizer.Instance["JustUploadedSuffix"]}");
    }

    [Fact]
    public void Uploading_an_invalid_language_reports_the_error()
    {
        SettingsViewModel viewModel = Create();
        _languages.Code = null;

        viewModel.UploadLanguage("/tmp/bad.json");

        viewModel.LanguageError.Should().Be("The chosen file is not a language catalog.");
        _languages.Uploaded.Should().BeEmpty();
    }

    [Fact]
    public void Replacing_an_existing_language_asks_first()
    {
        SettingsViewModel viewModel = Create();
        _dialogs.ConfirmAnswer = false;
        _languages.Code = "en";

        viewModel.UploadLanguage("/tmp/english.json");

        _dialogs.LastConfirm!.Value.Title.Should().Be("Replace language");
        _dialogs.LastConfirm.Value.Question.Should().Contain("en");
        _languages.Uploaded.Should().BeEmpty();
    }

    [Fact]
    public void Deleting_a_language_asks_first_and_calls_the_service()
    {
        SettingsViewModel viewModel = Create();
        _dialogs.ConfirmAnswer = true;

        viewModel.DeleteLanguage("fr");

        _dialogs.LastConfirm!.Value.Title.Should().Be("Delete language");
        _dialogs.LastConfirm.Value.Question.Should().Contain("'fr'");
        _languages.Deleted.Should().Contain("fr");
    }

    [Fact]
    public void Deleting_a_language_removes_it_from_the_list()
    {
        SettingsViewModel viewModel = Create();
        _dialogs.ConfirmAnswer = true;

        viewModel.DeleteLanguage("fr");

        _languages.Deleted.Should().Contain("fr");
        viewModel.Languages.Should().NotContain(row => row.Code == "fr");
        viewModel.SelectedLanguageCode.Should().Be("en");
    }

    [Fact]
    public void Deleting_english_switches_to_the_next_language()
    {
        SettingsViewModel viewModel = Create();
        _dialogs.ConfirmAnswer = true;

        try
        {
            viewModel.DeleteLanguage("en");

            viewModel.Languages.Should().NotContain(row => row.Code == "en");
            viewModel.SelectedLanguageCode.Should().Be("fr");
            viewModel.LanguageIndex.Should().Be(0);
            Localizer.Instance.CurrentLanguage.Should().Be("fr");
        }
        finally
        {
            Localizer.Instance.SetLanguage(Localizer.English);
        }
    }

    [Fact]
    public void Exporting_a_language_uses_the_save_dialog()
    {
        SettingsViewModel viewModel = Create();
        _dialogs.SaveFileAnswer = "/tmp/en.json";

        viewModel.ExportLanguageCommand.Execute(null);

        _dialogs.LastSaveFile!.Value.DefaultFileName.Should().Be("en.json");
        _languages.Exported.Should().Contain(("en", "/tmp/en.json"));
    }

    [Fact]
    public void Opening_a_language_calls_the_service()
    {
        SettingsViewModel viewModel = Create();

        viewModel.OpenLanguage("en");

        _languages.Opened.Should().Contain("en");
    }

    private SettingsViewModel Create()
    {
        return new SettingsViewModel(_ui, _languages, _catalogue.Object, _fleet.Object, _dialogs);
    }
}

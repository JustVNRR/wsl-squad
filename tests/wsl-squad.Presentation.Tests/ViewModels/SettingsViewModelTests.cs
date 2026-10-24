using FluentAssertions;

using Moq;

using WslSquad.Core.Dtos;
using WslSquad.Core.Services;
using WslSquad.Presentation.Gui.Ports;
using WslSquad.Presentation.Tests.Support;
using WslSquad.Presentation.Gui.ViewModels;

namespace WslSquad.Presentation.Tests.ViewModels;

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
            new SetAppearanceDto
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
        viewModel.Fonts.Select(row => row.Label).Should().Contain("Cascadia Mono  (just uploaded)");
        viewModel.SelectedFont.Should().Be("Cascadia Mono");
        changed.Should().Contain(nameof(SettingsViewModel.Fonts));
    }

    [Fact]
    public void Deleting_the_selected_theme_keeps_a_visible_selection()
    {
        SettingsViewModel viewModel = Create();

        viewModel.DeleteTheme("amber");

        viewModel.Colours.Should().NotBeEmpty();
        viewModel.SelectedColour.Should().NotBeNullOrEmpty();
        viewModel.ColourIndex.Should().BeInRange(0, viewModel.Colours.Count - 1);
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
    public void Uploading_a_theme_adds_it_to_the_list()
    {
        SettingsViewModel viewModel = Create();

        viewModel.UploadTheme("/tmp/theme.xaml");

        viewModel.Colours.Select(row => row.Label).Should().Contain("mine  (just uploaded)");
        viewModel.SelectedColour.Should().Be("mine");
    }

    private SettingsViewModel Create()
    {
        return new SettingsViewModel(_ui, _catalogue.Object, _fleet.Object, _dialogs);
    }
}

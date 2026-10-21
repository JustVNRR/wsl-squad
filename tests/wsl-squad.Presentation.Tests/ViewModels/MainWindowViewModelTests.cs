using FluentAssertions;

using Moq;

using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Settings;
using wsl_squad.Presentation.Tests.Support;
using wsl_squad.Presentation.ViewModels;

namespace wsl_squad.Presentation.Tests.ViewModels;

public class MainWindowViewModelTests
{
    private readonly Mock<IInstanceService> _instance = new();
    private readonly Mock<IFleetService> _fleet = new();
    private readonly Mock<IPackCatalogService> _catalogue = new();
    private readonly TestThemeService _ui = new();
    private readonly TestDialogService _dialogs = new();
    private readonly RecordingLogger _logger = new();
    private readonly MainWindowViewModel _viewModel;

    public MainWindowViewModelTests()
    {
        _fleet.Setup(fleet => fleet.GetRoot()).Returns("/fleet");
        _instance.Setup(service => service.GetAll()).Returns(
        [
            new WslIndexDto { Name = "alpha", Size = 2048, Docker = "no" },
        ]);
        _instance.Setup(service => service.GetBuildSources()).Returns(new GetBuildSourcesResponseDto());
        _instance.Setup(service => service.GetAllSavedImages()).Returns([]);
        _catalogue.Setup(catalogue => catalogue.GetAllPacks()).Returns([]);
        _ui.Theme = new UiThemeSettings
        {
            FontFamily = "VT323",
            FontSize = 15,
            ColourSet = "amber",
            ColourVariant = "Dark",
        };

        _viewModel = new MainWindowViewModel(
            _instance.Object,
            _fleet.Object,
            _ui,
            _catalogue.Object,
            _dialogs,
            _logger);
    }

    [Fact]
    public void Refresh_loads_the_root_and_the_rows()
    {
        _viewModel.RefreshCommand.Execute(null);

        _viewModel.Root.Should().Be("/fleet");
        _viewModel.Rows.Should().ContainSingle();
        _viewModel.Rows[0].Name.Should().Be("alpha");
        _viewModel.Rows[0].Size.Should().Be("2 KB");
        _logger.Entries.Should().Contain(entry => entry.Message == "1 row(s) loaded.");
    }

    [Fact]
    public void Refresh_reports_an_empty_fleet()
    {
        _instance.Setup(service => service.GetAll()).Returns([]);

        _viewModel.RefreshCommand.Execute(null);

        _viewModel.Rows.Should().BeEmpty();
        _logger.Entries.Should().Contain(entry => entry.Message == "No WSL SQUAD instances found.");
    }

    [Fact]
    public void Refresh_reports_a_read_error()
    {
        _fleet.Setup(fleet => fleet.GetRoot()).Throws(new InvalidOperationException("gone"));

        _viewModel.RefreshCommand.Execute(null);

        _logger.Entries.Should().Contain(entry => entry.Level == "ERROR");
        _dialogs.Errors.Should().ContainSingle();
    }

    [Fact]
    public void Theme_toggles_and_persists_the_variant()
    {
        _viewModel.ThemeCommand.Execute(null);

        _viewModel.Variant.Should().Be("Light");
        _ui.Theme.ColourVariant.Should().Be("Light");
    }

    [Fact]
    public async Task Open_calls_the_instance_service()
    {
        _instance.Setup(service => service.Open("alpha")).Returns(new OpenResponseDto { InstanceName = "alpha" });

        await _viewModel.OpenCommand.ExecuteAsync("alpha");

        _instance.Verify(service => service.Open("alpha"), Times.Once);
    }

    [Fact]
    public async Task Archive_uses_the_dialogue_request()
    {
        ArchiveDto request = new() { InstanceName = "alpha", Format = "tar.gz", Permit = true };
        _dialogs.ArchiveAnswer = request;

        await _viewModel.ArchiveCommand.ExecuteAsync("alpha");

        _instance.Verify(service => service.Archive(request), Times.Once);
    }

    [Fact]
    public async Task Add_runs_the_build_in_a_terminal()
    {
        BuildDto request = new() { Name = "beta", BuildType = WslBuildType.Dockerfile, BuildPath = "/x" };
        _dialogs.BuildAnswer = request;

        await _viewModel.AddCommand.ExecuteAsync(null);

        _dialogs.BuildTerminal.Should().Be(request);
        _instance.Verify(service => service.Build(It.IsAny<BuildDto>()), Times.Never);
    }

    [Fact]
    public async Task Docker_registers_when_the_row_is_not_registered()
    {
        _viewModel.RefreshCommand.Execute(null);
        _dialogs.DockerRegisterAnswer = new DockerDialogResult("alpha", true);

        await _viewModel.DockerCommand.ExecuteAsync("alpha");

        _instance.Verify(service => service.RegisterDocker(It.Is<RegisterDockerDto>(dto =>
            dto.InstanceName == "alpha" && dto.Restart)), Times.Once);
    }

    [Fact]
    public void Editing_packs_opens_the_distro_manager()
    {
        _viewModel.EditCommand.Execute("alpha");

        _instance.Verify(service => service.OpenPackManager("alpha"), Times.Once);
        _instance.Verify(service => service.GetPacks(It.IsAny<string>()), Times.Never);
        _instance.Verify(service => service.ManagePacks(It.IsAny<ManagePacksDto>()), Times.Never);
    }

    [Fact]
    public void Editing_packs_reports_a_manager_failure()
    {
        _instance.Setup(service => service.OpenPackManager("alpha"))
            .Throws(new InvalidOperationException("no terminal"));

        _viewModel.EditCommand.Execute("alpha");

        _dialogs.Errors.Should().ContainSingle(entry =>
            (entry.Message ?? string.Empty).Contains("no terminal", StringComparison.Ordinal));
    }

    [Fact]
    public void Theme_toggles_and_reapplies_the_look()
    {
        _viewModel.ThemeCommand.Execute(null);

        _ui.Theme.ColourVariant.Should().Be("Light");
        _viewModel.Variant.Should().Be("Light");
        _ui.Previewed.Should().Contain(("VT323", 15, "amber", "Light"));
    }

    [Fact]
    public async Task Settings_applies_the_outcome()
    {
        _dialogs.SettingsAnswer = new SettingsOutcome("Cascadia", 13, "amber", "Dark", null);

        await _viewModel.SettingsCommand.ExecuteAsync(null);

        _ui.Theme.FontFamily.Should().Be("Cascadia");
        _ui.Theme.FontSize.Should().Be(13);
        _viewModel.ColourSet.Should().Be("amber");
    }

    [Fact]
    public async Task Remove_uses_the_dialogue_request()
    {
        RemoveDto request = new() { InstanceName = "alpha", ArchiveFirst = true };
        _dialogs.RemoveAnswer = request;

        await _viewModel.RemoveCommand.ExecuteAsync("alpha");

        _instance.Verify(service => service.Remove(request), Times.Once);
    }
}

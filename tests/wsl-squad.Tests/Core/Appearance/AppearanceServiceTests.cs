using System.IO.Abstractions.TestingHelpers;

using FluentAssertions;

using wsl_squad.Core.Application.Appearance;
using wsl_squad.Core.Domain.Appearance;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Fleet;
using wsl_squad.Tests.Support;

namespace wsl_squad.Tests.Core.Appearance;

public class AppearanceServiceTests
{
    private readonly LifecycleFixture _fixture = new();
    private readonly FakeWindowsTerminalStore _terminal = new();
    private readonly FakeIconRenderer _icons = new();

    private AppearanceService CreateService()
    {
        return new AppearanceService(
            _fixture.FileSystem,
            _fixture.Instances,
            _fixture.Metadata,
            _terminal,
            _icons,
            _fixture.Clock);
    }

    private void PrepareInstanceWithAppearance()
    {
        _fixture.AddInstance(defaultUser: "jane");
        InstanceMetadata metadata = _fixture.Metadata.Read("/fleet/demo").Value;
        metadata.Appearance = new InstanceAppearance
        {
            Font = "Old Font",
            ColorScheme = "One Half Dark",
            TabTitle = "Old",
            Icon = new InstanceIcon
            {
                Text = "AB",
                Top = "#111111",
                Bottom = "#222222",
                TextColor = "#FFFFFF",
            },
        };
        _fixture.Metadata.Save("/fleet/demo", metadata);
    }

    [Fact]
    public void Returns_the_current_appearance()
    {
        PrepareInstanceWithAppearance();

        OperationResult<AppearanceOutcome> result = CreateService().Get(LifecycleFixture.Root, "demo");

        result.IsSuccess.Should().BeTrue();
        result.Value.Font.Should().Be("Old Font");
        result.Value.ColorScheme.Should().Be("One Half Dark");
        result.Value.TabTitle.Should().Be("Old");
        result.Value.Icon!.Text.Should().Be("AB");
        result.Value.HasProfile.Should().BeTrue();
    }

    [Fact]
    public void Merges_requested_fields_and_preserves_the_rest()
    {
        PrepareInstanceWithAppearance();

        OperationResult<AppearanceOutcome> result = CreateService().Set(
            LifecycleFixture.Root,
            "demo",
            new AppearanceRequest(Font: "New Font"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Font.Should().Be("New Font");
        result.Value.ColorScheme.Should().Be("One Half Dark");
        result.Value.Icon!.Text.Should().Be("AB");

        InstanceAppearance? saved = _fixture.Metadata.Read("/fleet/demo").Value.Appearance;
        saved!.Font.Should().Be("New Font");
        saved.Icon!.Text.Should().Be("AB");
        _terminal.Fragments.Should().ContainSingle();
        _terminal.Fragments[0].Font.Should().Be("New Font");
        _terminal.Fragments[0].ColorScheme.Should().Be("One Half Dark");
    }

    [Fact]
    public void Updates_icon_settings()
    {
        PrepareInstanceWithAppearance();

        OperationResult<AppearanceOutcome> result = CreateService().Set(
            LifecycleFixture.Root,
            "demo",
            new AppearanceRequest(IconText: "Z", IconTop: "#0000FF", IconTextColor: "#FF0000"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Icon!.Text.Should().Be("Z");
        result.Value.Icon.Top.Should().Be("#0000FF");
        result.Value.Icon.Bottom.Should().Be("#222222");
        result.Value.Icon.TextColor.Should().Be("#FF0000");
    }

    [Fact]
    public void Renders_an_icon_when_icon_visuals_change()
    {
        PrepareInstanceWithAppearance();

        OperationResult<AppearanceOutcome> result = CreateService().Set(
            LifecycleFixture.Root,
            "demo",
            new AppearanceRequest(IconText: "Z"));

        result.IsSuccess.Should().BeTrue();
        _icons.Rendered.Should().ContainSingle().Which.Should().Be("/fleet/demo/terminal-icon.png");
        _fixture.Metadata.Read("/fleet/demo").Value.Appearance!.Icon!.Image
            .Should().Be("/fleet/demo/terminal-icon.png");
        _terminal.Fragments.Should().ContainSingle();
        _terminal.Fragments[0].IconPath.Should().Be("/fleet/demo/terminal-icon.png");
    }

    [Fact]
    public void Warns_when_icon_rendering_fails()
    {
        PrepareInstanceWithAppearance();
        _icons.Fail = true;

        OperationResult<AppearanceOutcome> result = CreateService().Set(
            LifecycleFixture.Root,
            "demo",
            new AppearanceRequest(IconText: "Z"));

        result.Status.Should().Be(OperationStatus.Warning);
        result.Warnings.Should().Contain(warning => warning.Contains("icon rendering", StringComparison.Ordinal));
        _fixture.Metadata.Read("/fleet/demo").Value.Appearance!.Icon!.Text.Should().Be("Z");
        _fixture.Metadata.Read("/fleet/demo").Value.Appearance!.Icon!.Image.Should().BeNull();
    }

    [Theory]
    [InlineData("TOOLONG")]
    [InlineData("a-b")]
    public void Rejects_invalid_icon_text(string text)
    {
        PrepareInstanceWithAppearance();

        OperationResult<AppearanceOutcome> result = CreateService().Set(
            LifecycleFixture.Root,
            "demo",
            new AppearanceRequest(IconText: text));

        result.Error!.Code.Should().Be(ErrorCode.Validation);
        _terminal.Fragments.Should().BeEmpty();
    }

    [Fact]
    public void Rejects_invalid_icon_colours()
    {
        PrepareInstanceWithAppearance();

        OperationResult<AppearanceOutcome> result = CreateService().Set(
            LifecycleFixture.Root,
            "demo",
            new AppearanceRequest(IconTop: "#12345"));

        result.Error!.Code.Should().Be(ErrorCode.Validation);
    }

    [Fact]
    public void Rejects_a_missing_icon_image()
    {
        PrepareInstanceWithAppearance();

        OperationResult<AppearanceOutcome> result = CreateService().Set(
            LifecycleFixture.Root,
            "demo",
            new AppearanceRequest(IconImage: "/missing/icon.png"));

        result.Error!.Code.Should().Be(ErrorCode.Validation);
    }

    [Fact]
    public void Rejects_an_unknown_colour_scheme()
    {
        PrepareInstanceWithAppearance();

        OperationResult<AppearanceOutcome> result = CreateService().Set(
            LifecycleFixture.Root,
            "demo",
            new AppearanceRequest(ColorScheme: "Unknown Scheme"));

        result.Error!.Code.Should().Be(ErrorCode.Validation);
    }

    [Fact]
    public void Saves_metadata_and_warns_when_no_terminal_profile_exists()
    {
        PrepareInstanceWithAppearance();
        _terminal.ProfileGuid = null;

        OperationResult<AppearanceOutcome> result = CreateService().Set(
            LifecycleFixture.Root,
            "demo",
            new AppearanceRequest(Font: "New Font"));

        result.Status.Should().Be(OperationStatus.Warning);
        result.Warnings.Should().Contain(warning => warning.Contains("profile", StringComparison.Ordinal));
        _terminal.Fragments.Should().BeEmpty();
        _fixture.Metadata.Read("/fleet/demo").Value.Appearance!.Font.Should().Be("New Font");
    }

    [Fact]
    public void Writes_a_warning_when_the_fragment_cannot_be_written()
    {
        PrepareInstanceWithAppearance();
        _terminal.WriteFailure = new IOException("locked");

        OperationResult<AppearanceOutcome> result = CreateService().Set(
            LifecycleFixture.Root,
            "demo",
            new AppearanceRequest(Font: "New Font"));

        result.Status.Should().Be(OperationStatus.Warning);
        _fixture.Metadata.Read("/fleet/demo").Value.Appearance!.Font.Should().Be("New Font");
    }

    [Fact]
    public void Rejects_an_unknown_instance()
    {
        OperationResult<AppearanceOutcome> result = CreateService().Get(LifecycleFixture.Root, "missing");

        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }

    [Fact]
    public void Creates_metadata_when_it_is_missing()
    {
        _fixture.FileSystem.AddFile("/fleet/demo/.wsl-squad", new MockFileData("{}"));

        OperationResult<AppearanceOutcome> result = CreateService().Set(
            LifecycleFixture.Root,
            "demo",
            new AppearanceRequest(Font: "New Font"));

        result.IsSuccess.Should().BeTrue();
        _fixture.Metadata.Read("/fleet/demo").Value.Appearance!.Font.Should().Be("New Font");
    }
}

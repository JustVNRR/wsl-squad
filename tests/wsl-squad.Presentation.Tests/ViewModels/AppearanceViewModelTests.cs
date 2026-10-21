using FluentAssertions;

using wsl_squad.Core.Dtos;
using wsl_squad.Presentation.ViewModels;

namespace wsl_squad.Presentation.Tests.ViewModels;

public class AppearanceViewModelTests
{
    [Fact]
    public void Appearance_exposes_the_current_choices()
    {
        SetAppearanceViewModel viewModel = Create();

        viewModel.Lead.Should().Be("ALPHA'S APPEARANCE.");
        viewModel.NoFragment.Should().BeFalse();
        viewModel.IconText.Should().Be("AL");
        viewModel.PairLabels.Should().Contain("orange  (current)");
        viewModel.FontLabels.Should().Contain("VT323  (current)");
        viewModel.SchemeLabels.Should().Contain("One Half Dark  (current)");
        viewModel.ApplyCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public void Appearance_rejects_an_invalid_icon_text()
    {
        SetAppearanceViewModel viewModel = Create();

        viewModel.IconText = "toolong";

        viewModel.IconError.Should().NotBeEmpty();
        viewModel.ApplyCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void Appearance_returns_a_core_request()
    {
        SetAppearanceViewModel viewModel = Create();
        viewModel.IconText = "be";

        viewModel.ApplyCommand.Execute(null);

        viewModel.Result.Should().NotBeNull();
        viewModel.Result!.InstanceName.Should().Be("alpha");
        viewModel.Result.Font.Should().Be("VT323");
        viewModel.Result.ColourScheme.Should().Be("One Half Dark");
        viewModel.Result.Icon!.Text.Should().Be("BE");
        viewModel.Result.Icon.Top.Should().Be("#CF7040");
        viewModel.Result.IconImage.Should().BeEmpty();
    }

    [Fact]
    public void Appearance_keeps_an_uploaded_image_until_the_monogram_changes()
    {
        SetAppearanceViewModel viewModel = Create();
        viewModel.SetImage("/tmp/icon.png");

        viewModel.ApplyCommand.Execute(null);

        viewModel.Result!.IconImage.Should().Be("/tmp/icon.png");

        viewModel = Create();
        viewModel.IconText = "BE";
        viewModel.SetImage("/tmp/icon.png");
        viewModel.IconText = "CE";

        viewModel.ImagePath.Should().BeEmpty();
    }

    [Fact]
    public void Appearance_warns_without_a_terminal_fragment()
    {
        SetAppearanceViewModel viewModel = Create(hasFragment: false);

        viewModel.NoFragment.Should().BeTrue();
    }

    private static SetAppearanceViewModel Create(bool hasFragment = true)
    {
        return new SetAppearanceViewModel(
            "alpha",
            new GetAppearanceResponseDto
            {
                Font = "VT323",
                ColourScheme = "One Half Dark",
                Icon = new WslIconDto
                {
                    Text = "AL",
                    Top = "#CF7040",
                    Bottom = "#B95E30",
                    TextColor = "#FFFFFF",
                },
                HasFragment = hasFragment,
                IconPath = string.Empty,
            },
            ["VT323", "MesloLGS NF"],
            [
                new WslColorSchemeDto { Name = "One Half Dark" },
                new WslColorSchemeDto { Name = "Campbell" },
            ],
            [
                new WslIconPairDto
                {
                    Name = "orange",
                    Top = "#CF7040",
                    Bottom = "#B95E30",
                    TextColor = "#FFFFFF",
                },
                new WslIconPairDto
                {
                    Name = "blue",
                    Top = "#3B82F6",
                    Bottom = "#2563EB",
                    TextColor = "#FFFFFF",
                },
            ]);
    }
}

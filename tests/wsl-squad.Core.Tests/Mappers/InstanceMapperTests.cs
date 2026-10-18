using FluentAssertions;

using wsl_squad.Core.Dtos;
using wsl_squad.Core.Entities;
using wsl_squad.Core.Mappers;

namespace wsl_squad.Core.Tests.Mappers;

public class InstanceMapperTests
{
    private readonly InstanceMapper _mapper = new();

    [Fact]
    public void Maps_an_instance_to_a_list_row()
    {
        WslIndexDto dto = _mapper.ToIndexDto(new WslInstance
        {
            Name = "alpha",
            Size = 2048,
            IsArchive = true,
        });

        dto.Should().Be(new WslIndexDto { Name = "alpha", Size = 2048, IsArchive = true });
    }

    [Fact]
    public void Maps_a_pack()
    {
        WslPackDto dto = _mapper.ToPackDto(new WslPack
        {
            Name = "tools",
            Description = "Tools",
            Family = "debian",
        });

        dto.Name.Should().Be("tools");
        dto.Description.Should().Be("Tools");
        dto.Family.Should().Be("debian");
    }

    [Fact]
    public void Maps_an_appearance_and_its_icon()
    {
        WslTheme theme = WslTheme.Default("alpha");
        theme.SetIcon(new IconDrawing("AL", "#000000", "#111111", "#FFFFFF"));

        GetAppearanceResponseDto dto = _mapper.ToAppearanceResponse(theme, hasFragment: true);

        dto.Font.Should().Be("MesloLGS NF");
        dto.ColourScheme.Should().Be("One Half Dark");
        dto.HasFragment.Should().BeTrue();
        dto.Icon!.Text.Should().Be("AL");
    }

    [Fact]
    public void Maps_an_appearance_request_to_a_theme()
    {
        WslTheme theme = _mapper.FromSetAppearanceDto(new SetAppearanceDto
        {
            Font = "Consolas",
            ColourScheme = "One Half Dark",
            Icon = new WslIconDto { Text = "AL", Top = "#000000", Bottom = "#111111", TextColor = "#FFFFFF" },
            IconImage = "/tmp/icon.png",
        });

        theme.FontName.Should().Be("Consolas");
        theme.IconPath.Should().Be("/tmp/icon.png");
        theme.Icon!.Text.Should().Be("AL");
    }
}

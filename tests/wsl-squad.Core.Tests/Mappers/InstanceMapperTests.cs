using FluentAssertions;

using WslSquad.Core.Dtos;
using WslSquad.Core.Entities;
using WslSquad.Core.Mappers;

namespace WslSquad.Core.Tests.Mappers;

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

        dto.Should().Be(new WslIndexDto { Name = "alpha", Size = 2048, IsArchive = true, Family = string.Empty });
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
        WslTheme theme = WslTheme.Default();
        theme.SetIcon(new IconDrawing("AL", "#000000", "#111111", "#FFFFFF"));

        GetAppearanceResponseDto dto = _mapper.ToAppearanceResponse(theme);

        dto.Font.Should().Be("MesloLGS NF");
        dto.ColourScheme.Should().Be("One Half Dark");
        dto.Icon!.Text.Should().Be("AL");
    }

}

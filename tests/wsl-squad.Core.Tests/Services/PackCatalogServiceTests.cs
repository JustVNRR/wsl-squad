using WslSquad.Core.Exceptions;
using FluentAssertions;

using Moq;

using WslSquad.Core.Dtos;
using WslSquad.Core.Entities;
using WslSquad.Core.Mappers;
using WslSquad.Core.Repositories;
using WslSquad.Core.Services;

namespace WslSquad.Core.Tests.Services;

public class PackCatalogServiceTests
{
    private readonly Mock<IPackCatalogRepository> _repository = new();
    private readonly PackCatalogService _service;

    public PackCatalogServiceTests()
    {
        _service = new PackCatalogService(new WslPackCatalog(), _repository.Object, new InstanceMapper());
    }

    [Fact]
    public void Lists_packs_with_their_dependents()
    {
        _repository.Setup(repository => repository.GetAllPacks()).Returns(
        [
            new WslPack { Name = "base" },
            new WslPack { Name = "child", Requires = ["base"] },
        ]);

        IReadOnlyList<WslPackDto> rows = _service.GetAllPacks();

        rows.Should().HaveCount(2);
        rows.Single(row => row.Name == "base").Dependents.Should().Equal("child");
    }

    [Fact]
    public void Creates_a_pack_and_rewrites_its_description()
    {
        _repository.Setup(repository => repository.GetAllPacks()).Returns([new WslPack { Name = "source" }]);

        string name = _service.CreatePack(new CreatePackDto
        {
            SourceName = "source",
            NewName = "copy",
            Description = "Copy",
        });

        name.Should().Be("copy");
        _repository.Verify(repository => repository.CopyPack("source", "copy"), Times.Once);
        _repository.Verify(repository => repository.SavePackDescription("copy", "Copy"), Times.Once);
    }

    [Fact]
    public void Rejects_an_invalid_new_pack_name()
    {
        Action act = () => _service.CreatePack(new CreatePackDto
        {
            SourceName = "source",
            NewName = "bad name",
        });

        act.Should().Throw<ArgumentException>();
        _repository.Verify(repository => repository.CopyPack(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Rejects_an_unknown_pack()
    {
        _repository.Setup(repository => repository.GetAllPacks()).Returns([]);

        Action act = () => _service.DeletePack("ghost");

        act.Should().Throw<WslSquadException>().Which.Code.Should().Be(WslSquadErrorCode.PackNotFound);
    }
}

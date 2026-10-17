using System.IO.Abstractions.TestingHelpers;

using FluentAssertions;

using wsl_squad.Core.Application.Packs;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Packs;

namespace wsl_squad.Tests.Core.Packs;

public class PackCatalogServiceTests
{
    private const string _catalogRoot = "/catalog";

    private readonly MockFileSystem _fileSystem = new();

    private PackCatalogService CreateService()
    {
        return new PackCatalogService(_fileSystem, new PackCatalogRepository(_fileSystem));
    }

    private void AddPack(string name)
    {
        _fileSystem.AddFile(
            $"/catalog/packs/{name}/pack.conf",
            new MockFileData("PACK_DESCRIPTION := Pack\n"));
        _fileSystem.AddFile($"/catalog/packs/{name}/install.sh", new MockFileData("#!/bin/bash"));
    }

    [Fact]
    public void Lists_catalog_packs()
    {
        AddPack("shell");
        AddPack("py");

        OperationResult<IReadOnlyList<WslPack>> result = CreateService().List(_catalogRoot);

        result.Value.Select(pack => pack.Name).Should().Equal("py", "shell");
    }

    [Fact]
    public void Returns_a_pack_directory()
    {
        AddPack("shell");

        CreateService().GetDirectory(_catalogRoot, "shell").Value.Should().Be("/catalog/packs/shell");
    }

    [Fact]
    public void Duplicates_a_pack_with_its_files()
    {
        AddPack("shell");

        OperationResult<string> result = CreateService().Duplicate(_catalogRoot, "shell", "copy");

        result.IsSuccess.Should().BeTrue();
        _fileSystem.File.Exists("/catalog/packs/copy/pack.conf").Should().BeTrue();
        _fileSystem.File.Exists("/catalog/packs/copy/install.sh").Should().BeTrue();
    }

    [Fact]
    public void Refuses_to_duplicate_onto_an_existing_pack()
    {
        AddPack("shell");
        AddPack("copy");

        OperationResult<string> result = CreateService().Duplicate(_catalogRoot, "shell", "copy");

        result.Error!.Code.Should().Be(ErrorCode.AlreadyExists);
    }

    [Fact]
    public void Refuses_invalid_pack_names()
    {
        AddPack("shell");

        CreateService().Duplicate(_catalogRoot, "shell", "bad name").Error!.Code
            .Should().Be(ErrorCode.InvalidName);
    }

    [Fact]
    public void Deletes_a_pack_and_its_folder()
    {
        AddPack("shell");

        OperationResult result = CreateService().Delete(_catalogRoot, "shell");

        result.IsSuccess.Should().BeTrue();
        _fileSystem.Directory.Exists("/catalog/packs/shell").Should().BeFalse();
    }

    [Fact]
    public void Reports_a_missing_pack()
    {
        CreateService().GetDirectory(_catalogRoot, "missing").Error!.Code.Should().Be(ErrorCode.NotFound);
    }
}

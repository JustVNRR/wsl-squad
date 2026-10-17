using System.IO.Abstractions.TestingHelpers;

using FluentAssertions;

using wsl_squad.Core.Application.Packs;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Packs;

namespace wsl_squad.Tests.Core.Packs;

public class PackCatalogRepositoryTests
{
    private const string _catalogRoot = "/catalog";

    private readonly MockFileSystem _fileSystem = new();

    [Fact]
    public void Loads_packs_sorted_by_name()
    {
        _fileSystem.AddFile(
            "/catalog/packs/shell/pack.conf",
            new MockFileData("PACK_DESCRIPTION := Shell\nPACK_FAMILY := debian\n"));
        _fileSystem.AddFile(
            "/catalog/packs/py/pack.conf",
            new MockFileData("PACK_DESCRIPTION := Python\nPACK_REQUIRES := shell\n"));

        OperationResult<PackCatalogLoadResult> result =
            new PackCatalogRepository(_fileSystem).Load(_catalogRoot);

        result.IsSuccess.Should().BeTrue();
        result.Value.Packs.Select(pack => pack.Name).Should().Equal("py", "shell");
        result.Value.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void Warns_about_folders_without_pack_conf()
    {
        _fileSystem.AddFile(
            "/catalog/packs/shell/pack.conf",
            new MockFileData("PACK_DESCRIPTION := Shell\n"));
        _fileSystem.AddFile("/catalog/packs/raw/readme.md", new MockFileData("data"));

        OperationResult<PackCatalogLoadResult> result =
            new PackCatalogRepository(_fileSystem).Load(_catalogRoot);

        result.Value.Packs.Should().ContainSingle();
        result.Value.Warnings.Should().Contain(message => message.Contains("raw", StringComparison.Ordinal));
    }

    [Fact]
    public void Warns_about_invalid_pack_folder_names()
    {
        _fileSystem.AddFile(
            "/catalog/packs/bad name/pack.conf",
            new MockFileData("PACK_DESCRIPTION := Bad\n"));

        OperationResult<PackCatalogLoadResult> result =
            new PackCatalogRepository(_fileSystem).Load(_catalogRoot);

        result.Value.Packs.Should().BeEmpty();
        result.Value.Warnings.Should().Contain(message => message.Contains("bad name", StringComparison.Ordinal));
    }

    [Fact]
    public void Returns_an_empty_catalog_when_no_packs_directory_exists()
    {
        OperationResult<PackCatalogLoadResult> result =
            new PackCatalogRepository(_fileSystem).Load(_catalogRoot);

        result.Value.Packs.Should().BeEmpty();
        result.Value.Warnings.Should().BeEmpty();
    }
}

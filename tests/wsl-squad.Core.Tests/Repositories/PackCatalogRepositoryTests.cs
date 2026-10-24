using FluentAssertions;

using WslSquad.Core.Entities;
using WslSquad.Core.Repositories;
using WslSquad.Core.Tests.Support;

namespace WslSquad.Core.Tests.Repositories;

public class PackCatalogRepositoryTests
{
    [Fact]
    public void Parses_a_pack_conf()
    {
        using TemporaryDirectory temp = new();
        temp.Write(
            "packs/tools/pack.conf",
            "PACK_DESCRIPTION := Tools pack\nPACK_REQUIRES := base\nPACK_VISIBLE := no\nPACK_FAMILY := Debian\n");
        PackCatalogRepository repository = new(temp.Combine("packs"));

        WslPack pack = repository.GetAllPacks().Should().ContainSingle().Subject;
        pack.Name.Should().Be("tools");
        pack.Description.Should().Be("Tools pack");
        pack.Requires.Should().Equal("base");
        pack.Offered.Should().BeFalse();
        pack.Family.Should().Be("debian");
    }

    [Fact]
    public void Rewrites_the_description()
    {
        using TemporaryDirectory temp = new();
        string conf = temp.Write("packs/tools/pack.conf", "PACK_DESCRIPTION := Old\nPACK_FAMILY := debian\n");
        PackCatalogRepository repository = new(temp.Combine("packs"));

        repository.SavePackDescription("tools", "New");

        File.ReadAllText(conf).Should().Contain("PACK_DESCRIPTION := New");
    }

    [Fact]
    public void Copies_and_deletes_a_pack()
    {
        using TemporaryDirectory temp = new();
        _ = temp.Write("packs/tools/pack.conf", "PACK_DESCRIPTION := Tools\n");
        PackCatalogRepository repository = new(temp.Combine("packs"));

        repository.CopyPack("tools", "tools-copy");
        File.Exists(temp.Combine("packs", "tools-copy", "pack.conf")).Should().BeTrue();

        repository.DeletePack("tools-copy");
        Directory.Exists(temp.Combine("packs", "tools-copy")).Should().BeFalse();
    }

    [Fact]
    public void Seeds_the_catalogue_once()
    {
        using TemporaryDirectory temp = new();
        _ = temp.Write("source/tools/pack.conf", "PACK_DESCRIPTION := Tools\n");
        PackCatalogRepository repository = new(temp.Combine("packs"));

        repository.SeedFrom(temp.Combine("source"));

        File.Exists(temp.Combine("packs", "tools", "pack.conf")).Should().BeTrue();
    }
}

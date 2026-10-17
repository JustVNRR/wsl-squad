using System.IO.Abstractions.TestingHelpers;

using FluentAssertions;

using wsl_squad.Core.Application.Catalog;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Tests.Support;

namespace wsl_squad.Tests.Core.Catalog;

public class CatalogSeederTests
{
    private const string _catalogRoot = "/catalog";

    private readonly MockFileSystem _fileSystem = new();
    private readonly FakeEmbeddedResourceCatalog _resources = new();

    private CatalogSeeder CreateSeeder()
    {
        return new CatalogSeeder(_fileSystem, _resources);
    }

    [Fact]
    public void Seeds_dockerfiles_onboardings_packs_recipes_and_fonts()
    {
        _resources.Add("linux/build/Dockerfile", "FROM ubuntu");
        _resources.Add("linux/build/onboarding.sh", "#!/bin/bash");
        _resources.Add("linux/packs/oh_my_shell/pack.conf", "PACK_FAMILY := debian");
        _resources.Add("recipes/ubuntu-24-04/Dockerfile", "FROM ubuntu:24.04");
        _resources.Add("fonts/VT323/VT323-Regular.ttf", "font");

        OperationResult<CatalogSeedResult> result = CreateSeeder().Seed(_catalogRoot);

        result.IsSuccess.Should().BeTrue();
        result.Value.Created.Should().Contain("dockerfiles/default/Dockerfile");
        _fileSystem.File.ReadAllText("/catalog/dockerfiles/default/Dockerfile").Should().Be("FROM ubuntu");
        _fileSystem.File.Exists("/catalog/onboardings/default/onboarding.sh").Should().BeTrue();
        _fileSystem.File.Exists("/catalog/packs/oh_my_shell/pack.conf").Should().BeTrue();
        _fileSystem.File.Exists("/catalog/recipes/ubuntu-24-04/Dockerfile").Should().BeTrue();
        _fileSystem.File.Exists("/catalog/fonts/VT323/VT323-Regular.ttf").Should().BeTrue();
        _fileSystem.File.Exists("/catalog/.seed-manifest.json").Should().BeTrue();
    }

    [Fact]
    public void A_second_seed_is_a_no_op()
    {
        _resources.Add("linux/build/Dockerfile", "FROM ubuntu");
        CatalogSeeder seeder = CreateSeeder();
        seeder.Seed(_catalogRoot);

        OperationResult<CatalogSeedResult> second = seeder.Seed(_catalogRoot);

        second.Value.Created.Should().BeEmpty();
        second.Value.Updated.Should().BeEmpty();
        second.Value.Skipped.Should().BeEmpty();
    }

    [Fact]
    public void Preserves_user_edits()
    {
        _resources.Add("linux/build/Dockerfile", "FROM ubuntu");
        CatalogSeeder seeder = CreateSeeder();
        seeder.Seed(_catalogRoot);
        const string target = "/catalog/dockerfiles/default/Dockerfile";
        _fileSystem.File.WriteAllText(target, "FROM my-edit");

        OperationResult<CatalogSeedResult> second = seeder.Seed(_catalogRoot);

        second.Value.Skipped.Should().Contain("dockerfiles/default/Dockerfile");
        _fileSystem.File.ReadAllText(target).Should().Be("FROM my-edit");
    }

    [Fact]
    public void Updates_unedited_files_when_the_embedded_content_changes()
    {
        _resources.Add("linux/build/Dockerfile", "FROM ubuntu");
        CatalogSeeder seeder = CreateSeeder();
        seeder.Seed(_catalogRoot);
        _resources.Add("linux/build/Dockerfile", "FROM ubuntu:24.04");

        OperationResult<CatalogSeedResult> second = seeder.Seed(_catalogRoot);

        second.Value.Updated.Should().Contain("dockerfiles/default/Dockerfile");
        _fileSystem.File.ReadAllText("/catalog/dockerfiles/default/Dockerfile").Should().Be("FROM ubuntu:24.04");
    }

    [Fact]
    public void Ignores_resources_outside_the_catalog_mapping()
    {
        _resources.Add("unrelated/file.txt", "data");

        OperationResult<CatalogSeedResult> result = CreateSeeder().Seed(_catalogRoot);

        result.Value.Created.Should().BeEmpty();
        _fileSystem.File.Exists("/catalog/unrelated/file.txt").Should().BeFalse();
    }

    [Fact]
    public void Rejects_a_relative_catalog_root()
    {
        OperationResult<CatalogSeedResult> result = CreateSeeder().Seed("relative/catalog");

        result.Status.Should().Be(OperationStatus.Failed);
        result.Error!.Code.Should().Be(ErrorCode.Validation);
    }
}

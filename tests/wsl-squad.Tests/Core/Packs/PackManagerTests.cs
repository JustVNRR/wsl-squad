using System.IO.Abstractions.TestingHelpers;

using FluentAssertions;

using wsl_squad.Core.Application.Packs;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Core.Domain.Packs;
using wsl_squad.Tests.Support;

namespace wsl_squad.Tests.Core.Packs;

public class PackManagerTests
{
    private const string _catalogRoot = "/catalog";
    private const string _shellConf = "PACK_DESCRIPTION := Shell\nPACK_FAMILY := debian\n";

    private readonly LifecycleFixture _fixture = new();
    private readonly FakeWslPackGateway _gateway = new();

    private PackManager CreateManager()
    {
        return new PackManager(
            _fixture.FileSystem,
            _gateway,
            _fixture.Instances,
            _fixture.Metadata,
            new PackCatalogRepository(_fixture.FileSystem));
    }

    private void AddPack(
        string name,
        string conf,
        bool installScript = true,
        bool installRootScript = false,
        bool removeScript = false)
    {
        string directory = $"{_catalogRoot}/packs/{name}";
        _fixture.FileSystem.AddFile($"{directory}/pack.conf", new MockFileData(conf));

        if (installScript)
        {
            _fixture.FileSystem.AddFile($"{directory}/install.sh", new MockFileData("#!/bin/bash"));
        }

        if (installRootScript)
        {
            _fixture.FileSystem.AddFile($"{directory}/install_root.sh", new MockFileData("#!/bin/bash"));
        }

        if (removeScript)
        {
            _fixture.FileSystem.AddFile($"{directory}/remove.sh", new MockFileData("#!/bin/bash"));
        }
    }

    private void PrepareInstance()
    {
        _fixture.AddInstance(defaultUser: "jane");
        _gateway.SetFamily("ubuntu", "debian");
    }

    [Fact]
    public async Task Status_lists_candidates_and_removable_packs()
    {
        PrepareInstance();
        AddPack("shell", _shellConf);
        AddPack("py", "PACK_DESCRIPTION := Python\nPACK_REQUIRES := shell\n");
        AddPack("ops", "PACK_DESCRIPTION := Ops\nPACK_VISIBLE := no\n");
        _gateway.SetInstalled("shell");

        OperationResult<PackStatus> result = await CreateManager()
            .GetStatusAsync(LifecycleFixture.Root, _catalogRoot, "demo");

        result.IsSuccess.Should().BeTrue();
        result.Value.Installed.Should().Equal("shell");
        result.Value.Candidates.Select(pack => pack.Name).Should().Equal("py");
        result.Value.Removable.Should().Equal("shell");
        result.Value.Catalog.Should().HaveCount(3);
    }

    [Fact]
    public async Task Status_warns_about_incomplete_catalog_entries()
    {
        PrepareInstance();
        _fixture.FileSystem.AddFile("/catalog/packs/raw/readme.md", new MockFileData("data"));

        OperationResult<PackStatus> result = await CreateManager()
            .GetStatusAsync(LifecycleFixture.Root, _catalogRoot, "demo");

        result.Status.Should().Be(OperationStatus.Warning);
        result.Warnings.Should().Contain(message => message.Contains("raw", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Installs_the_dependency_closure_in_order()
    {
        PrepareInstance();
        AddPack("shell", _shellConf);
        AddPack("py", "PACK_DESCRIPTION := Python\nPACK_REQUIRES := shell\n");

        OperationResult<PackManageOutcome> result = await CreateManager()
            .ManageAsync(LifecycleFixture.Root, _catalogRoot, "demo", ["py"], []);

        result.IsSuccess.Should().BeTrue();
        result.Value.Added.Should().Equal("shell", "py");
        _gateway.Calls.Should().ContainInOrder(
            "copy:jane:shell",
            "sudo-open:jane",
            "run:jane:/home/jane/.config/packs/shell/install.sh",
            "sudo-close",
            "copy:jane:py",
            "sudo-open:jane",
            "run:jane:/home/jane/.config/packs/py/install.sh",
            "sudo-close");
    }

    [Fact]
    public async Task Runs_root_scripts_before_user_scripts()
    {
        PrepareInstance();
        AddPack("shell", _shellConf, installRootScript: true);

        OperationResult<PackManageOutcome> result = await CreateManager()
            .ManageAsync(LifecycleFixture.Root, _catalogRoot, "demo", ["shell"], []);

        result.IsSuccess.Should().BeTrue();
        _gateway.Calls.Should().ContainInOrder(
            "copy:jane:shell",
            "run:root:/home/jane/.config/packs/shell/install_root.sh",
            "sudo-open:jane",
            "run:jane:/home/jane/.config/packs/shell/install.sh",
            "sudo-close");
    }

    [Fact]
    public async Task Reports_partial_results_and_cleans_up_after_an_install_failure()
    {
        PrepareInstance();
        AddPack("shell", _shellConf);
        _gateway.FailOnUserScript = true;

        OperationResult<PackManageOutcome> result = await CreateManager()
            .ManageAsync(LifecycleFixture.Root, _catalogRoot, "demo", ["shell"], []);

        result.Status.Should().Be(OperationStatus.Partial);
        result.Value.Added.Should().BeEmpty();
        result.Value.Failed.Should().ContainSingle();
        result.Value.Failed[0].PackName.Should().Be("shell");
        result.Value.Failed[0].Step.Should().Be("install");
        _gateway.Calls.Should().Contain("sudo-close");
        _gateway.Calls.Should().Contain("rm-folder:shell");
    }

    [Fact]
    public async Task Removes_a_pack_and_runs_its_remove_script()
    {
        PrepareInstance();
        AddPack("shell", _shellConf, removeScript: true);
        _gateway.SetInstalled("shell");

        OperationResult<PackManageOutcome> result = await CreateManager()
            .ManageAsync(LifecycleFixture.Root, _catalogRoot, "demo", [], ["shell"]);

        result.IsSuccess.Should().BeTrue();
        result.Value.Removed.Should().Equal("shell");
        _gateway.Calls.Should().ContainInOrder(
            "run:root:/home/jane/.config/packs/shell/remove.sh",
            "rm-folder:shell");
    }

    [Fact]
    public async Task Refuses_to_remove_a_pack_needed_by_a_remaining_pack()
    {
        PrepareInstance();
        AddPack("shell", _shellConf);
        AddPack("py", "PACK_DESCRIPTION := Python\nPACK_REQUIRES := shell\n");
        _gateway.SetInstalled("shell", "py");

        OperationResult<PackManageOutcome> result = await CreateManager()
            .ManageAsync(LifecycleFixture.Root, _catalogRoot, "demo", [], ["shell"]);

        result.Error!.Code.Should().Be(ErrorCode.PackConflict);
        _gateway.Calls.Should().NotContain(call => call.StartsWith("rm-folder", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Rejects_packs_from_another_distribution_family()
    {
        PrepareInstance();
        AddPack("gcp", "PACK_DESCRIPTION := GCP\nPACK_FAMILY := fedora\n");

        OperationResult<PackManageOutcome> result = await CreateManager()
            .ManageAsync(LifecycleFixture.Root, _catalogRoot, "demo", ["gcp"], []);

        result.Error!.Code.Should().Be(ErrorCode.Validation);
        _gateway.Calls.Should().NotContain(call => call.StartsWith("copy", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Rejects_unknown_packs()
    {
        PrepareInstance();

        OperationResult<PackManageOutcome> result = await CreateManager()
            .ManageAsync(LifecycleFixture.Root, _catalogRoot, "demo", ["missing"], []);

        result.Error!.Code.Should().Be(ErrorCode.NotFound);
    }
}

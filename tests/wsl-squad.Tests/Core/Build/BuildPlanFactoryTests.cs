using System.IO.Abstractions.TestingHelpers;

using FluentAssertions;

using wsl_squad.Core.Application.Build;
using wsl_squad.Core.Domain.Build;
using wsl_squad.Core.Domain.Common;
using wsl_squad.Tests.Support;

namespace wsl_squad.Tests.Core.Build;

public class BuildPlanFactoryTests
{
    private const string _root = "/fleet";
    private const string _buildId = "20261017T120000Z-abcdef12";

    private readonly MockFileSystem _fileSystem = new();
    private readonly FakeWslRegistryGateway _registry = new();

    private BuildPlanFactory CreateFactory()
    {
        return new BuildPlanFactory(_fileSystem, _registry, new FakeBuildIdGenerator());
    }

    private static BuildRequest DockerfileRequest(string name = "demo")
    {
        return new BuildRequest
        {
            Name = name,
            SourceKind = BuildSourceKind.Dockerfile,
            SourcePath = "/source/Dockerfile",
        };
    }

    [Fact]
    public void Builds_a_plan_for_a_dockerfile()
    {
        _fileSystem.AddFile("/source/Dockerfile", new MockFileData("FROM scratch"));

        OperationResult<BuildPlan> result = CreateFactory().Create(_root, DockerfileRequest());

        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("demo");
        result.Value.User.Should().Be("root");
        result.Value.ImageTag.Should().Be($"wsl-squad/demo:{_buildId}");
        result.Value.InstallPath.Should().Be("/fleet/demo");
        result.Value.StagingDirectory.Should().Be($"/fleet/.staging/{_buildId}");
        result.Value.DockerfilePath.Should().Be("/source/Dockerfile");
        result.Value.ContextPath.Should().Be("/source");
    }

    [Fact]
    public void Accepts_a_custom_context_and_user()
    {
        _fileSystem.AddFile("/source/Dockerfile", new MockFileData("FROM scratch"));
        _fileSystem.AddFile("/context/keep.txt", new MockFileData("data"));

        OperationResult<BuildPlan> result = CreateFactory().Create(
            _root,
            new BuildRequest
            {
                Name = "demo",
                User = "jane",
                SourceKind = BuildSourceKind.Dockerfile,
                SourcePath = "/source/Dockerfile",
                Context = "/context",
            });

        result.Value.User.Should().Be("jane");
        result.Value.ContextPath.Should().Be("/context");
    }

    [Fact]
    public void Rejects_an_invalid_instance_name()
    {
        OperationResult<BuildPlan> result = CreateFactory().Create(_root, DockerfileRequest("bad name"));

        result.Error!.Code.Should().Be(ErrorCode.InvalidName);
    }

    [Fact]
    public void Rejects_an_invalid_user_name()
    {
        _fileSystem.AddFile("/source/Dockerfile", new MockFileData("FROM scratch"));

        OperationResult<BuildPlan> result = CreateFactory().Create(
            _root,
            new BuildRequest
            {
                Name = "demo",
                User = "Bad User",
                SourceKind = BuildSourceKind.Dockerfile,
                SourcePath = "/source/Dockerfile",
            });

        result.Error!.Code.Should().Be(ErrorCode.Validation);
    }

    [Fact]
    public void Rejects_a_name_already_registered_in_wsl()
    {
        _registry.Add("demo", "/fleet/demo");

        OperationResult<BuildPlan> result = CreateFactory().Create(_root, DockerfileRequest());

        result.Error!.Code.Should().Be(ErrorCode.AlreadyExists);
    }

    [Fact]
    public void Rejects_an_existing_non_empty_folder()
    {
        _fileSystem.AddFile("/fleet/demo/old.txt", new MockFileData("data"));

        OperationResult<BuildPlan> result = CreateFactory().Create(_root, DockerfileRequest());

        result.Error!.Code.Should().Be(ErrorCode.AlreadyExists);
    }

    [Fact]
    public void Rejects_a_missing_dockerfile()
    {
        OperationResult<BuildPlan> result = CreateFactory().Create(_root, DockerfileRequest());

        result.Error!.Code.Should().Be(ErrorCode.Validation);
    }

    [Fact]
    public void Rejects_a_recipe_without_a_dockerfile()
    {
        _fileSystem.AddFile("/recipes/empty/readme.md", new MockFileData("data"));

        OperationResult<BuildPlan> result = CreateFactory().Create(
            _root,
            new BuildRequest
            {
                Name = "demo",
                SourceKind = BuildSourceKind.Recipe,
                SourcePath = "/recipes/empty",
            });

        result.Error!.Code.Should().Be(ErrorCode.Validation);
    }

    [Fact]
    public void Accepts_a_recipe_with_a_dockerfile()
    {
        _fileSystem.AddFile("/recipes/ubuntu/Dockerfile", new MockFileData("FROM ubuntu"));

        OperationResult<BuildPlan> result = CreateFactory().Create(
            _root,
            new BuildRequest
            {
                Name = "demo",
                SourceKind = BuildSourceKind.Recipe,
                SourcePath = "/recipes/ubuntu",
            });

        result.IsSuccess.Should().BeTrue();
        result.Value.DockerfilePath.Should().Be("/recipes/ubuntu/Dockerfile");
        result.Value.ContextPath.Should().Be("/recipes/ubuntu");
    }

    [Fact]
    public void Rejects_a_missing_image_archive()
    {
        OperationResult<BuildPlan> result = CreateFactory().Create(
            _root,
            new BuildRequest
            {
                Name = "demo",
                SourceKind = BuildSourceKind.Image,
                SourcePath = "/images/missing.tar",
            });

        result.Error!.Code.Should().Be(ErrorCode.Validation);
    }

    [Fact]
    public void Rejects_a_registry_reference_with_whitespace()
    {
        OperationResult<BuildPlan> result = CreateFactory().Create(
            _root,
            new BuildRequest
            {
                Name = "demo",
                SourceKind = BuildSourceKind.Registry,
                SourcePath = "ubuntu 24.04",
            });

        result.Error!.Code.Should().Be(ErrorCode.Validation);
    }

    [Fact]
    public void Rejects_an_onboarding_script_outside_a_windows_drive()
    {
        _fileSystem.AddFile("/source/Dockerfile", new MockFileData("FROM scratch"));
        _fileSystem.AddFile("/scripts/onboarding.sh", new MockFileData("#!/bin/bash"));

        OperationResult<BuildPlan> result = CreateFactory().Create(
            _root,
            new BuildRequest
            {
                Name = "demo",
                SourceKind = BuildSourceKind.Dockerfile,
                SourcePath = "/source/Dockerfile",
                OnboardingPath = "/scripts/onboarding.sh",
            });

        result.Error!.Code.Should().Be(ErrorCode.Validation);
    }

    [Fact]
    public void Accepts_an_onboarding_script_on_a_windows_drive()
    {
        _fileSystem.AddFile("/source/Dockerfile", new MockFileData("FROM scratch"));
        _fileSystem.AddFile(@"C:\scripts\onboarding.sh", new MockFileData("#!/bin/bash"));

        OperationResult<BuildPlan> result = CreateFactory().Create(
            _root,
            new BuildRequest
            {
                Name = "demo",
                SourceKind = BuildSourceKind.Dockerfile,
                SourcePath = "/source/Dockerfile",
                OnboardingPath = @"C:\scripts\onboarding.sh",
            });

        result.IsSuccess.Should().BeTrue();
        result.Value.OnboardingPath.Should().Be(@"C:\scripts\onboarding.sh");
    }
}

using FluentAssertions;

using Moq;

using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Tests.Support;
using wsl_squad.Presentation.ViewModels;

namespace wsl_squad.Presentation.Tests.ViewModels;

public class BuildViewModelTests
{
    private readonly Mock<IInstanceService> _instance = new();
    private readonly TestDialogService _dialogs = new();

    public BuildViewModelTests()
    {
        _instance.Setup(service => service.GetBuildSources()).Returns(new GetBuildSourcesResponseDto
        {
            Dockerfiles =
            [
                new WslBuildSourceDto
                {
                    Name = "ubuntu (01/01 12:00)",
                    Path = "/catalog/ubuntu/Dockerfile",
                    Family = "debian",
                },
            ],
            Onboardings =
            [
                new WslBuildSourceDto
                {
                    Name = "setup (01/01 12:00)",
                    Path = "/catalog/setup/onboarding.sh",
                },
            ],
            Recipes = [],
        });
        _instance.Setup(service => service.GetAllSavedImages()).Returns([]);
        _instance.Setup(service => service.GetAll()).Returns([]);
        _instance.Setup(service => service.GetBuildPacks()).Returns(
        [
            new WslBuildPackDto { Name = "tools", Family = "shell", Description = "Tools" },
        ]);
        _instance.Setup(service => service.IsRegistryImageValid(It.IsAny<string>()))
            .Returns(new IsRegistryImageValidResponseDto { Valid = true });
    }

    [Fact]
    public void Loads_the_source_lists()
    {
        BuildViewModel viewModel = Create();

        viewModel.DockerfileNames.Should().ContainSingle();
        viewModel.OnboardingNames.Should().ContainSingle();
        viewModel.ImageNames.Should().BeEmpty();
        viewModel.RecipeNames.Should().BeEmpty();
        viewModel.CanTrashDockerfile.Should().BeFalse();
    }

    [Fact]
    public void Creates_a_dockerfile_build_request()
    {
        BuildViewModel viewModel = Create();
        viewModel.Name = "alpha";
        viewModel.User = "alice";
        _dialogs.BuildPacksAnswer = ["tools"];

        viewModel.CreateCommand.Execute(null);

        viewModel.Result.Should().NotBeNull();
        viewModel.Result!.Name.Should().Be("alpha");
        viewModel.Result.User.Should().Be("alice");
        viewModel.Result.BuildType.Should().Be(WslBuildType.Dockerfile);
        viewModel.Result.BuildPath.Should().Be("/catalog/ubuntu/Dockerfile");
        viewModel.Result.Context.Should().Be("/catalog/ubuntu");
        viewModel.Result.Onboarding.Should().Be("/catalog/setup/onboarding.sh");
        viewModel.Result.Packs.Should().Equal("tools");
        viewModel.Result.RegisterDocker.Should().BeTrue();
    }

    [Fact]
    public void Refuses_a_taken_or_invalid_name()
    {
        _instance.Setup(service => service.GetAll()).Returns([new WslIndexDto { Name = "taken" }]);
        BuildViewModel viewModel = Create();
        viewModel.Name = "taken";

        viewModel.NameError.Should().Contain("already exists");
        viewModel.CreateCommand.CanExecute(null).Should().BeFalse();

        viewModel.Name = "bad name";
        viewModel.NameError.Should().Contain("Letters");
        viewModel.CreateCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public void Image_road_needs_an_uploaded_image()
    {
        BuildViewModel viewModel = Create();
        viewModel.Name = "alpha";
        viewModel.RoadIndex = 1;

        viewModel.CreateCommand.Execute(null);

        viewModel.ImageError.Should().Contain("Upload a Docker image");
        viewModel.Result.Should().BeNull();
    }

    [Fact]
    public void Registry_road_reports_an_unknown_image()
    {
        _instance.Setup(service => service.IsRegistryImageValid("ghost:latest"))
            .Returns(new IsRegistryImageValidResponseDto { Valid = false, Why = "not found" });
        BuildViewModel viewModel = Create();
        viewModel.Name = "alpha";
        viewModel.RoadIndex = 2;
        viewModel.RegistryRef = "ghost:latest";

        viewModel.CreateCommand.Execute(null);

        viewModel.RegistryError.Should().Contain("Unknown registry image");
        viewModel.Result.Should().BeNull();
    }

    [Fact]
    public void Creates_a_recipe_from_the_dialogue()
    {
        _dialogs.RecipeNameAnswer = "mine";
        _instance.Setup(service => service.CreateRecipe(It.Is<NewRecipeDto>(dto => dto.Name == "mine")))
            .Returns(new WslBuildSourceDto { Name = "mine (01/01 12:00)", Path = "/recipes/mine" });
        BuildViewModel viewModel = Create();

        viewModel.NewRecipe();

        viewModel.RecipeNames.Should().Contain("mine (01/01 12:00)");
        viewModel.RecipeIndex.Should().Be(0);
    }

    [Fact]
    public void Reapplies_the_recipe_selection_after_a_creation()
    {
        _dialogs.RecipeNameAnswer = "mine";
        _instance.Setup(service => service.CreateRecipe(It.Is<NewRecipeDto>(dto => dto.Name == "mine")))
            .Returns(new WslBuildSourceDto { Name = "mine (01/01 12:00)", Path = "/recipes/mine" });
        BuildViewModel viewModel = Create();
        List<string> changes = RecordChanges(viewModel);

        viewModel.NewRecipe();

        LastIndex(changes).Should().BeGreaterThan(LastCollectionChange(changes));
    }

    [Fact]
    public void Reapplies_the_recipe_selection_after_a_deletion()
    {
        _instance.Setup(service => service.GetBuildSources()).Returns(new GetBuildSourcesResponseDto
        {
            Recipes =
            [
                new WslBuildSourceDto
                {
                    Name = "mine (01/01 12:00)",
                    Path = "/recipes/mine",
                    Uploaded = true,
                },
            ],
        });
        _dialogs.ConfirmAnswer = true;
        BuildViewModel viewModel = Create();
        List<string> changes = RecordChanges(viewModel);

        viewModel.DeleteSelected("Recipes");

        viewModel.RecipeNames.Should().BeEmpty();
        viewModel.RecipeIndex.Should().Be(-1);
        LastIndex(changes).Should().BeGreaterThan(LastCollectionChange(changes));
    }

    [Theory]
    [InlineData("Dockerfiles", "Delete Dockerfile", "ubuntu (01/01 12:00)")]
    [InlineData("Onboardings", "Delete onboarding shell", "setup (01/01 12:00)")]
    [InlineData("DockerImages", "Delete Docker image", "base.tar")]
    [InlineData("Recipes", "Delete recipe", "mine (01/01 12:00)")]
    public void Deleting_an_uploaded_source_asks_for_confirmation(string listName, string title, string rowName)
    {
        _instance.Setup(service => service.GetBuildSources()).Returns(new GetBuildSourcesResponseDto
        {
            Dockerfiles =
            [
                new WslBuildSourceDto
                {
                    Name = "ubuntu (01/01 12:00)",
                    Path = "/catalog/ubuntu/Dockerfile",
                    Uploaded = true,
                },
            ],
            Onboardings =
            [
                new WslBuildSourceDto
                {
                    Name = "setup (01/01 12:00)",
                    Path = "/catalog/setup/onboarding.sh",
                    Uploaded = true,
                },
            ],
            Recipes =
            [
                new WslBuildSourceDto
                {
                    Name = "mine (01/01 12:00)",
                    Path = "/recipes/mine",
                    Uploaded = true,
                },
            ],
        });
        _instance.Setup(service => service.GetAllSavedImages()).Returns(
        [
            new WslDockerImageDto
            {
                Name = "base.tar",
                Path = "/images/base.tar",
                Uploaded = true,
            },
        ]);
        BuildViewModel viewModel = Create();

        viewModel.DeleteSelected(listName);

        _dialogs.LastConfirm.Should().NotBeNull();
        _dialogs.LastConfirm!.Value.Title.Should().Be(title);
        _dialogs.LastConfirm.Value.Question.Should().Be($"Remove '{rowName}'?");
        _instance.Verify(service => service.DeleteRecipe(It.IsAny<string>()), Times.Never);
    }

    private static List<string> RecordChanges(BuildViewModel viewModel)
    {
        List<string> changes = [];
        viewModel.PropertyChanged += (_, e) => changes.Add($"P:{e.PropertyName}");
        viewModel.RecipeNames.CollectionChanged += (_, _) => changes.Add("C");

        return changes;
    }

    private static int LastIndex(List<string> changes)
    {
        return changes.FindLastIndex(change => change == $"P:{nameof(BuildViewModel.RecipeIndex)}");
    }

    private static int LastCollectionChange(List<string> changes)
    {
        return changes.FindLastIndex(change => change == "C");
    }

    private BuildViewModel Create()
    {
        return new BuildViewModel(_instance.Object, _dialogs);
    }
}

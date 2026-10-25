using FluentAssertions;

using Moq;

using WslSquad.Core.Dtos;
using WslSquad.Core.Services;
using WslSquad.Presentation.Tests.Support;
using WslSquad.Presentation.Gui.ViewModels;

namespace WslSquad.Presentation.Tests.ViewModels;

public class BuildViewModelTests
{
    private readonly Mock<IInstanceService> _instance = new();
    private readonly TestDialogService _dialogs = new();

    public BuildViewModelTests()
    {
        _instance.Setup(service => service.GetBuildSources()).Returns(new GetBuildSourcesResponseDto
        {
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
    }

    [Fact]
    public void Loads_the_source_lists()
    {
        BuildViewModel viewModel = Create();

        viewModel.OnboardingNames.Should().ContainSingle();
        viewModel.ImageNames.Should().BeEmpty();
        viewModel.RecipeNames.Should().BeEmpty();
        viewModel.CanTrashOnboarding.Should().BeFalse();
    }

    [Fact]
    public void Recipe_build_keeps_the_image_and_ignores_registration_and_onboarding_options()
    {
        _instance.Setup(service => service.GetBuildSources()).Returns(new GetBuildSourcesResponseDto
        {
            Onboardings = [],
            Recipes =
            [
                new WslBuildSourceDto
                {
                    Name = "default",
                    Path = "/catalog/default",
                },
            ],
        });
        BuildViewModel viewModel = Create();
        viewModel.Name = "alpha";
        viewModel.RoadIndex = 1;
        viewModel.SaveImage = true;
        viewModel.RegisterDocker = true;
        viewModel.RunOnboarding = true;

        viewModel.CreateCommand.Execute(null);

        viewModel.ShowGeneralOptions.Should().BeFalse();
        viewModel.Result.Should().NotBeNull();
        viewModel.Result!.BuildType.Should().Be(WslBuildType.Recipe);
        viewModel.Result.KeepImage.Should().BeTrue();
        viewModel.Result.RegisterDocker.Should().BeFalse();
        viewModel.Result.Onboarding.Should().BeEmpty();
        viewModel.Result.User.Should().BeEmpty();
    }

    [Fact]
    public void Recipe_mode_proposes_saving_the_image_by_default()
    {
        BuildViewModel viewModel = Create();

        viewModel.RoadIndex = 1;

        viewModel.ShowSaveImage.Should().BeTrue();
        viewModel.SaveImage.Should().BeTrue();

        viewModel.RoadIndex = 0;

        viewModel.ShowSaveImage.Should().BeFalse();
        viewModel.SaveImage.Should().BeFalse();
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
        viewModel.RoadIndex = 0;

        viewModel.CreateCommand.Execute(null);

        viewModel.ImageError.Should().Contain("Upload a Docker image");
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

        viewModel.DeleteSelected(WslCatalogSection.Recipes);

        viewModel.RecipeNames.Should().BeEmpty();
        viewModel.RecipeIndex.Should().Be(-1);
        LastIndex(changes).Should().BeGreaterThan(LastCollectionChange(changes));
    }

    [Theory]
    [InlineData(WslCatalogSection.Onboardings, "Delete onboarding shell", "setup (01/01 12:00)")]
    [InlineData(WslCatalogSection.DockerImages, "Delete Docker image", "base.tar")]
    [InlineData(WslCatalogSection.Recipes, "Delete recipe", "mine (01/01 12:00)")]
    public void Deleting_an_uploaded_source_asks_for_confirmation(
        WslCatalogSection kind,
        string title,
        string rowName)
    {
        _instance.Setup(service => service.GetBuildSources()).Returns(new GetBuildSourcesResponseDto
        {
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
            new WslBuildSourceDto
            {
                Name = "base.tar",
                Path = "/images/base.tar",
                Uploaded = true,
            },
        ]);
        BuildViewModel viewModel = Create();

        viewModel.DeleteSelected(kind);

        _dialogs.LastConfirm.Should().NotBeNull();
        _dialogs.LastConfirm!.Value.Title.Should().Be(title);
        _dialogs.LastConfirm.Value.Question.Should().Be($"Remove '{rowName}'?");
        _instance.Verify(service => service.DeleteRecipe(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Constructor_survives_a_source_read_failure()
    {
        Mock<IInstanceService> instance = new();
        instance.Setup(service => service.GetBuildSources()).Throws(new InvalidOperationException("gone"));
        instance.Setup(service => service.GetAll()).Returns([]);
        TestDialogService dialogs = new();

        BuildViewModel viewModel = new(instance.Object, dialogs);

        viewModel.OnboardingNames.Should().BeEmpty();
        viewModel.ImageNames.Should().BeEmpty();
        viewModel.RecipeNames.Should().BeEmpty();
    }

    [Fact]
    public void NewRecipe_reports_a_creation_failure()
    {
        _instance.Setup(service => service.CreateRecipe(It.IsAny<NewRecipeDto>()))
            .Throws(new InvalidOperationException("recipe failed"));
        _dialogs.RecipeNameAnswer = "mine";
        BuildViewModel viewModel = Create();

        viewModel.NewRecipe();

        viewModel.RecipeError.Should().Be("recipe failed");
    }

    [Fact]
    public void EditSource_survives_an_editor_failure()
    {
        _instance.Setup(service => service.OpenInEditor(It.IsAny<string>()))
            .Throws(new InvalidOperationException("editor failed"));
        BuildViewModel viewModel = Create();

        Action act = () => viewModel.EditSource(WslCatalogSection.Onboardings);

        act.Should().NotThrow();
    }

    [Fact]
    public void ExportRecipeTo_survives_an_export_failure()
    {
        _instance.Setup(service => service.ExportRecipe(It.IsAny<ExportRecipeDto>()))
            .Throws(new InvalidOperationException("export failed"));
        BuildViewModel viewModel = Create();

        Action act = () => viewModel.ExportRecipeTo(WslCatalogSection.Onboardings, "/tmp/out");

        act.Should().NotThrow();
    }

    [Fact]
    public void ValidateName_survives_a_fleet_read_failure()
    {
        BuildViewModel viewModel = Create();
        _instance.Setup(service => service.GetAll()).Throws(new InvalidOperationException("fleet failed"));

        viewModel.Name = "alpha";

        viewModel.NameError.Should().BeEmpty();
    }

    [Fact]
    public void AddImage_reports_an_upload_failure()
    {
        _instance.Setup(service => service.UploadRecipe(It.IsAny<UploadRecipeDto>()))
            .Throws(new InvalidOperationException("upload failed"));
        BuildViewModel viewModel = Create();

        viewModel.AddImage("/tmp/base.tar");

        viewModel.ImageError.Should().Be("upload failed");
    }

    [Fact]
    public void AddOnboarding_reports_an_upload_failure()
    {
        _instance.Setup(service => service.UploadRecipe(It.IsAny<UploadRecipeDto>()))
            .Throws(new InvalidOperationException("upload failed"));
        BuildViewModel viewModel = Create();

        viewModel.AddOnboarding("/tmp/onboarding.sh");

        viewModel.OnboardingError.Should().Be("upload failed");
    }

    [Fact]
    public void ExportRecipeTo_reports_an_onboarding_export_failure()
    {
        _instance.Setup(service => service.ExportRecipe(It.IsAny<ExportRecipeDto>()))
            .Throws(new InvalidOperationException("export failed"));
        BuildViewModel viewModel = Create();

        viewModel.ExportRecipeTo(WslCatalogSection.Onboardings, "/tmp/out");

        viewModel.OnboardingError.Should().Be("export failed");
    }

    [Fact]
    public void DeleteSelected_reports_an_onboarding_delete_failure()
    {
        _instance.Setup(service => service.GetBuildSources()).Returns(new GetBuildSourcesResponseDto
        {
            Onboardings =
            [
                new WslBuildSourceDto
                {
                    Name = "setup (01/01 12:00)",
                    Path = "/catalog/setup/onboarding.sh",
                    Uploaded = true,
                },
            ],
            Recipes = [],
        });
        _instance.Setup(service => service.DeleteRecipe(It.IsAny<string>()))
            .Throws(new InvalidOperationException("delete failed"));
        _dialogs.ConfirmAnswer = true;
        BuildViewModel viewModel = Create();

        viewModel.DeleteSelected(WslCatalogSection.Onboardings);

        viewModel.OnboardingError.Should().Be("delete failed");
    }

    [Fact]
    public void DeleteSelected_survives_a_delete_failure()
    {
        _instance.Setup(service => service.GetBuildSources()).Returns(new GetBuildSourcesResponseDto
        {
            Onboardings =
            [
                new WslBuildSourceDto
                {
                    Name = "setup (01/01 12:00)",
                    Path = "/catalog/setup/onboarding.sh",
                    Uploaded = true,
                },
            ],
            Recipes = [],
        });
        _instance.Setup(service => service.DeleteRecipe(It.IsAny<string>()))
            .Throws(new InvalidOperationException("delete failed"));
        _dialogs.ConfirmAnswer = true;
        BuildViewModel viewModel = Create();

        Action act = () => viewModel.DeleteSelected(WslCatalogSection.Onboardings);

        act.Should().NotThrow();
        viewModel.OnboardingNames.Should().ContainSingle();
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

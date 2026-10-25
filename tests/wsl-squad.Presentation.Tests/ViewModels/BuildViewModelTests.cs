using FluentAssertions;

using Moq;

using WslSquad.Core.Dtos;
using WslSquad.Core.Exceptions;
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
        _instance.Setup(service => service.GetRecipes()).Returns(
        [
            new WslBuildSourceDto
            {
                Name = "default",
                Path = "/catalog/default",
                BaseName = "default",
            },
        ]);
        _instance.Setup(service => service.GetAll()).Returns([]);
    }

    [Fact]
    public void Shows_the_recipe_and_defaults_to_saving_the_image()
    {
        BuildViewModel viewModel = Create();

        viewModel.RecipeNames.Should().ContainSingle().Which.Should().Be("default");
        viewModel.RecipeIndex.Should().Be(0);
        viewModel.SaveImage.Should().BeTrue();
        viewModel.HasSavedImage.Should().BeFalse();
        viewModel.UseLocalImage.Should().BeFalse();
        viewModel.ShowSaveImage.Should().BeTrue();
        viewModel.ShowDeleteImage.Should().BeFalse();
        viewModel.CanDeleteRecipe.Should().BeTrue();
    }

    [Fact]
    public void Creates_a_recipe_build_request()
    {
        BuildViewModel viewModel = Create();
        viewModel.Name = "alpha";

        viewModel.CreateCommand.Execute(null);

        viewModel.Result.Should().NotBeNull();
        viewModel.Result!.Name.Should().Be("alpha");
        viewModel.Result.RecipeName.Should().Be("default");
        viewModel.Result.BuildPath.Should().Be(Path.Combine("/catalog/default", "Dockerfile"));
        viewModel.Result.Context.Should().Be("/catalog/default");
        viewModel.Result.KeepImage.Should().BeTrue();
        viewModel.Result.UseSavedImage.Should().BeFalse();
    }

    [Fact]
    public void Can_skip_saving_the_image()
    {
        BuildViewModel viewModel = Create();
        viewModel.Name = "alpha";
        viewModel.SaveImage = false;

        viewModel.CreateCommand.Execute(null);

        viewModel.Result!.KeepImage.Should().BeFalse();
    }

    [Fact]
    public void Uses_the_saved_recipe_image_by_default()
    {
        _instance.Setup(service => service.GetSavedRecipeImage("default"))
            .Returns("/images/default.tar");
        BuildViewModel viewModel = Create();
        viewModel.Name = "alpha";

        viewModel.HasSavedImage.Should().BeTrue();
        viewModel.UseLocalImage.Should().BeTrue();
        viewModel.ShowSaveImage.Should().BeFalse();
        viewModel.ShowDeleteImage.Should().BeFalse();

        viewModel.CreateCommand.Execute(null);

        viewModel.Result!.RecipeName.Should().Be("default");
        viewModel.Result.UseSavedImage.Should().BeTrue();
        viewModel.Result.KeepImage.Should().BeFalse();
    }

    [Fact]
    public void Unchecking_the_local_image_offers_to_save_a_new_one()
    {
        _instance.Setup(service => service.GetSavedRecipeImage("default"))
            .Returns("/images/default.tar");
        BuildViewModel viewModel = Create();
        viewModel.Name = "alpha";
        viewModel.UseLocalImage = false;

        viewModel.ShowSaveImage.Should().BeTrue();
        viewModel.ShowDeleteImage.Should().BeTrue();
        viewModel.SaveImage.Should().BeTrue();

        viewModel.CreateCommand.Execute(null);

        viewModel.Result!.UseSavedImage.Should().BeFalse();
        viewModel.Result.KeepImage.Should().BeTrue();
    }

    [Fact]
    public void Deletes_the_saved_image_after_confirmation()
    {
        _instance.Setup(service => service.GetSavedRecipeImage("default"))
            .Returns("/images/default.tar");
        _dialogs.ConfirmAnswer = true;
        BuildViewModel viewModel = Create();
        viewModel.UseLocalImage = false;

        viewModel.DeleteImageCommand.Execute(null);

        _dialogs.LastConfirm!.Value.Title.Should().Be("Delete saved image");
        _instance.Verify(service => service.DeleteSavedRecipeImage("default"), Times.Once);
        viewModel.HasSavedImage.Should().BeFalse();
        viewModel.UseLocalImage.Should().BeFalse();
        viewModel.ShowDeleteImage.Should().BeFalse();
        viewModel.ShowSaveImage.Should().BeTrue();
        viewModel.SaveImage.Should().BeTrue();
    }

    [Fact]
    public void Keeps_the_saved_image_when_deletion_is_cancelled()
    {
        _instance.Setup(service => service.GetSavedRecipeImage("default"))
            .Returns("/images/default.tar");
        _dialogs.ConfirmAnswer = false;
        BuildViewModel viewModel = Create();
        viewModel.UseLocalImage = false;

        viewModel.DeleteImageCommand.Execute(null);

        _instance.Verify(service => service.DeleteSavedRecipeImage(It.IsAny<string>()), Times.Never);
        viewModel.HasSavedImage.Should().BeTrue();
        viewModel.UseLocalImage.Should().BeFalse();
        viewModel.ShowDeleteImage.Should().BeTrue();
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
    public void Create_is_disabled_without_a_recipe()
    {
        _instance.Setup(service => service.GetRecipes()).Returns([]);
        BuildViewModel viewModel = Create();
        viewModel.Name = "alpha";

        viewModel.CreateCommand.CanExecute(null).Should().BeFalse();
        viewModel.Result.Should().BeNull();
    }

    [Fact]
    public void Creates_a_recipe_from_the_dialogue()
    {
        _dialogs.RecipeNameAnswer = "mine";
        _instance.Setup(service => service.CreateRecipe(It.Is<NewRecipeDto>(dto => dto.Name == "mine")))
            .Returns(new WslBuildSourceDto
            {
                Name = "mine",
                Path = "/recipes/mine",
                BaseName = "mine",
            });
        BuildViewModel viewModel = Create();

        viewModel.NewRecipe();

        _dialogs.LastRecipeName!.Value.Taken.Should().Contain("default");
        viewModel.RecipeNames.Should().Contain("mine");
        viewModel.RecipeIndex.Should().Be(1);
        viewModel.CanDeleteRecipe.Should().BeTrue();
    }

    [Fact]
    public void Duplicates_the_selected_recipe_with_a_copy_name()
    {
        _dialogs.RecipeNameAnswer = "default-copy";
        _instance.Setup(service => service.DuplicateRecipe(It.Is<DuplicateRecipeDto>(dto =>
                dto.SourcePath == "/catalog/default" && dto.NewName == "default-copy")))
            .Returns(new WslBuildSourceDto
            {
                Name = "default-copy",
                Path = "/catalog/default-copy",
                BaseName = "default-copy",
            });
        BuildViewModel viewModel = Create();

        viewModel.DuplicateRecipe();

        _dialogs.LastRecipeName!.Value.Suggested.Should().Be("default-copy");
        _dialogs.LastRecipeName.Value.Source.Should().Be("default");
        viewModel.RecipeNames.Should().Contain("default-copy");
        viewModel.RecipeIndex.Should().Be(1);
    }

    [Fact]
    public void Reports_an_existing_recipe_name_from_the_service()
    {
        _dialogs.RecipeNameAnswer = "default-copy";
        _instance.Setup(service => service.DuplicateRecipe(It.IsAny<DuplicateRecipeDto>()))
            .Throws(new WslSquadException(WslSquadErrorCode.RecipeAlreadyExists, "default-copy"));
        BuildViewModel viewModel = Create();

        viewModel.DuplicateRecipe();

        viewModel.RecipeError.Should().Contain("already exists");
        viewModel.RecipeNames.Should().ContainSingle();
    }

    [Fact]
    public void Deletes_the_selected_recipe_after_confirmation()
    {
        _instance.Setup(service => service.GetRecipes()).Returns(
        [
            new WslBuildSourceDto
            {
                Name = "mine",
                Path = "/recipes/mine",
            },
        ]);
        _dialogs.ConfirmAnswer = true;
        BuildViewModel viewModel = Create();

        viewModel.DeleteRecipe();

        _dialogs.LastConfirm!.Value.Title.Should().Be("Delete recipe");
        _instance.Verify(service => service.DeleteRecipe("/recipes/mine"), Times.Once);
        viewModel.RecipeNames.Should().BeEmpty();
        viewModel.RecipeIndex.Should().Be(-1);
    }

    [Fact]
    public void A_cancelled_delete_keeps_the_recipe()
    {
        _instance.Setup(service => service.GetRecipes()).Returns(
        [
            new WslBuildSourceDto
            {
                Name = "mine",
                Path = "/recipes/mine",
            },
        ]);
        _dialogs.ConfirmAnswer = false;
        BuildViewModel viewModel = Create();

        viewModel.DeleteRecipe();

        _instance.Verify(service => service.DeleteRecipe(It.IsAny<string>()), Times.Never);
        viewModel.RecipeNames.Should().ContainSingle();
    }

    [Fact]
    public void Edit_opens_the_selected_recipe()
    {
        BuildViewModel viewModel = Create();

        viewModel.EditRecipe();

        _instance.Verify(service => service.OpenInEditor("/catalog/default"), Times.Once);
    }

    [Fact]
    public void Constructor_survives_a_source_read_failure()
    {
        Mock<IInstanceService> instance = new();
        instance.Setup(service => service.GetRecipes()).Throws(new InvalidOperationException("gone"));
        instance.Setup(service => service.GetAll()).Returns([]);
        TestDialogService dialogs = new();

        BuildViewModel viewModel = new(instance.Object, dialogs);

        viewModel.RecipeNames.Should().BeEmpty();
        viewModel.RecipeIndex.Should().Be(-1);
    }

    private BuildViewModel Create()
    {
        return new BuildViewModel(_instance.Object, _dialogs);
    }
}

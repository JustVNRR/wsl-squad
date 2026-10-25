using System.Diagnostics;

using WslSquad.Core.Dtos;
using WslSquad.Core.Services;
using WslSquad.Core.Validation;
using WslSquad.Presentation.Cli.Ports;
using WslSquad.Presentation.Workflows;
using WslSquad.Presentation.Localization;

namespace WslSquad.Presentation.Cli.Presenters.Instance;

public sealed class BuildPresenter : BasePresenter<IBuildView>
{
    private readonly IInstanceService _service;

    public BuildPresenter(IInstanceService service, IBuildView view)
        : base(view)
    {
        _service = service;
    }

    public IWorkflowResult Run()
    {
        IReadOnlyList<WslBuildSourceDto> recipes;
        IReadOnlyList<string> takenNames;

        try
        {
            BuildContextDto buildContext = _service.GetBuildContext();
            recipes = buildContext.Recipes;
            takenNames = buildContext.TakenNames;
        }
        catch (InvalidOperationException exception)
        {
            ShowFailure(exception.ToMessage());

            return WorkflowResults.Failed(string.Empty, Localizer.Instance.Format("CliBuildFailed", exception.Message));
        }

        if (recipes.Count == 0)
        {
            View.ShowAbort(
            [
                Localizer.Instance["CliAbortNoRecipe"],
                Localizer.Instance["CliAbortCreateRecipe"],
                Localizer.Instance["CliAbortNothingModified"],
            ]);

            return WorkflowResults.Failed(string.Empty, Localizer.Instance["CliBuildNoRecipe"]);
        }

        string name = string.Empty;
        string source = string.Empty;
        string recipeName = string.Empty;
        bool keep = false;
        bool useLocalImage = false;
        bool hasLocalImage = false;
        bool done = false;

        Stack<string> stack = new();
        stack.Push("name");

        while (stack.Count > 0)
        {
            string step = stack.Peek();

            switch (step)
            {
                case "name":
                    {
                        bool ok = false;

                        while (true)
                        {
                            string? answer = View.AskInstanceName(Localizer.Instance["CliAskInstanceName"]);

                            if (string.IsNullOrWhiteSpace(answer))
                            {
                                break;
                            }

                            answer = answer.Trim();

                            NameProblem problem = NameRules.ValidateInstanceName(answer, takenNames);

                            if (problem != NameProblem.None)
                            {
                                View.ShowNameProblem(problem.ToMessage(answer)!);

                                continue;
                            }

                            name = answer;
                            ok = true;

                            break;
                        }

                        if (ok)
                        {
                            stack.Push("recipe");
                        }
                        else
                        {
                            stack.Clear();
                        }

                        break;
                    }

                case "recipe":
                    {
                        BuildSourceChoice? picked = View.AskSource(
                            Localizer.Instance["LabelRecipe"],
                            [.. recipes.Select(ToChoice)]);

                        if (picked is null)
                        {
                            stack.Pop();

                            break;
                        }

                        source = picked.Path;
                        recipeName = picked.BaseName;
                        useLocalImage = false;
                        hasLocalImage = _service.GetSavedRecipeImage(recipeName) is not null;
                        stack.Push(hasLocalImage ? "local" : "keep");

                        break;
                    }

                case "local":
                    {
                        bool? answer = View.AskUseLocalImage(recipeName);

                        if (answer is null)
                        {
                            stack.Pop();
                        }
                        else if (answer.Value)
                        {
                            useLocalImage = true;
                            done = true;
                            stack.Clear();
                        }
                        else
                        {
                            useLocalImage = false;
                            stack.Push("keep");
                        }

                        break;
                    }

                case "keep":
                    {
                        bool? answer = hasLocalImage
                            ? View.AskOverwriteImage(recipeName)
                            : View.AskKeepImage();

                        if (answer is null)
                        {
                            stack.Pop();
                        }
                        else
                        {
                            keep = answer.Value;
                            done = true;
                            stack.Clear();
                        }

                        break;
                    }

                default:
                    stack.Clear();
                    break;
            }
        }

        if (!done)
        {
            View.ShowCancelled(Localizer.Instance["CliModified"]);

            return WorkflowResults.Cancelled(Localizer.Instance["OperationBuild"]);
        }

        BuildDto dto = BuildDto.FromRecipe(
            name,
            recipeName,
            source,
            keepImage: keep,
            useSavedImage: useLocalImage,
            interactive: true);

        try
        {
            BuildResponseDto response = _service.Build(dto);
            View.Show(response);

            try
            {
                _service.Open(dto.Name ?? string.Empty);
            }
            catch (InvalidOperationException ex)
            {
                Trace.TraceWarning($"[BuildPresenter] Could not open shell for '{dto.Name}': {ex.Message}");
            }

            return WorkflowResults.Success(dto.Name ?? string.Empty);
        }
        catch (InvalidOperationException exception)
        {
            ShowFailure(exception.ToMessage());

            return WorkflowResults.Failed(dto.Name ?? string.Empty, exception.ToMessage());
        }
    }

    private static BuildSourceChoice ToChoice(WslBuildSourceDto source)
    {
        return new BuildSourceChoice(
            source.Name ?? string.Empty,
            source.Path ?? string.Empty,
            source.BaseName ?? Path.GetFileName(source.Path ?? string.Empty));
    }
}

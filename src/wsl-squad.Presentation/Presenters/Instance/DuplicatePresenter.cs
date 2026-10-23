using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Core.Validation;
using wsl_squad.Presentation.Presenters.Views;

namespace wsl_squad.Presentation.Presenters.Instance;

public sealed class DuplicatePresenter : InstanceTargetPresenterBase
{
    public DuplicatePresenter(IInstanceService service, IInstanceView view)
        : base(service, view)
    {
    }

    protected override string OperationName => "Duplicate";

    protected override ActionResult ExecuteCore(string instanceName)
    {
        string copy;

        while (true)
        {
            string? answer = View.AskCopyName();

            if (string.IsNullOrEmpty(answer))
            {
                View.ShowCancelled("created");

                return ActionResult.Ok("Duplicate : cancelled");
            }

            if (NameRules.IsValid(answer))
            {
                copy = answer;

                break;
            }

            View.ShowNameHint();
        }

        View.ShowDiskWarning(instanceName);

        if (View.Confirm("Continue?", defaultNo: false) != true)
        {
            View.ShowCancelled("created");

            return ActionResult.Ok("Duplicate : cancelled");
        }

        View.ShowDuplicating(instanceName, copy);

        DuplicateResponseDto response = Service.Duplicate(new DuplicateDto
        {
            SourceName = instanceName,
            CopyName = copy,
        });
        View.Show(response);

        return ActionResult.Ok($"Duplicate : {copy}");
    }
}

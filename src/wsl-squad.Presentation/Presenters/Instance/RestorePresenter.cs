using wsl_squad.Core.Dtos;
using wsl_squad.Core.Services;
using wsl_squad.Presentation.Presenters.Views;

namespace wsl_squad.Presentation.Presenters.Instance;

public sealed class RestorePresenter : BasePresenter<IInstanceView>
{
    private readonly IInstanceService _service;

    public RestorePresenter(IInstanceService service, IInstanceView view)
        : base(view)
    {
        _service = service;
    }

    public ActionResult Run(string? target)
    {
        IReadOnlyList<WslIndexDto> rows;

        try
        {
            rows = _service.GetAll();
        }
        catch (InvalidOperationException exception)
        {
            ShowFailure(exception.Message);

            return ActionResult.Nok($"Restore : {exception.Message}");
        }

        string archiveName = target ?? string.Empty;

        if (archiveName.Length == 0)
        {
            List<WslIndexDto> archives = [.. rows.Where(row => row.IsArchive)];

            if (archives.Count == 0)
            {
                View.ShowNoArchives();

                return ActionResult.Nok("Restore : there are no archives");
            }

            string? picked = View.AskArchive(archives);

            if (string.IsNullOrEmpty(picked))
            {
                View.ShowCancelled("created");

                return ActionResult.Ok("Restore : cancelled");
            }

            archiveName = picked;
        }

        View.ShowRestoring(archiveName);
        string? name = View.AskNewName();

        if (string.IsNullOrEmpty(name))
        {
            View.ShowCancelled("created");

            return ActionResult.Ok("Restore : cancelled");
        }

        try
        {
            RestoreResponseDto response = _service.Restore(new RestoreDto
            {
                ArchiveName = archiveName,
                InstanceName = name,
            });
            View.Show(response);

            return ActionResult.Ok($"Restore : {name}");
        }
        catch (InvalidOperationException exception)
        {
            ShowFailure(exception.Message);

            return ActionResult.Nok($"Restore : {name} - {exception.Message}");
        }
    }
}

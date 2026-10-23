namespace wsl_squad.Presentation.Presenters.Views;

public interface IInstanceView :
    IInstanceArchiveView,
    IInstanceCompactView,
    IInstanceDeleteArchiveView,
    IInstanceDockerView,
    IInstanceDuplicateView,
    IInstanceManagePacksView,
    IInstanceOpenView,
    IInstanceRemoveView,
    IInstanceRestartView,
    IInstanceRestoreView
{
}

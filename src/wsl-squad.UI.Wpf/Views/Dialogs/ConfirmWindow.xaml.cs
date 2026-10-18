using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

using wsl_squad.Presentation.ViewModels;

namespace wsl_squad.UI.Wpf.Views.Dialogs;

public partial class ConfirmWindow : ChromeWindow
{
    public ConfirmWindow(ConfirmViewModel viewModel)
    {
        InitializeComponent();
        Dress();
        DataContext = viewModel;

        if (!string.IsNullOrEmpty(viewModel.Body))
        {
            TxtQuestion.Inlines.Add(new Run(viewModel.Body));
        }

        if (!string.IsNullOrWhiteSpace(viewModel.Danger))
        {
            if (!string.IsNullOrEmpty(viewModel.Body))
            {
                TxtQuestion.Inlines.Add(new LineBreak());
                TxtQuestion.Inlines.Add(new LineBreak());
            }

            TxtQuestion.Inlines.Add(new Run(viewModel.Danger)
            {
                Foreground = (Brush)FindResource("AppDangerBrush"),
            });
        }

        if (string.IsNullOrEmpty(viewModel.Body) && string.IsNullOrWhiteSpace(viewModel.Danger))
        {
            TxtQuestion.Visibility = Visibility.Collapsed;
        }

        viewModel.CloseRequested += (_, _) => DialogResult = true;
    }
}

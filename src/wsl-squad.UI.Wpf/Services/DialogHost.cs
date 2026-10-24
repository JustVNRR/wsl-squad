using System.Windows;
using System.Windows.Input;

namespace WslSquad.UI.Wpf.Services;

public static class DialogHost
{
    private static readonly Stack<Window> _stack = new();

    public static Window? Top =>
        _stack.Count > 0 ? _stack.Peek() : Application.Current?.MainWindow;

    public static bool? Show(Window dialog)
    {
        ArgumentNullException.ThrowIfNull(dialog);

        Window? owner = Top;
        Window? main = Application.Current?.MainWindow;
        bool hideMain = _stack.Count == 0 &&
                        main is not null &&
                        main.IsVisible &&
                        !ReferenceEquals(main, dialog);

        if (owner is not null && !ReferenceEquals(owner, dialog) && owner.IsVisible)
        {
            dialog.Owner = owner;
        }

        if (hideMain)
        {
            void Hide(object? sender, EventArgs e)
            {
                dialog.ContentRendered -= Hide;
                main!.Hide();
            }

            dialog.ContentRendered += Hide;
        }

        _stack.Push(dialog);

        try
        {
            return dialog.ShowDialog();
        }
        finally
        {
            _ = _stack.Pop();

            if (hideMain)
            {
                main!.Show();
                _ = main.Activate();
                _ = main.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
            }
        }
    }
}

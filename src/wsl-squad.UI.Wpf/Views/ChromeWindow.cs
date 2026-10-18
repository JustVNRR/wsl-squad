using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace wsl_squad.UI.Wpf.Views;

public class ChromeWindow : Window
{
    private const string _frameTag = "wsl-frame";
    private bool _framed;
    private bool _centered;

    public static FontFamily? UiFont { get; set; }

    public static int UiSize { get; set; } = 15;

    public bool AnimateOnShow { get; set; } = true;

    public ChromeWindow()
    {
        FontFamily = UiFont ?? new FontFamily("Segoe UI");
        FontSize = UiSize;
    }

    protected void Dress(int fontSize = 0)
    {
        FontFamily = UiFont ?? FontFamily;
        FontSize = fontSize > 0 ? fontSize : UiSize;

        if (_framed)
        {
            return;
        }

        _framed = true;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;

        UIElement? content = Content as UIElement;
        Content = null;

        Border frame = new()
        {
            BorderBrush = (Brush)FindResource("AppFrameBrush"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Background = (Brush)FindResource("AppBackgroundBrush"),
            Margin = new Thickness(14),
            Effect = NewGlow(),
            Child = content,
            Tag = _frameTag,
        };

        Content = frame;
        MouseLeftButtonDown += OnMouseLeftButtonDown;
        Opacity = 0;
        ContentRendered += OnContentRendered;
    }

    public void RefreshFrame()
    {
        if (Content is Border frame && string.Equals(frame.Tag as string, _frameTag, StringComparison.Ordinal))
        {
            frame.BorderBrush = (Brush)FindResource("AppFrameBrush");
            frame.Background = (Brush)FindResource("AppBackgroundBrush");
            frame.Effect = NewGlow();
        }
    }

    public bool? OpenAs(Window? owner)
    {
        Window? parent = owner;

        if (parent is null &&
            Application.Current?.MainWindow is { IsVisible: true } main &&
            !ReferenceEquals(main, this))
        {
            parent = main;
        }

        if (parent is not null)
        {
            Owner = parent;
        }

        return ShowDialog();
    }

    private void CenterOnOwnerOrScreen()
    {
        if (_centered || WindowState != WindowState.Normal)
        {
            return;
        }

        _centered = true;

        Rect area = Owner is { IsVisible: true } owner
            ? new Rect(owner.Left, owner.Top, owner.ActualWidth, owner.ActualHeight)
            : SystemParameters.WorkArea;

        Left = area.Left + Math.Max(0, (area.Width - ActualWidth) / 2);
        Top = area.Top + Math.Max(0, (area.Height - ActualHeight) / 2);
    }

    private DropShadowEffect NewGlow()
    {
        return new DropShadowEffect
        {
            Color = (Color)FindResource("AppGlowColor"),
            BlurRadius = 10,
            ShadowDepth = 0,
            Opacity = 0.30,
        };
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        DependencyObject? node = e.OriginalSource as DependencyObject;

        while (node is not null && !ReferenceEquals(node, this))
        {
            if (node is ButtonBase or TextBoxBase or ScrollBar or ComboBox)
            {
                return;
            }

            node = VisualTreeHelper.GetParent(node);
        }

        try
        {
            DragMove();
        }
        catch (InvalidOperationException)
        {
        }
    }

    private void OnContentRendered(object? sender, EventArgs e)
    {
        CenterOnOwnerOrScreen();

        if (!AnimateOnShow)
        {
            Opacity = 1;
            return;
        }

        DoubleAnimation fade = new(0, 1, TimeSpan.FromMilliseconds(180))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
        };

        BeginAnimation(OpacityProperty, fade);
    }
}

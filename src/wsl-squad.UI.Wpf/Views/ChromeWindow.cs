using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace wsl_squad.UI.Wpf.Views;

public partial class ChromeWindow : Window
{
    private const string _frameTag = "wsl-frame";
    private const uint _monitorDefaultToNearest = 0x00000002;
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
        PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
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

    private void CenterOnMainScreen()
    {
        if (_centered || WindowState != WindowState.Normal)
        {
            return;
        }

        _centered = true;

        Window? reference = Application.Current?.MainWindow;

        if (reference is null || ReferenceEquals(reference, this))
        {
            reference = Owner;
        }

        if (reference is not null && TryGetWorkArea(reference, out Rect area))
        {
            CenterIn(area);

            return;
        }

        CenterIn(SystemParameters.WorkArea);
    }

    private void CenterIn(Rect area)
    {
        double left = area.Left + ((area.Width - ActualWidth) / 2);
        double top = area.Top + ((area.Height - ActualHeight) / 2);

        Left = Math.Max(area.Left, Math.Min(left, area.Right - ActualWidth));
        Top = Math.Max(area.Top, Math.Min(top, area.Bottom - ActualHeight));
    }

    private static bool TryGetWorkArea(Window reference, out Rect area)
    {
        area = default;
        IntPtr handle = new WindowInteropHelper(reference).Handle;

        if (handle == IntPtr.Zero)
        {
            return false;
        }

        IntPtr monitor = MonitorFromWindow(handle, _monitorDefaultToNearest);

        if (monitor == IntPtr.Zero)
        {
            return false;
        }

        MonitorInfo info = new() { Size = Marshal.SizeOf<MonitorInfo>() };

        if (!GetMonitorInfo(monitor, ref info))
        {
            return false;
        }

        DpiScale dpi = VisualTreeHelper.GetDpi(reference);
        area = new Rect(
            info.Work.Left / dpi.DpiScaleX,
            info.Work.Top / dpi.DpiScaleY,
            (info.Work.Right - info.Work.Left) / dpi.DpiScaleX,
            (info.Work.Bottom - info.Work.Top) / dpi.DpiScaleY);

        return true;
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

    private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed ||
            IsInteractive(e.OriginalSource as DependencyObject) ||
            IsInteractive(e.Source as DependencyObject))
        {
            return;
        }

        try
        {
            DragMove();
        }
        catch (InvalidOperationException ex)
        {
            Trace.TraceWarning($"[ChromeWindow] Window drag move aborted: {ex.Message}");
        }
    }

    private bool IsInteractive(DependencyObject? node)
    {
        while (node is not null && !ReferenceEquals(node, this))
        {
            if (node is ButtonBase or TextBoxBase or PasswordBox or RangeBase or ItemsControl or Thumb or Popup or ComboBox)
            {
                return true;
            }

            node = node switch
            {
                Visual => VisualTreeHelper.GetParent(node) ?? LogicalTreeHelper.GetParent(node),
                ContentElement content => ContentOperations.GetParent(content) ?? LogicalTreeHelper.GetParent(node),
                _ => LogicalTreeHelper.GetParent(node),
            };
        }

        return false;
    }

    private void OnContentRendered(object? sender, EventArgs e)
    {
        CenterOnMainScreen();

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

    [LibraryImport("user32.dll")]
    private static partial IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [LibraryImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect Work;
        public uint Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}

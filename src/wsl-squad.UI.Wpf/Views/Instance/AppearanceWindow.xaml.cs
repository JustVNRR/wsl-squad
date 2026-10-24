using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using Microsoft.Win32;

using WslSquad.Core.Dtos;
using WslSquad.Presentation.Gui.ViewModels;
using WslSquad.UI.Wpf.Services;

namespace WslSquad.UI.Wpf.Views.Instance;

public partial class AppearanceWindow : ChromeWindow
{
    private readonly SetAppearanceViewModel _viewModel;

    public AppearanceWindow(SetAppearanceViewModel viewModel)
    {
        InitializeComponent();
        Dress();
        DataContext = viewModel;

        _viewModel = viewModel;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        viewModel.CloseRequested += (_, _) => Close();
        BtnImage.Click += (_, _) => PickImage();
        ContentRendered += (_, _) => _ = TxtIconText.Focus();

        UpdateIconError();
        PaintPreview();
        PaintIcon();
    }

    private void PickImage()
    {
        OpenFileDialog dialog = new()
        {
            Title = $"Icon image for '{_viewModel.InstanceName}'",
            Filter = "Images (*.png;*.jpg;*.jpeg;*.ico;*.bmp)|*.png;*.jpg;*.jpeg;*.ico;*.bmp",
        };

        if (dialog.ShowDialog(this) == true)
        {
            _viewModel.SetImage(dialog.FileName);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(SetAppearanceViewModel.IconError):
                UpdateIconError();
                break;
            case nameof(SetAppearanceViewModel.IconText):
            case nameof(SetAppearanceViewModel.PairIndex):
            case nameof(SetAppearanceViewModel.ImagePath):
                PaintIcon();
                break;
            case nameof(SetAppearanceViewModel.FontIndex):
            case nameof(SetAppearanceViewModel.SchemeIndex):
                PaintPreview();
                break;
            default:
                break;
        }
    }

    private void UpdateIconError()
    {
        GuiIconError.Visibility = _viewModel.IconError.Length == 0
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void PaintIcon()
    {
        if (TryLoadIcon(_viewModel.ImagePath, out BitmapFrame? image))
        {
            ImgIconPreview.Source = image;
            ImgIconPreview.Visibility = Visibility.Visible;
            BrdTile.Visibility = Visibility.Collapsed;
            TxtTile.Visibility = Visibility.Collapsed;

            return;
        }

        ImgIconPreview.Source = null;
        ImgIconPreview.Visibility = Visibility.Collapsed;
        BrdTile.Visibility = Visibility.Visible;
        TxtTile.Visibility = Visibility.Visible;

        WslIconPairDto? pair = _viewModel.SelectedPair?.Pair;

        if (pair is null)
        {
            return;
        }

        BrdTile.Background = new LinearGradientBrush(
            ParseColor(pair.Top, Color.FromRgb(0x33, 0x33, 0x33)),
            ParseColor(pair.Bottom, Color.FromRgb(0x11, 0x11, 0x11)),
            90);
        string text = _viewModel.IconText.Trim().ToUpperInvariant();
        TxtTile.Text = text;
        TxtTile.Foreground = new SolidColorBrush(ParseColor(pair.TextColor, Colors.White));
        TxtTile.FontFamily = new FontFamily("Cascadia Mono, Consolas, Segoe UI");
        TxtTile.FontWeight = FontWeights.Bold;
        TxtTile.FontSize = text.Length switch
        {
            0 => 34,
            1 => 34,
            2 => 28,
            _ => 22,
        };
    }

    private static bool TryLoadIcon(string? path, out BitmapFrame? image)
    {
        image = null;

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return false;
        }

        try
        {
            using FileStream stream = File.OpenRead(path);
            BitmapFrame frame = BitmapFrame.Create(
                stream,
                BitmapCreateOptions.None,
                BitmapCacheOption.OnLoad);
            frame.Freeze();
            image = frame;

            return true;
        }
        catch (Exception exception) when (
            exception is IOException or NotSupportedException or ArgumentException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private void PaintPreview()
    {
        WslColorSchemeDto? scheme = _viewModel.SelectedScheme;
        string background = scheme?.Background ?? "#0C0C0C";
        string foreground = scheme?.Foreground ?? "#FFFFFF";
        IReadOnlyList<string> palette = PaletteOf(scheme);
        FontFamily face = FontFaceResolver.Resolve(_viewModel.SelectedFontName);

        BrdPreview.Background = new SolidColorBrush(ParseColor(background, Color.FromRgb(0x0C, 0x0C, 0x0C)));

        foreach (TextBlock block in new[] { TxtPreview1, TxtPreview2, TxtPreview3, TxtPreview4, TxtPreview5, TxtPreview6 })
        {
            block.FontFamily = face;
            block.Foreground = new SolidColorBrush(ParseColor(foreground, Colors.White));

            foreach (Run run in block.Inlines.OfType<Run>())
            {
                run.Foreground = new SolidColorBrush(ParseColor(ColorForTag(run.Tag as string, foreground, palette), Colors.White));
            }
        }

        PnlPalette.Children.Clear();

        for (int index = 0; index < 16; index++)
        {
            Border swatch = new()
            {
                Width = 18,
                Height = 12,
                Margin = new Thickness(0, 0, 3, 0),
                Background = new SolidColorBrush(
                    ParseColor(index < palette.Count ? palette[index] : foreground, Colors.White)),
            };

            _ = PnlPalette.Children.Add(swatch);
        }
    }

    private static IReadOnlyList<string> PaletteOf(WslColorSchemeDto? scheme)
    {
        if (scheme is null)
        {
            return [];
        }

        return
        [
            scheme.Black ?? string.Empty,
            scheme.DarkRed ?? string.Empty,
            scheme.DarkGreen ?? string.Empty,
            scheme.DarkYellow ?? string.Empty,
            scheme.DarkBlue ?? string.Empty,
            scheme.DarkMagenta ?? string.Empty,
            scheme.DarkCyan ?? string.Empty,
            scheme.Gray ?? string.Empty,
            scheme.DarkGray ?? string.Empty,
            scheme.Red ?? string.Empty,
            scheme.Green ?? string.Empty,
            scheme.Yellow ?? string.Empty,
            scheme.Blue ?? string.Empty,
            scheme.Magenta ?? string.Empty,
            scheme.Cyan ?? string.Empty,
            scheme.White ?? string.Empty,
        ];
    }

    private static string ColorForTag(
        string? tag,
        string foreground,
        IReadOnlyList<string> palette)
    {
        if (string.IsNullOrEmpty(tag) || tag == "fg")
        {
            return foreground;
        }

        if (tag.Length >= 2 &&
            tag[0] == 'c' &&
            int.TryParse(tag[1..], out int index) &&
            index < palette.Count)
        {
            return palette[index];
        }

        return foreground;
    }

    private static bool TryHex(char digit, out byte value)
    {
        return byte.TryParse(
            new string(digit, 2),
            NumberStyles.HexNumber,
            CultureInfo.InvariantCulture,
            out value);
    }

    private static Color ParseColor(string? value, Color fallback)
    {
        string text = value?.Trim() ?? string.Empty;

        if (text.Length == 0)
        {
            return fallback;
        }

        if (text.Length == 4 && text[0] == '#' &&
            TryHex(text[1], out byte shortRed) &&
            TryHex(text[2], out byte shortGreen) &&
            TryHex(text[3], out byte shortBlue))
        {
            return Color.FromRgb(shortRed, shortGreen, shortBlue);
        }

        if (text.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
        {
            int open = text.IndexOf('(');
            int close = text.LastIndexOf(')');

            if (open >= 0 && close > open)
            {
                string[] parts = text[(open + 1)..close].Split(',');

                if (parts.Length >= 3 &&
                    int.TryParse(parts[0].Trim(), out int red) &&
                    int.TryParse(parts[1].Trim(), out int green) &&
                    int.TryParse(parts[2].Trim(), out int blue) &&
                    red is >= 0 and <= 255 &&
                    green is >= 0 and <= 255 &&
                    blue is >= 0 and <= 255)
                {
                    return Color.FromRgb((byte)red, (byte)green, (byte)blue);
                }
            }

            return fallback;
        }

        try
        {
            object? parsed = ColorConverter.ConvertFromString(text);

            return parsed is Color color ? color : fallback;
        }
        catch (FormatException)
        {
            return fallback;
        }
        catch (NotSupportedException)
        {
            return fallback;
        }
    }
}

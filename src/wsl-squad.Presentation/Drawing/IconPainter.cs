using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.Versioning;

using wsl_squad.Core.Entities;

namespace wsl_squad.Presentation.Drawing;

[SupportedOSPlatform("windows")]
public static class IconPainter
{
    private static readonly (string Name, string Top, string Bottom, string Text)[] _palette =
    [
        ("orange", "#CF7040", "#B95E30", "#FFFFFF"),
        ("blue", "#3B82F6", "#2563EB", "#FFFFFF"),
        ("green", "#2E8B57", "#1F5C3E", "#FFFFFF"),
        ("purple", "#7C5CBF", "#5B3F9E", "#FFFFFF"),
        ("red", "#C0453B", "#93291F", "#FFFFFF"),
        ("teal", "#148F8A", "#0E6B67", "#FFFFFF"),
        ("graphite", "#4B5563", "#374151", "#FFFFFF"),
        ("sand", "#E3C567", "#C9A73F", "#2A2410"),
    ];

    public static IconDrawing Draw(string instanceName, string outputPath, IconDrawing? drawing)
    {
        (string _, string defaultTop, string defaultBottom, string defaultText) = _palette[PaletteIndex(instanceName)];
        string text = drawing?.Text is { Length: > 0 } given ? given : Letters(instanceName);
        string top = string.IsNullOrEmpty(drawing?.Top) ? defaultTop : drawing.Top;
        string bottom = string.IsNullOrEmpty(drawing?.Bottom) ? defaultBottom : drawing.Bottom;
        string textColor = string.IsNullOrEmpty(drawing?.TextColor) ? defaultText : drawing.TextColor;

        const int size = 256;
        using Bitmap bitmap = new(size, size, PixelFormat.Format32bppArgb);
        using Graphics canvas = Graphics.FromImage(bitmap);
        canvas.SmoothingMode = SmoothingMode.AntiAlias;
        canvas.Clear(Color.Transparent);

        float margin = (float)Math.Round(size * 0.023);
        RectangleF rect = new(margin, margin, size - (2 * margin), size - (2 * margin));
        float radius = rect.Width * 0.22f;
        float diameter = radius * 2;

        using GraphicsPath tile = new();
        tile.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        tile.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        tile.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        tile.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        tile.CloseFigure();

        using LinearGradientBrush gradient = new(
            rect,
            ColorTranslator.FromHtml(top),
            ColorTranslator.FromHtml(bottom),
            90);
        canvas.FillPath(gradient, tile);

        using FontFamily family = LoadMonogramFamily()
            ?? throw new InvalidOperationException("No usable font family found on this machine.");

        using GraphicsPath probe = Monogram(text, family, 100);
        RectangleF probeBounds = probe.GetBounds();
        float scale = Math.Min(
            rect.Width * 0.74f / probeBounds.Width,
            rect.Height * 0.74f / probeBounds.Height);

        using GraphicsPath monogram = Monogram(text, family, 100 * scale);
        RectangleF bounds = monogram.GetBounds();
        using Matrix shift = new();
        shift.Translate(
            rect.X + ((rect.Width - bounds.Width) / 2) - bounds.X,
            rect.Y + ((rect.Height - bounds.Height) / 2) - bounds.Y);
        monogram.Transform(shift);

        using SolidBrush brush = new(ColorTranslator.FromHtml(textColor));
        canvas.FillPath(brush, monogram);

        string? directory = Path.GetDirectoryName(Path.GetFullPath(outputPath));

        if (!string.IsNullOrEmpty(directory))
        {
            _ = Directory.CreateDirectory(directory);
        }

        bitmap.Save(Path.GetFullPath(outputPath), ImageFormat.Png);

        return new IconDrawing(text, top, bottom, textColor);
    }

    private static FontFamily? LoadMonogramFamily()
    {
        using InstalledFontCollection collection = new();
        string[] names = ["Cascadia Mono", "Cascadia Code", "Consolas", "Segoe UI"];
        string? name = Array.Find(
            names,
            candidate => Array.Exists(collection.Families, family => family.Name == candidate));

        if (name is not null)
        {
            return new FontFamily(name);
        }

        return collection.Families.Length > 0 ? new FontFamily(collection.Families[0].Name) : null;
    }

    private static GraphicsPath Monogram(string text, FontFamily family, float pixelSize)
    {
        GraphicsPath path = new() { FillMode = FillMode.Winding };
        path.AddString(
            text,
            family,
            (int)FontStyle.Bold,
            pixelSize,
            new PointF(0, 0),
            StringFormat.GenericTypographic);

        return path;
    }

    private static string Letters(string instance)
    {
        string[] pieces =
        [
            .. instance
                .Split(['-', '_', '.'], StringSplitOptions.RemoveEmptyEntries)
                .Where(piece => char.IsAsciiLetter(piece[0])),
        ];

        if (pieces.Length >= 2)
        {
            return string.Concat(pieces[0][0], pieces[1][0]).ToUpperInvariant();
        }

        string word = instance.Split(['-', '_', '.'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "?";

        return word.Length >= 2
            ? word[..2].ToUpperInvariant()
            : word.ToUpperInvariant();
    }

    private static int PaletteIndex(string instance)
    {
        long hash = 0;

        foreach (char character in instance.ToLowerInvariant())
        {
            hash = ((hash * 31) + character) % 1000003;
        }

        return (int)(hash % _palette.Length);
    }
}

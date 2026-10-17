using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

using wsl_squad.Core.Abstractions.Windows;
using wsl_squad.Core.Domain.Common;

namespace wsl_squad.Core.Infrastructure.Windows;

/// <summary>Draws 256px rounded gradient icons with GDI+.</summary>
public sealed class GdiIconRenderer : IIconRenderer
{
    private const int _size = 256;

    public OperationResult<string> Render(
        string outputPath,
        string instanceName,
        string? text,
        string? top,
        string? bottom,
        string? textColor)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(6, 1))
        {
            return OperationResult<string>.Fail(
                ErrorCode.TerminalUnavailable,
                "Icon rendering requires Windows.");
        }

        try
        {
            string monogram = string.IsNullOrWhiteSpace(text) ? Initials(instanceName) : text;
            Color topColor = ParseColor(top, Color.FromArgb(0x33, 0xFF, 0x66));
            Color bottomColor = ParseColor(bottom, Color.FromArgb(0x14, 0x5C, 0x28));
            Color ink = ParseColor(textColor, Color.Black);

            using Bitmap bitmap = new(_size, _size);
            using Graphics graphics = Graphics.FromImage(bitmap);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            using LinearGradientBrush gradient = new(
                new Rectangle(0, 0, _size, _size),
                topColor,
                bottomColor,
                LinearGradientMode.Vertical);
            using GraphicsPath path = RoundedRectangle(new Rectangle(8, 8, _size - 16, _size - 16), 44);
            graphics.FillPath(gradient, path);

            using Font font = new("Segoe UI", 112, FontStyle.Bold, GraphicsUnit.Pixel);
            using StringFormat format = new()
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
            };
            using SolidBrush brush = new(ink);
            graphics.DrawString(
                monogram,
                font,
                brush,
                new RectangleF(0, 0, _size, _size),
                format);

            string? directory = Path.GetDirectoryName(outputPath);

            if (!string.IsNullOrEmpty(directory))
            {
                _ = Directory.CreateDirectory(directory);
            }

            bitmap.Save(outputPath, ImageFormat.Png);

            return OperationResult<string>.Ok(outputPath);
        }
        catch (Exception exception) when (exception is ExternalException
                                              or IOException
                                              or UnauthorizedAccessException
                                              or ArgumentException)
        {
            return OperationResult<string>.Fail(
                ErrorCode.IoError,
                "The instance icon could not be rendered.",
                exception.Message);
        }
    }

    private static string Initials(string instanceName)
    {
        string[] parts = instanceName.Split(['-', '_', '.'], StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
        {
            return instanceName.Length > 0 ? instanceName[..1].ToUpperInvariant() : "?";
        }

        string initials = string.Concat(
            parts.Take(3).Select(part => char.ToUpperInvariant(part[0])));

        return initials.Length == 0 ? "?" : initials;
    }

    private static Color ParseColor(string? value, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return fallback;
        }

        try
        {
            return ColorTranslator.FromHtml(value);
        }
        catch (ArgumentException)
        {
            return fallback;
        }
    }

    [SupportedOSPlatform("windows6.1")]
    private static GraphicsPath RoundedRectangle(Rectangle bounds, int radius)
    {
        int diameter = radius * 2;
        GraphicsPath path = new();
        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }
}

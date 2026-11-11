using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace ePad;

/// <summary>
/// Renders toolbar icons from the Windows icon font (Segoe Fluent Icons on Windows 11,
/// Segoe MDL2 Assets on Windows 10), so no image files need to ship with the app.
/// </summary>
public static class Icons
{
    public const char New = '';    // Add
    public const char Save = '';   // Save
    public const char Delete = ''; // Delete

    private static readonly string? FontName = FindIconFont();

    private static string? FindIconFont()
    {
        using var fonts = new InstalledFontCollection();
        var names = fonts.Families.Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var candidate in new[] { "Segoe Fluent Icons", "Segoe MDL2 Assets" })
            if (names.Contains(candidate)) return candidate;
        return null;
    }

    /// <summary>Returns the glyph as a bitmap, or null if no icon font is installed.</summary>
    public static Bitmap? Render(char glyph, int size, Color color)
    {
        if (FontName is null) return null;
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        using var font = new Font(FontName, size * 0.75f, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(color);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString(glyph.ToString(), font, brush, new RectangleF(0, 0, size, size), format);
        return bmp;
    }
}

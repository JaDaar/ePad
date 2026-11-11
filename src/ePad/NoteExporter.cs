using System.Globalization;
using System.Text;

namespace ePad;

public enum ExportFormat { Txt, Pdf, Md }

/// <summary>Writes a note to disk as plain text, Markdown or PDF.</summary>
public static class NoteExporter
{
    public static ExportFormat FormatFromPath(string path) => Path.GetExtension(path).ToLowerInvariant() switch
    {
        ".pdf" => ExportFormat.Pdf,
        ".md" or ".markdown" => ExportFormat.Md,
        _ => ExportFormat.Txt,
    };

    public static void Export(string path, string body, ExportFormat format, int tabWidth)
    {
        switch (format)
        {
            case ExportFormat.Pdf:
                File.WriteAllBytes(path, BuildPdf(body, tabWidth));
                break;
            case ExportFormat.Md:
                File.WriteAllText(path, ToMarkdown(body), Encoding.ASCII);
                break;
            default:
                File.WriteAllText(path, body, Encoding.ASCII);
                break;
        }
    }

    /// <summary>
    /// Notes are plain text, so the body is kept as-is; the first non-blank line becomes
    /// a level-1 heading unless it already is one.
    /// </summary>
    public static string ToMarkdown(string body)
    {
        var lines = body.Replace("\r\n", "\n").Split('\n');
        var first = Array.FindIndex(lines, l => l.Trim().Length > 0);
        if (first >= 0 && !lines[first].TrimStart().StartsWith('#'))
            lines[first] = "# " + lines[first].Trim();
        return string.Join("\r\n", lines);
    }

    // ---------- PDF ----------

    private const float Margin = 54f;      // 0.75 inch
    private const float FontSize = 10f;
    private const float Leading = 12.5f;   // line spacing
    private const float CharWidth = 0.6f;  // Courier glyphs are 600/1000 em wide

    /// <summary>
    /// Builds a minimal PDF 1.4 document using the built-in Courier font.
    /// Notes are ASCII-only, so no font embedding or text encoding is needed.
    /// </summary>
    public static byte[] BuildPdf(string body, int tabWidth)
    {
        // A4 in metric regions, US Letter elsewhere.
        var metric = RegionInfo.CurrentRegion.IsMetric;
        var pageWidth = metric ? 595.28f : 612f;
        var pageHeight = metric ? 841.89f : 792f;

        var maxChars = (int)((pageWidth - 2 * Margin) / (FontSize * CharWidth));
        var linesPerPage = (int)((pageHeight - 2 * Margin) / Leading);

        var lines = body.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')
            .SelectMany(l => Wrap(ExpandTabs(l, tabWidth), maxChars))
            .ToList();
        var pages = lines.Chunk(linesPerPage).ToList();
        if (pages.Count == 0) pages.Add([]);

        var pdf = new StringBuilder();
        var offsets = new List<int>();

        void Obj(string content)
        {
            offsets.Add(pdf.Length); // ASCII only, so characters == bytes
            pdf.Append(offsets.Count).Append(" 0 obj\n").Append(content).Append("\nendobj\n");
        }

        // Object numbers: 1 catalog, 2 page tree, 3 font, 4 info, then a (page, content) pair per page.
        const int firstPageObj = 5;
        var kids = string.Join(" ", Enumerable.Range(0, pages.Count).Select(i => $"{firstPageObj + i * 2} 0 R"));

        pdf.Append("%PDF-1.4\n");
        Obj("<< /Type /Catalog /Pages 2 0 R >>");
        Obj($"<< /Type /Pages /Kids [{kids}] /Count {pages.Count} >>");
        Obj("<< /Type /Font /Subtype /Type1 /BaseFont /Courier /Encoding /WinAnsiEncoding >>");
        Obj($"<< /Title ({Escape(NoteStore.TitleFrom(body))}) /Producer (ePad) " +
            $"/CreationDate (D:{DateTime.Now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture)}) >>");

        for (var p = 0; p < pages.Count; p++)
        {
            var content = new StringBuilder();
            content.Append(CultureInfo.InvariantCulture,
                $"BT\n/F1 {FontSize} Tf\n{Leading} TL\n{Margin} {pageHeight - Margin - FontSize} Td\n");
            foreach (var line in pages[p])
                content.Append('(').Append(Escape(line)).Append(") Tj T*\n");
            content.Append("ET\n");

            // Page number, bottom right.
            var footer = $"{p + 1} / {pages.Count}";
            var footerSize = 8f;
            var footerX = pageWidth - Margin - footer.Length * footerSize * CharWidth;
            content.Append(CultureInfo.InvariantCulture,
                $"BT\n/F1 {footerSize} Tf\n{footerX:0.##} {Margin / 2:0.##} Td\n({footer}) Tj\nET\n");

            var stream = content.ToString();
            Obj(FormattableString.Invariant(
                $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {pageWidth} {pageHeight}] /Resources << /Font << /F1 3 0 R >> >> /Contents {firstPageObj + p * 2 + 1} 0 R >>"));
            Obj($"<< /Length {stream.Length} >>\nstream\n{stream}endstream");
        }

        var xref = pdf.Length;
        pdf.Append("xref\n0 ").Append(offsets.Count + 1).Append('\n');
        pdf.Append("0000000000 65535 f \n");
        foreach (var o in offsets) pdf.Append(o.ToString("D10")).Append(" 00000 n \n");
        pdf.Append($"trailer\n<< /Size {offsets.Count + 1} /Root 1 0 R /Info 4 0 R >>\nstartxref\n{xref}\n%%EOF\n");

        return Encoding.ASCII.GetBytes(pdf.ToString());
    }

    private static string ExpandTabs(string line, int tabWidth)
    {
        if (!line.Contains('\t')) return line;
        var sb = new StringBuilder();
        foreach (var c in line)
        {
            if (c == '\t') sb.Append(' ', tabWidth - sb.Length % tabWidth);
            else sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>Breaks a line to fit the page, preferring to break at a space.</summary>
    private static IEnumerable<string> Wrap(string line, int max)
    {
        while (line.Length > max)
        {
            var cut = line.LastIndexOf(' ', max);
            if (cut <= 0)
            {
                yield return line[..max];
                line = line[max..];
            }
            else
            {
                yield return line[..cut];
                line = line[(cut + 1)..];
            }
        }
        yield return line;
    }

    /// <summary>Escapes PDF string delimiters and replaces anything outside printable ASCII.</summary>
    private static string Escape(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var c in s)
        {
            if (c is '(' or ')' or '\\') sb.Append('\\').Append(c);
            else if (c < 32 || c > 126) sb.Append('?');
            else sb.Append(c);
        }
        return sb.ToString();
    }
}

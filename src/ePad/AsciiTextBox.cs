using System.Text;

namespace ePad;

/// <summary>
/// A plain multiline TextBox that only accepts 7-bit ASCII.
/// Typed non-ASCII characters are rejected; pasted text is converted
/// (smart quotes, dashes, etc. become their ASCII equivalents, anything else becomes '?').
/// </summary>
public sealed class AsciiTextBox : TextBox
{
    private const int WM_PASTE = 0x0302;
    private const int EM_SETTABSTOPS = 0x00CB;

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, ref int lParam);

    private int _tabWidth = 4;

    /// <summary>Tab stop width, in characters.</summary>
    [System.ComponentModel.DefaultValue(4)]
    public int TabWidth
    {
        get => _tabWidth;
        set { _tabWidth = Math.Clamp(value, 1, 16); ApplyTabStops(); }
    }

    // Tab stops are lost whenever the native control is recreated (e.g. when WordWrap changes).
    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyTabStops();
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        ApplyTabStops();
    }

    private void ApplyTabStops()
    {
        if (!IsHandleCreated) return;
        var dialogUnits = _tabWidth * 4; // 4 dialog units = 1 average character width
        SendMessage(Handle, EM_SETTABSTOPS, 1, ref dialogUnits);
        Invalidate();
    }

    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        if (e.KeyChar > 127) e.Handled = true;
        base.OnKeyPress(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_PASTE)
        {
            if (Clipboard.ContainsText())
                SelectedText = ToAscii(Clipboard.GetText());
            return;
        }
        base.WndProc(ref m);
    }

    public static string ToAscii(string input)
    {
        // Decompose accented letters (e -> e + combining accent) so we can keep the base letter.
        var normalized = input.Normalize(NormalizationForm.FormKD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (c <= 127) { sb.Append(c); continue; }
            switch (char.GetUnicodeCategory(c))
            {
                case System.Globalization.UnicodeCategory.NonSpacingMark:
                    continue; // drop combining accents
            }
            sb.Append(c switch
            {
                '‘' or '’' or '‚' or '′' => "'",
                '“' or '”' or '„' or '″' => "\"",
                '–' or '—' or '−' => "-",
                '…' => "...",
                ' ' or ' ' or ' ' or ' ' => " ",
                '•' => "*",
                _ => "?",
            });
        }
        // Normalise line endings to CRLF for the Windows TextBox.
        return sb.ToString().Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
    }
}

using System.Drawing.Text;
using static ePad.Strings;

namespace ePad;

/// <summary>
/// Tools > Options dialog. Works on a copy of the settings and hands the result to
/// <c>apply</c> when the user clicks OK or Apply.
/// </summary>
public sealed class OptionsForm : Form
{
    private sealed record Choice(string Text, object Value)
    {
        public override string ToString() => Text;
    }

    private readonly AppSettings _original;
    private readonly Action<AppSettings> _apply;

    private readonly TabControl _tabs = new() { Dock = DockStyle.Fill };

    // Editor tab
    private readonly ComboBox _font = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox _monoOnly = new() { AutoSize = true };
    private readonly NumericUpDown _fontSize = new() { DecimalPlaces = 1, Increment = 0.5m };
    private readonly NumericUpDown _tabWidth = new() { Minimum = 1, Maximum = 16 };
    private readonly CheckBox _wordWrap = new() { AutoSize = true };

    // Appearance tab
    private readonly ComboBox _theme = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly CheckBox _customColors = new() { AutoSize = true };
    private readonly Button _textColor = new() { FlatStyle = FlatStyle.Flat, UseVisualStyleBackColor = false };
    private readonly Button _backColor = new() { FlatStyle = FlatStyle.Flat, UseVisualStyleBackColor = false };
    private readonly CheckBox _showToolbar = new() { AutoSize = true };
    private readonly CheckBox _showStatusBar = new() { AutoSize = true };

    // General tab
    private readonly ComboBox _language = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _dateFormat = new();
    private readonly Label _dateExample = new() { AutoSize = true };
    private readonly CheckBox _autoSave = new() { AutoSize = true };
    private readonly NumericUpDown _autoSaveDelay = new() { DecimalPlaces = 1, Increment = 0.5m, Minimum = 0.5m, Maximum = 60 };
    private readonly CheckBox _confirmDelete = new() { AutoSize = true };
    private readonly CheckBox _rememberWindow = new() { AutoSize = true };

    private readonly AsciiTextBox _preview = new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Both,
        WordWrap = false,
        TabStop = false,
    };

    private Color _textColorValue, _backColorValue;
    private bool _populating;

    public OptionsForm(AppSettings current, Action<AppSettings> apply)
    {
        _original = current.Clone();
        _apply = apply;

        Text = T("OptionsTitle");
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        BuildLayout();
        Populate(_original);
        WireEvents();
        UpdatePreview();
    }

    private int Px(int v) => (int)Math.Round(v * DeviceDpi / 96.0);

    // ---------- Layout ----------

    private void BuildLayout()
    {
        _font.Width = Px(240);
        _font.MaxDropDownItems = 20;
        _fontSize.Minimum = (decimal)AppSettings.MinFontSize;
        _fontSize.Maximum = (decimal)AppSettings.MaxFontSize;
        foreach (var n in new[] { _fontSize, _tabWidth, _autoSaveDelay }) n.Width = Px(80);
        _theme.Width = Px(240);
        _language.Width = Px(240);
        _dateFormat.Width = Px(240);
        foreach (var b in new[] { _textColor, _backColor }) b.Size = new Size(Px(110), Px(26));

        _monoOnly.Text = T("MonospaceOnly");
        _wordWrap.Text = T("WrapLines");
        _customColors.Text = T("CustomColors");
        _showToolbar.Text = T("ShowToolbar");
        _showStatusBar.Text = T("ShowStatusBar");
        _autoSave.Text = T("AutoSave");
        _confirmDelete.Text = T("ConfirmDelete");
        _rememberWindow.Text = T("RememberWindow");

        _theme.Items.AddRange([
            new Choice(T("UseWindows"), ThemeMode.System),
            new Choice(T("ThemeLight"), ThemeMode.Light),
            new Choice(T("ThemeDark"), ThemeMode.Dark),
        ]);
        _language.Items.Add(new Choice(T("UseWindows"), ""));
        foreach (var (code, name) in Languages) _language.Items.Add(new Choice(name, code));

        var editor = Grid();
        Row(editor, T("Font"), _font);
        Row(editor, null, _monoOnly);
        Row(editor, T("FontSize"), _fontSize);
        Row(editor, T("TabWidth"), _tabWidth);
        Row(editor, null, _wordWrap);

        var appearance = Grid();
        Row(appearance, T("Theme"), _theme);
        Row(appearance, null, Hint(T("RequiresRestart")));
        Full(appearance, _customColors, topGap: 10);
        Row(appearance, T("TextColor"), _textColor);
        Row(appearance, T("BackColor"), _backColor);
        Full(appearance, _showToolbar, topGap: 10);
        Full(appearance, _showStatusBar);

        var general = Grid();
        Row(general, T("Language"), _language);
        Row(general, null, Hint(T("RequiresRestart")));
        Row(general, T("DateFormat"), _dateFormat, topGap: 10);
        Row(general, null, _dateExample);
        Full(general, _autoSave, topGap: 10);
        Row(general, T("AutoSaveDelay"), _autoSaveDelay);
        Full(general, _confirmDelete, topGap: 10);
        Full(general, _rememberWindow);

        AddTab(T("TabEditor"), editor);
        AddTab(T("TabAppearance"), appearance);
        AddTab(T("TabGeneral"), general);

        var ok = new Button { Text = T("OK"), AutoSize = true, MinimumSize = new Size(Px(80), 0) };
        var cancel = new Button { Text = T("Cancel"), AutoSize = true, MinimumSize = new Size(Px(80), 0), DialogResult = DialogResult.Cancel };
        var apply = new Button { Text = T("Apply"), AutoSize = true, MinimumSize = new Size(Px(80), 0) };
        var reset = new Button { Text = T("ResetDefaults"), AutoSize = true };
        ok.Click += (_, _) => { ApplyChanges(); DialogResult = DialogResult.OK; };
        apply.Click += (_, _) => ApplyChanges();
        reset.Click += (_, _) => { Populate(new AppSettings()); UpdatePreview(); };
        AcceptButton = ok;
        CancelButton = cancel;

        var buttons = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 5, Margin = new Padding(0, Px(8), 0, 0) };
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 3; i++) buttons.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        buttons.Controls.Add(reset, 0, 0);
        buttons.Controls.Add(ok, 2, 0);
        buttons.Controls.Add(cancel, 3, 0);
        buttons.Controls.Add(apply, 4, 0);

        var outer = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            Padding = new Padding(Px(10)),
        };
        outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, Px(100)));
        outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        outer.Controls.Add(_tabs, 0, 0);
        outer.Controls.Add(new Label { Text = T("Preview"), AutoSize = true, Margin = new Padding(0, Px(10), 0, Px(3)) }, 0, 1);
        outer.Controls.Add(_preview, 0, 2);
        outer.Controls.Add(buttons, 0, 3);
        Controls.Add(outer);
    }

    private TableLayoutPanel Grid()
    {
        var g = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            Padding = new Padding(Px(10)),
            Location = Point.Empty,
        };
        g.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        g.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        return g;
    }

    private void Row(TableLayoutPanel g, string? label, Control control, int topGap = 0)
    {
        var top = Px(3 + topGap);
        g.Controls.Add(new Label
        {
            Text = label ?? "",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, top, Px(10), Px(3)),
        });
        control.Margin = new Padding(0, top, 0, Px(3));
        control.Anchor = AnchorStyles.Left;
        g.Controls.Add(control);
    }

    private void Full(TableLayoutPanel g, Control control, int topGap = 0)
    {
        control.Margin = new Padding(0, Px(3 + topGap), 0, Px(3));
        g.Controls.Add(control);
        g.SetColumnSpan(control, 2);
    }

    private Label Hint(string text) => new()
    {
        Text = text,
        AutoSize = true,
        ForeColor = SystemColors.GrayText,
    };

    private void AddTab(string title, TableLayoutPanel content)
    {
        // The visual-style background is always light, so turn it off to let dark mode color the page.
        var page = new TabPage(title) { UseVisualStyleBackColor = false };
        page.Controls.Add(content);
        _tabs.TabPages.Add(page);
    }

    // Size the tab control to fit its largest page (TabControl cannot auto-size itself).
    protected override void OnLoad(EventArgs e)
    {
        var sizes = _tabs.TabPages.Cast<TabPage>().Select(p => p.Controls[0].GetPreferredSize(Size.Empty)).ToList();
        var display = _tabs.DisplayRectangle;
        _tabs.MinimumSize = new Size(
            sizes.Max(s => s.Width) + (_tabs.Width - display.Width) + Px(8),
            sizes.Max(s => s.Height) + (_tabs.Height - display.Height) + Px(8));
        base.OnLoad(e);
    }

    // ---------- Data ----------

    private void Populate(AppSettings s)
    {
        _populating = true;
        _monoOnly.Checked = FontLists.IsMonospace(s.FontFamily);
        FillFonts(s.FontFamily);
        _fontSize.Value = (decimal)s.FontSize;
        _tabWidth.Value = s.TabWidth;
        _wordWrap.Checked = s.WordWrap;

        SelectChoice(_theme, s.Theme);
        _customColors.Checked = s.UseCustomColors;
        SetColor(_textColor, ref _textColorValue, s.GetTextColor());
        SetColor(_backColor, ref _backColorValue, s.GetBackColor());
        _showToolbar.Checked = s.ShowToolbar;
        _showStatusBar.Checked = s.ShowStatusBar;

        SelectChoice(_language, s.Language);
        _dateFormat.Text = s.DateTimeFormat;
        _autoSave.Checked = s.AutoSave;
        _autoSaveDelay.Value = s.AutoSaveDelaySeconds;
        _confirmDelete.Checked = s.ConfirmDelete;
        _rememberWindow.Checked = s.RememberWindow;
        _populating = false;
    }

    private AppSettings Collect()
    {
        var s = _original.Clone(); // keeps window position and anything not edited here
        s.FontFamily = _font.SelectedItem as string ?? s.FontFamily;
        s.FontSize = (float)_fontSize.Value;
        s.TabWidth = (int)_tabWidth.Value;
        s.WordWrap = _wordWrap.Checked;
        s.Theme = (ThemeMode)((Choice)_theme.SelectedItem!).Value;
        s.UseCustomColors = _customColors.Checked;
        s.TextColor = AppSettings.ToHex(_textColorValue);
        s.BackColor = AppSettings.ToHex(_backColorValue);
        s.ShowToolbar = _showToolbar.Checked;
        s.ShowStatusBar = _showStatusBar.Checked;
        s.Language = (string)((Choice)_language.SelectedItem!).Value;
        if (AppSettings.IsValidDateFormat(_dateFormat.Text)) s.DateTimeFormat = _dateFormat.Text;
        s.AutoSave = _autoSave.Checked;
        s.AutoSaveDelaySeconds = _autoSaveDelay.Value;
        s.ConfirmDelete = _confirmDelete.Checked;
        s.RememberWindow = _rememberWindow.Checked;
        return s;
    }

    private void ApplyChanges()
    {
        var s = Collect();
        _apply(s);
        _original.WindowBounds = s.WindowBounds;
    }

    private void FillFonts(string selected)
    {
        var names = _monoOnly.Checked ? FontLists.Monospaced : FontLists.All;
        _font.BeginUpdate();
        _font.Items.Clear();
        foreach (var n in names) _font.Items.Add(n);
        if (!_font.Items.Contains(selected)) _font.Items.Insert(0, selected);
        _font.SelectedItem = selected;
        _font.EndUpdate();
    }

    private static void SelectChoice(ComboBox combo, object value)
    {
        foreach (Choice c in combo.Items)
            if (Equals(c.Value, value)) { combo.SelectedItem = c; return; }
        combo.SelectedIndex = 0;
    }

    private static void SetColor(Button button, ref Color field, Color color)
    {
        field = color;
        button.BackColor = color;
        button.ForeColor = color.GetBrightness() > 0.5f ? Color.Black : Color.White;
        button.Text = AppSettings.ToHex(color);
    }

    private void PickColor(Button button, bool isText)
    {
        using var dlg = new ColorDialog { Color = isText ? _textColorValue : _backColorValue, FullOpen = true };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        if (isText) SetColor(button, ref _textColorValue, dlg.Color);
        else SetColor(button, ref _backColorValue, dlg.Color);
        UpdatePreview();
    }

    // ---------- Events and preview ----------

    private void WireEvents()
    {
        void Changed(object? _, EventArgs __) { if (!_populating) UpdatePreview(); }

        _monoOnly.CheckedChanged += (_, _) =>
        {
            if (_populating) return;
            FillFonts(_font.SelectedItem as string ?? "Consolas");
        };
        _font.SelectedIndexChanged += Changed;
        _fontSize.ValueChanged += Changed;
        _tabWidth.ValueChanged += Changed;
        _wordWrap.CheckedChanged += Changed;
        _customColors.CheckedChanged += Changed;
        _autoSave.CheckedChanged += Changed;
        _dateFormat.TextChanged += Changed;
        _textColor.Click += (_, _) => PickColor(_textColor, isText: true);
        _backColor.Click += (_, _) => PickColor(_backColor, isText: false);
    }

    private void UpdatePreview()
    {
        _preview.Text = T("PreviewText");
        _preview.Font = new Font(_font.SelectedItem as string ?? "Consolas", (float)_fontSize.Value);
        _preview.TabWidth = (int)_tabWidth.Value;
        _preview.WordWrap = _wordWrap.Checked;
        _preview.ScrollBars = _wordWrap.Checked ? ScrollBars.Vertical : ScrollBars.Both;

        if (_customColors.Checked)
        {
            _preview.ForeColor = _textColorValue;
            _preview.BackColor = _backColorValue;
        }
        else
        {
            _preview.ForeColor = SystemColors.WindowText;
            _preview.BackColor = SystemColors.Window;
        }

        _textColor.Enabled = _backColor.Enabled = _customColors.Checked;
        _autoSaveDelay.Enabled = _autoSave.Checked;

        _dateExample.Text = AppSettings.IsValidDateFormat(_dateFormat.Text)
            ? T("DateExample", DateTime.Now.ToString(_dateFormat.Text))
            : T("InvalidFormat");
        _dateExample.ForeColor = AppSettings.IsValidDateFormat(_dateFormat.Text) ? SystemColors.GrayText : Color.Firebrick;
    }
}

/// <summary>Installed font names, with a cached list of the monospaced ones.</summary>
internal static class FontLists
{
    private static List<string>? _all, _mono;

    public static List<string> All => _all ??= LoadAll();

    public static List<string> Monospaced => _mono ??= All.Where(IsMonospace).ToList();

    private static List<string> LoadAll()
    {
        using var fonts = new InstalledFontCollection();
        return fonts.Families
            .Where(f => f.IsStyleAvailable(FontStyle.Regular) && !f.Name.StartsWith('@'))
            .Select(f => f.Name)
            .ToList();
    }

    /// <summary>A font is treated as monospaced if narrow and wide letters measure the same.</summary>
    public static bool IsMonospace(string family)
    {
        try
        {
            using var font = new Font(family, 12f);
            if (!string.Equals(font.Name, family, StringComparison.OrdinalIgnoreCase)) return false; // not installed
            const TextFormatFlags flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
            var narrow = TextRenderer.MeasureText("iiiiiiiiii", font, Size.Empty, flags).Width;
            var wide = TextRenderer.MeasureText("WWWWWWWWWW", font, Size.Empty, flags).Width;
            return narrow == wide;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }
}

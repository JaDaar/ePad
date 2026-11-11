using static ePad.Strings;

namespace ePad;

public sealed class MainForm : Form
{
    private readonly NoteStore _store;
    private AppSettings _settings;

    // Theme and language are applied at startup only; remember what we started with.
    private readonly ThemeMode _startupTheme;
    private readonly string _startupLanguage;

    private readonly TextBox _search = new() { Dock = DockStyle.Top };
    private readonly ListBox _list = new() { Dock = DockStyle.Fill, IntegralHeight = false };
    private readonly AsciiTextBox _editor = new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        AcceptsTab = true,
        AcceptsReturn = true,
        ScrollBars = ScrollBars.Both,
        WordWrap = false,
        MaxLength = 0, // no length limit
        BorderStyle = BorderStyle.None,
    };
    private readonly ToolStripStatusLabel _status = new() { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ToolStripStatusLabel _counts = new();
    private readonly StatusStrip _statusStrip = new();
    private ToolStrip _toolbar = null!;
    private readonly ToolStripMenuItem _wordWrapItem = new();
    private readonly ToolStripMenuItem _toolbarItem = new();
    private readonly ToolStripMenuItem _statusBarItem = new();
    private readonly System.Windows.Forms.Timer _autoSave = new();

    private long? _currentId;
    private bool _dirty;
    private bool _loading;

    public MainForm(NoteStore store, AppSettings settings)
    {
        _store = store;
        _settings = settings;
        _startupTheme = settings.Theme;
        _startupLanguage = settings.Language;

        Text = "ePad";
        using (var iconStream = typeof(MainForm).Assembly.GetManifestResourceStream("ePad.ico"))
            if (iconStream is not null) Icon = new Icon(iconStream);
        Width = 1000;
        Height = 650;
        StartPosition = FormStartPosition.CenterScreen;
        _search.PlaceholderText = T("SearchPlaceholder");

        BuildLayout();
        WireEvents();
        RestoreWindowPosition();
        ApplySettings();

        RefreshList();
        if (_list.Items.Count > 0) _list.SelectedIndex = 0;
        else NewNote();
    }

    // ---------- Layout ----------

    private void BuildLayout()
    {
        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            FixedPanel = FixedPanel.Panel1,
        };
        Load += (_, _) => split.SplitterDistance = (int)Math.Round(240 * DeviceDpi / 96.0);
        split.Panel1.Controls.Add(_list);
        split.Panel1.Controls.Add(_search);
        split.Panel2.Controls.Add(_editor);

        static ToolStripMenuItem Item(string text, EventHandler onClick, Keys keys = Keys.None, string? keyText = null) =>
            new(text, null, onClick, keys) { ShortcutKeyDisplayString = keyText };

        var file = new ToolStripMenuItem(T("File"));
        file.DropDownItems.AddRange([
            Item(T("NewNote"), (_, _) => NewNote(), Keys.Control | Keys.N),
            Item(T("Save"), (_, _) => SaveCurrent(), Keys.Control | Keys.S),
            Item(T("DeleteNote"), (_, _) => DeleteCurrent(), Keys.Control | Keys.Shift | Keys.D),
            new ToolStripSeparator(),
            Item(T("Export"), (_, _) => { if (_currentId is long id) ExportNote(id, null); }, Keys.Control | Keys.E),
            Item(T("ImportTxt"), (_, _) => ImportFile(), Keys.Control | Keys.O),
            new ToolStripSeparator(),
            Item(T("OpenDataFolder"), (_, _) => OpenDataFolder()),
            Item(T("Exit"), (_, _) => Close(), Keys.Alt | Keys.F4),
        ]);

        var edit = new ToolStripMenuItem(T("Edit"));
        edit.DropDownItems.AddRange([
            Item(T("FindNote"), (_, _) => _search.Focus(), Keys.Control | Keys.F),
            Item(T("InsertDateTime"), (_, _) => InsertDateTime(), Keys.F5),
        ]);

        _wordWrapItem.Text = T("WordWrap");
        _wordWrapItem.Click += (_, _) => { _settings.WordWrap = !_settings.WordWrap; SettingsChanged(); };
        _toolbarItem.Text = T("Toolbar");
        _toolbarItem.Click += (_, _) => { _settings.ShowToolbar = !_settings.ShowToolbar; SettingsChanged(); };
        _statusBarItem.Text = T("StatusBar");
        _statusBarItem.Click += (_, _) => { _settings.ShowStatusBar = !_settings.ShowStatusBar; SettingsChanged(); };

        var view = new ToolStripMenuItem(T("View"));
        view.DropDownItems.AddRange([
            _wordWrapItem,
            _toolbarItem,
            _statusBarItem,
            new ToolStripSeparator(),
            Item(T("ZoomIn"), (_, _) => Zoom(+1), Keys.Control | Keys.Oemplus, "Ctrl++"),
            Item(T("ZoomOut"), (_, _) => Zoom(-1), Keys.Control | Keys.OemMinus, "Ctrl+-"),
            Item(T("ZoomReset"), (_, _) => Zoom(0), Keys.Control | Keys.D0, "Ctrl+0"),
        ]);

        var tools = new ToolStripMenuItem(T("Tools"));
        tools.DropDownItems.Add(Item(T("Options"), (_, _) => OpenOptions(), Keys.Control | Keys.Oemcomma, "Ctrl+,"));

        var help = new ToolStripMenuItem(T("Help"));
        help.DropDownItems.Add(Item(T("About"), (_, _) =>
        {
            using var about = new AboutForm();
            about.ShowDialog(this);
        }));

        var menu = new MenuStrip();
        menu.Items.AddRange([file, edit, view, tools, help]);

        _statusStrip.Items.AddRange([_status, _counts]);
        _toolbar = BuildToolbar();
        _list.ContextMenuStrip = BuildListMenu();

        // Controls dock in reverse order of adding: menu at the very top, toolbar below it.
        Controls.Add(split);
        Controls.Add(_toolbar);
        Controls.Add(menu);
        Controls.Add(_statusStrip);
        MainMenuStrip = menu;
    }

    private ToolStrip BuildToolbar()
    {
        var iconSize = (int)Math.Round(20 * DeviceDpi / 96.0);
        var toolbar = new ToolStrip
        {
            GripStyle = ToolStripGripStyle.Hidden,
            ImageScalingSize = new Size(iconSize, iconSize),
            Padding = new Padding(4, 2, 4, 2),
        };

        ToolStripButton Button(string text, char glyph, Color color, string tip, EventHandler onClick)
        {
            var image = Icons.Render(glyph, iconSize, color);
            return new ToolStripButton(text, image, onClick)
            {
                // Show the label only if the icon font is missing.
                DisplayStyle = image is null ? ToolStripItemDisplayStyle.Text : ToolStripItemDisplayStyle.Image,
                ToolTipText = tip,
                AutoToolTip = false,
                Margin = new Padding(2, 1, 2, 1),
            };
        }

        toolbar.Items.Add(Button(T("NewNote").Replace("&", ""), Icons.New, Color.FromArgb(0, 120, 212), T("NewTip"), (_, _) => NewNote()));
        toolbar.Items.Add(Button(T("Save").Replace("&", ""), Icons.Save, Color.FromArgb(16, 124, 16), T("SaveTip"), (_, _) => SaveCurrent()));
        toolbar.Items.Add(new ToolStripSeparator());
        toolbar.Items.Add(Button(T("DeleteNote").Replace("&", ""), Icons.Delete, Color.FromArgb(196, 43, 28), T("DeleteTip"), (_, _) => DeleteCurrent()));
        return toolbar;
    }

    /// <summary>Right-click menu for the note list.</summary>
    private ContextMenuStrip BuildListMenu()
    {
        var menu = new ContextMenuStrip();
        void Add(string text, ExportFormat format) =>
            menu.Items.Add(text, null, (_, _) =>
            {
                if (_list.SelectedItem is NoteSummary s) ExportNote(s.Id, format);
            });
        Add(T("ExportAsTxt"), ExportFormat.Txt);
        Add(T("ExportAsPdf"), ExportFormat.Pdf);
        Add(T("ExportAsMd"), ExportFormat.Md);
        menu.Items.Add(new ToolStripSeparator());

        // Right-click has already selected (and opened) the note, so the normal delete applies to it,
        // including the confirmation prompt.
        var iconSize = (int)Math.Round(16 * DeviceDpi / 96.0);
        menu.Items.Add(new ToolStripMenuItem(T("DeleteNote"),
            Icons.Render(Icons.Delete, iconSize, Color.FromArgb(196, 43, 28)),
            (_, _) => DeleteCurrent())
        {
            ShortcutKeyDisplayString = "Ctrl+Shift+D",
        });

        // Only open when a note is selected (e.g. not for a right-click on empty space).
        menu.Opening += (_, e) => e.Cancel = _list.SelectedItem is null;
        return menu;
    }

    private void WireEvents()
    {
        // A ListBox doesn't select on right-click by itself; select the note under the mouse first.
        _list.MouseDown += (_, e) =>
        {
            if (e.Button != MouseButtons.Right) return;
            var index = _list.IndexFromPoint(e.Location);
            if (index == ListBox.NoMatches) _list.ClearSelected();
            else _list.SelectedIndex = index;
        };
        _list.SelectedIndexChanged += (_, _) =>
        {
            if (_loading || _list.SelectedItem is not NoteSummary s || s.Id == _currentId) return;
            SaveCurrent();
            LoadNote(s.Id);
        };
        _editor.TextChanged += (_, _) =>
        {
            UpdateCounts();
            if (_loading) return;
            _dirty = true;
            _autoSave.Stop();
            if (_settings.AutoSave)
            {
                _status.Text = T("Editing");
                _autoSave.Start();
            }
            else
            {
                _status.Text = T("UnsavedChanges");
            }
        };
        _search.TextChanged += (_, _) => { SaveCurrent(); RefreshList(); };
        _search.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Down || e.KeyCode == Keys.Enter) { _list.Focus(); e.SuppressKeyPress = true; }
            if (e.KeyCode == Keys.Escape) { _search.Clear(); _editor.Focus(); e.SuppressKeyPress = true; }
        };
        _autoSave.Tick += (_, _) => SaveCurrent();
        FormClosing += (_, _) =>
        {
            SaveCurrent();
            RememberWindowPosition();
            SaveSettings();
        };
    }

    // ---------- Settings ----------

    /// <summary>Applies everything that can change while the app is running.</summary>
    private void ApplySettings()
    {
        var s = _settings;
        _editor.Font = new Font(s.FontFamily, s.FontSize);
        _editor.TabWidth = s.TabWidth;

        if (s.UseCustomColors)
        {
            _editor.ForeColor = s.GetTextColor();
            _editor.BackColor = s.GetBackColor();
        }
        else
        {
            _editor.ResetForeColor();
            _editor.ResetBackColor();
        }

        if (_editor.WordWrap != s.WordWrap)
        {
            _editor.WordWrap = s.WordWrap;
            _editor.ScrollBars = s.WordWrap ? ScrollBars.Vertical : ScrollBars.Both;
        }
        _wordWrapItem.Checked = s.WordWrap;

        _toolbar.Visible = _toolbarItem.Checked = s.ShowToolbar;
        _statusStrip.Visible = _statusBarItem.Checked = s.ShowStatusBar;

        _autoSave.Interval = (int)(s.AutoSaveDelaySeconds * 1000);
        if (!s.AutoSave) _autoSave.Stop();
    }

    private void SettingsChanged()
    {
        ApplySettings();
        SaveSettings();
    }

    private void SaveSettings()
    {
        try { _settings.Save(_store); }
        catch (Exception ex) { _status.Text = T("SaveFailed", ex.Message); }
    }

    private void OpenOptions()
    {
        var before = (_settings.Theme, _settings.Language);
        using (var dlg = new OptionsForm(_settings, s => { _settings = s; SettingsChanged(); }))
            dlg.ShowDialog(this);

        var after = (_settings.Theme, _settings.Language);
        var needsRestart = after != before && after != (_startupTheme, _startupLanguage);
        if (needsRestart &&
            MessageBox.Show(this, T("RestartPrompt"), "ePad", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            Application.Restart(); // FormClosing saves the current note first
        }
    }

    private void Zoom(int step)
    {
        _settings.FontSize = step == 0
            ? new AppSettings().FontSize
            : Math.Clamp(_settings.FontSize + step, AppSettings.MinFontSize, AppSettings.MaxFontSize);
        SettingsChanged();
    }

    private void RestoreWindowPosition()
    {
        if (!_settings.RememberWindow || _settings.WindowBounds is not [var x, var y, var w, var h]) return;
        var rect = new Rectangle(x, y, w, h);
        // Ignore saved positions that are too small or no longer on any screen (e.g. monitor unplugged).
        if (w < 300 || h < 200 || !Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(rect))) return;
        StartPosition = FormStartPosition.Manual;
        Bounds = rect;
        if (_settings.WindowMaximized) WindowState = FormWindowState.Maximized;
    }

    private void RememberWindowPosition()
    {
        if (WindowState == FormWindowState.Minimized) return;
        var r = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
        _settings.WindowBounds = [r.X, r.Y, r.Width, r.Height];
        _settings.WindowMaximized = WindowState == FormWindowState.Maximized;
    }

    // ---------- Notes ----------

    private void NewNote()
    {
        SaveCurrent();
        _search.Clear();
        var id = _store.Create();
        RefreshList(id);
        LoadNote(id);
        _editor.Focus();
    }

    private void LoadNote(long id)
    {
        var note = _store.Get(id);
        if (note is null) return;
        _loading = true;
        _editor.Text = note.Body;
        _editor.SelectionStart = 0;
        _loading = false;
        _currentId = id;
        _dirty = false;
        _status.Text = T("LastSaved", note.ModifiedUtc.ToLocalTime().ToString("g"));
        UpdateCounts();
    }

    private void SaveCurrent()
    {
        _autoSave.Stop();
        if (!_dirty || _currentId is not long id) return;
        try
        {
            _store.Update(id, _editor.Text);
            _dirty = false;
            _status.Text = T("Saved", DateTime.Now.ToString("T"));
            UpdateListTitle(id);
        }
        catch (Exception ex)
        {
            _status.Text = T("SaveFailed", ex.Message);
        }
    }

    private void DeleteCurrent()
    {
        if (_currentId is not long id) return;
        var title = NoteStore.TitleFrom(_editor.Text);
        if (_settings.ConfirmDelete &&
            MessageBox.Show(this,
                T("DeleteConfirm", title.Length > 0 ? title : T("EmptyNote")) + "\n\n" + T("CannotUndo"),
                T("DeleteTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;

        _autoSave.Stop();
        _store.Delete(id);
        _currentId = null;
        _dirty = false;
        RefreshList();
        if (_list.Items.Count > 0) _list.SelectedIndex = 0;
        else NewNote();
    }

    private void InsertDateTime()
    {
        var format = AppSettings.IsValidDateFormat(_settings.DateTimeFormat) ? _settings.DateTimeFormat : "g";
        _editor.SelectedText = DateTime.Now.ToString(format);
    }

    /// <summary>Exports a note. With no format given, the user picks one in the save dialog.</summary>
    private void ExportNote(long id, ExportFormat? format)
    {
        SaveCurrent();
        var note = _store.Get(id);
        if (note is null) return;

        using var dlg = new SaveFileDialog
        {
            Filter = format switch
            {
                ExportFormat.Txt => T("TextFilter"),
                ExportFormat.Pdf => T("PdfFilter"),
                ExportFormat.Md => T("MdFilter"),
                _ => T("ExportFilter"),
            },
            DefaultExt = format?.ToString().ToLowerInvariant() ?? "txt",
            AddExtension = true,
            FileName = MakeFileName(NoteStore.TitleFrom(note.Body)),
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            NoteExporter.Export(dlg.FileName, note.Body, format ?? NoteExporter.FormatFromPath(dlg.FileName), _settings.TabWidth);
            _status.Text = T("Exported", dlg.FileName);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, T("ExportFailed", ex.Message), "ePad", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ImportFile()
    {
        using var dlg = new OpenFileDialog { Filter = T("TextFilter"), Multiselect = true };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        SaveCurrent();
        long last = 0;
        foreach (var path in dlg.FileNames)
            last = _store.Create(AsciiTextBox.ToAscii(File.ReadAllText(path)));
        _search.Clear();
        RefreshList(last);
        LoadNote(last);
    }

    private void OpenDataFolder() =>
        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{_store.DatabasePath}\"");

    private void RefreshList(long? selectId = null)
    {
        selectId ??= _currentId;
        _loading = true;
        _list.BeginUpdate();
        _list.Items.Clear();
        foreach (var n in _store.List(_search.Text)) _list.Items.Add(n);
        _list.SelectedIndex = -1;
        for (var i = 0; i < _list.Items.Count; i++)
            if (((NoteSummary)_list.Items[i]).Id == selectId) { _list.SelectedIndex = i; break; }
        _list.EndUpdate();
        _loading = false;
    }

    private void UpdateListTitle(long id)
    {
        for (var i = 0; i < _list.Items.Count; i++)
        {
            if (((NoteSummary)_list.Items[i]).Id != id) continue;
            _loading = true;
            // Move the edited note to the top, since the list is ordered by last modified.
            _list.Items.RemoveAt(i);
            _list.Items.Insert(0, new NoteSummary(id, NoteStore.TitleFrom(_editor.Text), DateTime.UtcNow));
            _list.SelectedIndex = 0;
            _loading = false;
            return;
        }
    }

    private void UpdateCounts()
    {
        var text = _editor.Text;
        var lines = text.Length == 0 ? 1 : text.Count(c => c == '\n') + 1;
        _counts.Text = T("LinesChars", lines, text.Length);
    }

    private static string MakeFileName(string title)
    {
        if (string.IsNullOrWhiteSpace(title)) title = "note";
        foreach (var c in Path.GetInvalidFileNameChars()) title = title.Replace(c, '_');
        return title; // the save dialog adds the extension for the chosen format
    }
}

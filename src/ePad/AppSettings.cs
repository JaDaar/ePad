using System.Drawing.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ePad;

public enum ThemeMode { System, Light, Dark }

/// <summary>
/// User preferences. Stored as one JSON value in the "settings" table of the notes database.
/// </summary>
public sealed class AppSettings
{
    // Editor
    public string FontFamily { get; set; } = "Consolas";
    public float FontSize { get; set; } = 11f;
    public int TabWidth { get; set; } = 4;
    public bool WordWrap { get; set; }

    // Appearance
    public ThemeMode Theme { get; set; } = ThemeMode.System;
    public bool UseCustomColors { get; set; }
    public string TextColor { get; set; } = "#000000";
    public string BackColor { get; set; } = "#FFFFFF";
    public bool ShowToolbar { get; set; } = true;
    public bool ShowStatusBar { get; set; } = true;

    // General
    public string Language { get; set; } = ""; // "" = follow Windows
    public string DateTimeFormat { get; set; } = "yyyy-MM-dd HH:mm";
    public bool AutoSave { get; set; } = true;
    public decimal AutoSaveDelaySeconds { get; set; } = 1.5m;
    public bool ConfirmDelete { get; set; } = true;
    public bool RememberWindow { get; set; } = true;

    // Window state (not shown in the Options dialog)
    public int[]? WindowBounds { get; set; }
    public bool WindowMaximized { get; set; }

    public const float MinFontSize = 6f, MaxFontSize = 72f;

    private const string StorageKey = "app";

    private static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static AppSettings Load(NoteStore store)
    {
        try
        {
            var json = store.GetSetting(StorageKey);
            if (json is not null)
                return (JsonSerializer.Deserialize<AppSettings>(json, Json) ?? new AppSettings()).Normalized();
        }
        catch (Exception)
        {
            // Corrupt or incompatible settings: fall back to defaults.
        }
        return new AppSettings();
    }

    public void Save(NoteStore store) => store.SetSetting(StorageKey, JsonSerializer.Serialize(this, Json));

    public AppSettings Clone()
    {
        var copy = (AppSettings)MemberwiseClone();
        copy.WindowBounds = WindowBounds?.ToArray();
        return copy;
    }

    /// <summary>Clamps values into valid ranges and replaces fonts that are no longer installed.</summary>
    public AppSettings Normalized()
    {
        var defaults = new AppSettings();
        if (!IsFontInstalled(FontFamily)) FontFamily = defaults.FontFamily;
        FontSize = Math.Clamp(FontSize, MinFontSize, MaxFontSize);
        TabWidth = Math.Clamp(TabWidth, 1, 16);
        AutoSaveDelaySeconds = Math.Clamp(AutoSaveDelaySeconds, 0.5m, 60m);
        if (!IsValidDateFormat(DateTimeFormat)) DateTimeFormat = defaults.DateTimeFormat;
        TextColor = ToHex(ParseColor(TextColor, Color.Black));
        BackColor = ToHex(ParseColor(BackColor, Color.White));
        Language ??= "";
        return this;
    }

    public Color GetTextColor() => ParseColor(TextColor, Color.Black);
    public Color GetBackColor() => ParseColor(BackColor, Color.White);

    public static Color ParseColor(string? value, Color fallback)
    {
        try { return string.IsNullOrWhiteSpace(value) ? fallback : ColorTranslator.FromHtml(value); }
        catch (Exception) { return fallback; }
    }

    public static string ToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    public static bool IsValidDateFormat(string? format)
    {
        if (string.IsNullOrWhiteSpace(format)) return false;
        try { _ = DateTime.Now.ToString(format); return true; }
        catch (FormatException) { return false; }
    }

    private static HashSet<string>? _installedFonts;

    public static bool IsFontInstalled(string name)
    {
        if (_installedFonts is null)
        {
            using var fonts = new InstalledFontCollection();
            _installedFonts = fonts.Families.Select(f => f.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
        return _installedFonts.Contains(name);
    }
}

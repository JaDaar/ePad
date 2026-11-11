namespace ePad;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();

        // Optional: "ePad.exe --db <path>" uses a different notes database (portable use, testing).
        var dbIndex = Array.IndexOf(args, "--db");
        var dbPath = dbIndex >= 0 && dbIndex + 1 < args.Length ? Path.GetFullPath(args[dbIndex + 1]) : NoteStore.DefaultPath;
        var store = new NoteStore(dbPath);
        var settings = AppSettings.Load(store);

        // Language and theme must be set before any window is created.
        Strings.SetLanguage(settings.Language);
#pragma warning disable WFO5001 // Dark mode is marked experimental in WinForms
        Application.SetColorMode(settings.Theme switch
        {
            ThemeMode.Light => SystemColorMode.Classic,
            ThemeMode.Dark => SystemColorMode.Dark,
            _ => SystemColorMode.System,
        });
#pragma warning restore WFO5001

        Application.Run(new MainForm(store, settings));
    }
}

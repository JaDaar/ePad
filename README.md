# ePad

A small Windows app for quick plain-text notes, by Gnomic Software. It's written in C# with WinForms on .NET 10, and notes are saved to a local SQLite database.

## Build and run

- **Visual Studio 2026:** open `ePad.slnx` and press F5.
- **VS Code:** install the C# Dev Kit extension, open the folder, then run `dotnet run --project src/ePad`.
- **Command line:** `dotnet run --project src/ePad`

To use a different database file, for example a portable copy on a USB drive, start ePad with `--db <path>`:

```
dotnet run --project src/ePad -- --db D:\notes.db
```

## Building a release

```
powershell -ExecutionPolicy Bypass -File build\release.ps1
```

This needs the .NET 10 SDK and Inno Setup 6 (`winget install JRSoftware.InnoSetup`). It writes two files to `artifacts\`:

| File | What it is |
|---|---|
| `ePad-Setup-<version>.exe` | Installer. Installs for the current user without admin rights by default; "install for all users" is offered on the first page. Adds a Start menu shortcut, an optional desktop icon, and an entry in Settings > Apps for uninstalling. Shown in English, Spanish, French or German. |
| `ePad-<version>-win-x64-portable.zip` | A single `ePad.exe` that runs from anywhere without installing. |

Both are self-contained, so the target PC doesn't need .NET installed. They run on Windows 10 (1809 or later) and Windows 11, including ARM PCs, which run x64 apps through emulation.

- **Notes are never deleted** by uninstalling or upgrading. They stay in `%LOCALAPPDATA%\ePad\notes.db`.
- **Releasing a new version:** change `<Version>` in [ePad.csproj](src/ePad/ePad.csproj) and run the script again. The installer upgrades an existing installation in place.
- **Don't change `AppId`** in [installer/ePad.iss](installer/ePad.iss). Windows uses it to recognise upgrades.
- **SmartScreen warning:** the files aren't code-signed, so Windows SmartScreen shows "Windows protected your PC" the first time someone runs them. Users can click **More info > Run anyway**. Removing the warning requires a code-signing certificate.
- **App icon:** [build/make-icon.ps1](build/make-icon.ps1) regenerates `src/ePad/ePad.ico`.

## How it works

- Notes and settings live in `%LOCALAPPDATA%\ePad\notes.db`.
  - Table `notes` has the columns `id`, `title`, `body`, `created_utc` and `modified_utc`. The body is stored exactly as typed.
  - Table `settings` holds your preferences as one JSON value.
- The title is the first line of the note that isn't blank.
- Notes save automatically after you stop typing (1.5 s by default; you can change this in Options), when you switch notes, and when you close the app.
- The editor only accepts ASCII. Characters outside ASCII can't be typed. Pasted or imported text is converted: smart quotes become `'` and `"`, dashes become `-`, accents are dropped, and anything else becomes `?`.
- There's no formatting and no length limit.

## Exporting

Right-click a note in the list and choose **Export as .txt**, **.pdf** or **.md**. The same menu also has **Delete Note**, which asks before deleting unless you've turned that off in Options. **File > Export** (Ctrl+E) exports the open note and lets you pick the format in the save dialog.

- **.txt:** the note exactly as typed.
- **.md:** the first line becomes a `# heading`, and the rest is written as-is.
- **.pdf:** the note in Courier 10 pt, with long lines wrapped, tabs expanded and page numbers. Paper is A4 or US Letter depending on your Windows region. ePad writes the PDF itself, so no extra library is needed.

## Options (Tools > Options, Ctrl+,)

| Tab | Settings |
|---|---|
| Editor | Font (can show only monospaced fonts), size, tab width, word wrap |
| Appearance | Theme (Windows setting, Light or Dark), custom text and background colors, show toolbar, show status bar |
| General | Language, date/time format for F5, auto-save and its delay, ask before deleting, remember window size and position |

- A preview at the bottom of the dialog shows the font, colors and tab width.
- Most changes apply as soon as you click Apply or OK.
- Theme and language changes take effect after a restart. ePad offers to restart for you.

**Languages:** English, Spanish, French and German. The UI text is in [Strings.cs](src/ePad/Strings.cs). Each string has one column per language, and any missing translation falls back to English. To add a language, add it to `Languages` and add a column to each entry.

Some text follows Windows rather than the ePad language:

- Dates in the status bar use your Windows region format.
- The Yes/No buttons on message boxes come from Windows.

## Shortcuts

| Key | Action |
|---|---|
| Ctrl+N | New note |
| Ctrl+S | Save now |
| Ctrl+Shift+D | Delete note |
| Ctrl+F | Search notes |
| Ctrl+E | Export the open note (.txt, .pdf or .md) |
| Ctrl+O | Import .txt files |
| F5 | Insert date/time |
| Ctrl++ / Ctrl+- / Ctrl+0 | Zoom in / out / reset |
| Ctrl+, | Options |

## Source files

- `src/ePad/Program.cs`: startup; loads settings and applies the theme and language
- `src/ePad/NoteStore.cs`: SQLite data access (notes and settings)
- `src/ePad/AppSettings.cs`: the preferences model, with defaults and validation
- `src/ePad/Strings.cs`: UI text in each language
- `src/ePad/MainForm.cs`: the main window, built in code
- `src/ePad/OptionsForm.cs`: the Tools > Options dialog
- `src/ePad/AboutForm.cs`: the Help > About dialog
- `src/ePad/NoteExporter.cs`: export to .txt, .md and .pdf
- `src/ePad/AsciiTextBox.cs`: editor control that only accepts ASCII, with adjustable tab width
- `src/ePad/Icons.cs`: draws the toolbar icons from the Windows icon font

## License

MIT License. Copyright (c) 2026 Gnomic Software. See [LICENSE](LICENSE).

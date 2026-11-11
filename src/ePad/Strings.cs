using System.Globalization;

namespace ePad;

/// <summary>
/// User-interface text in each supported language.
/// Each entry lists translations in the same order as <see cref="Languages"/>.
/// A missing translation falls back to English.
/// </summary>
public static class Strings
{
    public static readonly (string Code, string NativeName)[] Languages =
    [
        ("en", "English"),
        ("es", "Español"),
        ("fr", "Français"),
        ("de", "Deutsch"),
    ];

    private static int _index;

    public static string CurrentCode => Languages[_index].Code;

    /// <summary>Selects the UI language. An empty code means "use the Windows display language".</summary>
    public static void SetLanguage(string? code)
    {
        if (string.IsNullOrEmpty(code)) code = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        var i = Array.FindIndex(Languages, l => l.Code == code);
        _index = i < 0 ? 0 : i;

        var culture = CultureInfo.GetCultureInfo(Languages[_index].Code);
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }

    public static string T(string key)
    {
        if (!Table.TryGetValue(key, out var t)) return key;
        return _index < t.Length && t[_index] is { Length: > 0 } s ? s : t[0];
    }

    public static string T(string key, params object[] args) => string.Format(T(key), args);

    //                                   English                       Español                              Français                                  Deutsch
    private static readonly Dictionary<string, string[]> Table = new()
    {
        // Menus
        ["File"] =           ["&File",                       "&Archivo",                          "&Fichier",                               "&Datei"],
        ["NewNote"] =        ["&New Note",                   "&Nueva nota",                       "&Nouvelle note",                         "&Neue Notiz"],
        ["Save"] =           ["&Save",                       "&Guardar",                          "&Enregistrer",                           "&Speichern"],
        ["DeleteNote"] =     ["&Delete Note",                "&Eliminar nota",                    "&Supprimer la note",                     "Notiz &löschen"],
        ["Export"] =         ["&Export...",                  "&Exportar...",                      "E&xporter...",                           "&Exportieren..."],
        ["ExportAsTxt"] =    ["Export as .&txt...",          "Exportar como .&txt...",            "Exporter en .&txt...",                   "Als .&txt exportieren..."],
        ["ExportAsPdf"] =    ["Export as .&pdf...",          "Exportar como .&pdf...",            "Exporter en .&pdf...",                   "Als .&pdf exportieren..."],
        ["ExportAsMd"] =     ["Export as .&md...",           "Exportar como .&md...",             "Exporter en .&md...",                    "Als .&md exportieren..."],
        ["ImportTxt"] =      ["&Import .txt...",             "&Importar .txt...",                 "&Importer un .txt...",                   ".txt &importieren..."],
        ["OpenDataFolder"] = ["Open Data &Folder",           "Abrir carpeta de &datos",           "Ouvrir le dossier des &données",         "&Datenordner öffnen"],
        ["Exit"] =           ["E&xit",                       "&Salir",                            "&Quitter",                               "&Beenden"],
        ["Edit"] =           ["&Edit",                       "&Edición",                          "É&dition",                               "&Bearbeiten"],
        ["FindNote"] =       ["&Find Note",                  "&Buscar nota",                      "&Rechercher une note",                   "Notiz &suchen"],
        ["InsertDateTime"] = ["Insert &Date/Time",           "Insertar &fecha/hora",              "Insérer la &date/l'heure",               "&Datum/Uhrzeit einfügen"],
        ["View"] =           ["&View",                       "&Ver",                              "&Affichage",                             "&Ansicht"],
        ["WordWrap"] =       ["&Word Wrap",                  "&Ajuste de línea",                  "&Retour automatique à la ligne",         "&Zeilenumbruch"],
        ["Toolbar"] =        ["&Toolbar",                    "&Barra de herramientas",            "Barre d'&outils",                        "&Symbolleiste"],
        ["StatusBar"] =      ["&Status Bar",                 "Barra de &estado",                  "Barre d'é&tat",                          "S&tatusleiste"],
        ["ZoomIn"] =         ["Zoom &In",                    "&Acercar",                          "Zoom &avant",                            "&Vergrößern"],
        ["ZoomOut"] =        ["Zoom &Out",                   "A&lejar",                           "Zoom a&rrière",                          "Ver&kleinern"],
        ["ZoomReset"] =      ["&Restore Default Zoom",       "&Restablecer zoom",                 "&Rétablir le zoom",                      "Zoom &zurücksetzen"],
        ["Tools"] =          ["&Tools",                      "&Herramientas",                     "&Outils",                                "E&xtras"],
        ["Options"] =        ["&Options...",                 "&Opciones...",                      "&Options...",                            "&Optionen..."],
        ["Help"] =           ["&Help",                       "Ay&uda",                            "&Aide",                                  "&Hilfe"],
        ["About"] =          ["&About ePad...",              "&Acerca de ePad...",                "À &propos d'ePad...",                    "&Über ePad..."],

        // Toolbar tooltips
        ["NewTip"] =         ["New note (Ctrl+N)",           "Nueva nota (Ctrl+N)",               "Nouvelle note (Ctrl+N)",                 "Neue Notiz (Strg+N)"],
        ["SaveTip"] =        ["Save (Ctrl+S)",               "Guardar (Ctrl+S)",                  "Enregistrer (Ctrl+S)",                   "Speichern (Strg+S)"],
        ["DeleteTip"] =      ["Delete note (Ctrl+Shift+D)",  "Eliminar nota (Ctrl+Mayús+D)",      "Supprimer la note (Ctrl+Maj+D)",         "Notiz löschen (Strg+Umschalt+D)"],

        // Main window
        ["SearchPlaceholder"] = ["Search notes...",          "Buscar notas...",                   "Rechercher dans les notes...",           "Notizen durchsuchen..."],
        ["EmptyNote"] =      ["(empty note)",                "(nota vacía)",                      "(note vide)",                            "(leere Notiz)"],
        ["Editing"] =        ["Editing...",                  "Editando...",                       "Modification...",                        "Bearbeitung..."],
        ["UnsavedChanges"] = ["Unsaved changes",             "Cambios sin guardar",               "Modifications non enregistrées",         "Ungespeicherte Änderungen"],
        ["LastSaved"] =      ["Last saved {0}",              "Guardado por última vez: {0}",      "Dernier enregistrement : {0}",           "Zuletzt gespeichert: {0}"],
        ["Saved"] =          ["Saved {0}",                   "Guardado {0}",                      "Enregistré à {0}",                       "Gespeichert um {0}"],
        ["SaveFailed"] =     ["Save failed: {0}",            "Error al guardar: {0}",             "Échec de l'enregistrement : {0}",        "Speichern fehlgeschlagen: {0}"],
        ["LinesChars"] =     ["{0} lines, {1} chars",        "{0} líneas, {1} caracteres",        "{0} lignes, {1} caractères",             "{0} Zeilen, {1} Zeichen"],
        ["DeleteTitle"] =    ["Delete Note",                 "Eliminar nota",                     "Supprimer la note",                      "Notiz löschen"],
        ["DeleteConfirm"] =  ["Delete \"{0}\"?",             "¿Eliminar \"{0}\"?",                "Supprimer « {0} » ?",                    "„{0}“ löschen?"],
        ["CannotUndo"] =     ["This cannot be undone.",      "Esta acción no se puede deshacer.", "Cette action est irréversible.",         "Dies kann nicht rückgängig gemacht werden."],
        ["TextFilter"] =     ["Text files (*.txt)|*.txt|All files (*.*)|*.*",
                              "Archivos de texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*",
                              "Fichiers texte (*.txt)|*.txt|Tous les fichiers (*.*)|*.*",
                              "Textdateien (*.txt)|*.txt|Alle Dateien (*.*)|*.*"],
        ["PdfFilter"] =      ["PDF documents (*.pdf)|*.pdf", "Documentos PDF (*.pdf)|*.pdf",      "Documents PDF (*.pdf)|*.pdf",            "PDF-Dokumente (*.pdf)|*.pdf"],
        ["MdFilter"] =       ["Markdown files (*.md)|*.md",  "Archivos Markdown (*.md)|*.md",     "Fichiers Markdown (*.md)|*.md",          "Markdown-Dateien (*.md)|*.md"],
        ["ExportFilter"] =   ["Text file (*.txt)|*.txt|PDF document (*.pdf)|*.pdf|Markdown (*.md)|*.md",
                              "Archivo de texto (*.txt)|*.txt|Documento PDF (*.pdf)|*.pdf|Markdown (*.md)|*.md",
                              "Fichier texte (*.txt)|*.txt|Document PDF (*.pdf)|*.pdf|Markdown (*.md)|*.md",
                              "Textdatei (*.txt)|*.txt|PDF-Dokument (*.pdf)|*.pdf|Markdown (*.md)|*.md"],
        ["Exported"] =       ["Exported to {0}",             "Exportado a {0}",                   "Exporté vers {0}",                       "Exportiert nach {0}"],
        ["ExportFailed"] =   ["Export failed: {0}",          "Error al exportar: {0}",            "Échec de l'exportation : {0}",           "Export fehlgeschlagen: {0}"],
        ["RestartPrompt"] =  ["Theme and language changes take effect after ePad restarts.\n\nRestart now?",
                              "Los cambios de tema e idioma se aplican al reiniciar ePad.\n\n¿Reiniciar ahora?",
                              "Les changements de thème et de langue prennent effet au redémarrage d'ePad.\n\nRedémarrer maintenant ?",
                              "Design- und Sprachänderungen werden nach einem Neustart von ePad wirksam.\n\nJetzt neu starten?"],

        // About
        ["AboutTitle"] =     ["About {0}",                   "Acerca de {0}",                     "À propos de {0}",                        "Über {0}"],
        ["Version"] =        ["Version {0}",                 "Versión {0}",                       "Version {0}",                            "Version {0}"],
        ["AppDescription"] = ["Quick plain-text notes, stored in SQLite.",
                              "Notas rápidas de texto plano, guardadas en SQLite.",
                              "Notes rapides en texte brut, stockées dans SQLite.",
                              "Schnelle Notizen als reiner Text, gespeichert in SQLite."],

        // Dialog buttons
        ["OK"] =             ["OK",                          "Aceptar",                           "OK",                                     "OK"],
        ["Cancel"] =         ["Cancel",                      "Cancelar",                          "Annuler",                                "Abbrechen"],
        ["Apply"] =          ["&Apply",                      "A&plicar",                          "A&ppliquer",                             "Ü&bernehmen"],
        ["ResetDefaults"] =  ["&Reset to Defaults",          "&Restablecer valores",              "&Rétablir les valeurs par défaut",       "&Standardwerte"],

        // Options dialog
        ["OptionsTitle"] =   ["Options",                     "Opciones",                          "Options",                                "Optionen"],
        ["TabEditor"] =      ["Editor",                      "Editor",                            "Éditeur",                                "Editor"],
        ["TabAppearance"] =  ["Appearance",                  "Apariencia",                        "Apparence",                              "Darstellung"],
        ["TabGeneral"] =     ["General",                     "General",                           "Général",                                "Allgemein"],
        ["Font"] =           ["Font:",                       "Fuente:",                           "Police :",                               "Schriftart:"],
        ["MonospaceOnly"] =  ["Show monospaced fonts only",  "Mostrar solo fuentes monoespaciadas", "Afficher uniquement les polices à chasse fixe", "Nur Festbreitenschriften anzeigen"],
        ["FontSize"] =       ["Size:",                       "Tamaño:",                           "Taille :",                               "Größe:"],
        ["TabWidth"] =       ["Tab width (spaces):",         "Ancho de tabulación (espacios):",   "Largeur de tabulation (espaces) :",      "Tabulatorbreite (Leerzeichen):"],
        ["WrapLines"] =      ["Wrap long lines",             "Ajustar líneas largas",             "Renvoyer les longues lignes à la ligne", "Lange Zeilen umbrechen"],
        ["Theme"] =          ["Theme:",                      "Tema:",                             "Thème :",                                "Design:"],
        ["UseWindows"] =     ["Use Windows setting",         "Usar configuración de Windows",     "Utiliser le paramètre Windows",          "Windows-Einstellung verwenden"],
        ["ThemeLight"] =     ["Light",                       "Claro",                             "Clair",                                  "Hell"],
        ["ThemeDark"] =      ["Dark",                        "Oscuro",                            "Sombre",                                 "Dunkel"],
        ["RequiresRestart"] = ["Takes effect after restarting ePad.", "Se aplica al reiniciar ePad.", "Prend effet au redémarrage d'ePad.",     "Wirksam nach Neustart von ePad."],
        ["CustomColors"] =   ["Use custom editor colors",    "Usar colores personalizados en el editor", "Couleurs personnalisées pour l'éditeur", "Eigene Editorfarben verwenden"],
        ["TextColor"] =      ["Text color:",                 "Color del texto:",                  "Couleur du texte :",                     "Textfarbe:"],
        ["BackColor"] =      ["Background color:",           "Color de fondo:",                   "Couleur d'arrière-plan :",               "Hintergrundfarbe:"],
        ["ShowToolbar"] =    ["Show toolbar",                "Mostrar barra de herramientas",     "Afficher la barre d'outils",             "Symbolleiste anzeigen"],
        ["ShowStatusBar"] =  ["Show status bar",             "Mostrar barra de estado",           "Afficher la barre d'état",               "Statusleiste anzeigen"],
        ["Language"] =       ["Language:",                   "Idioma:",                           "Langue :",                               "Sprache:"],
        ["DateFormat"] =     ["Date/time format (F5):",      "Formato de fecha/hora (F5):",       "Format de date/heure (F5) :",            "Datums-/Zeitformat (F5):"],
        ["DateExample"] =    ["Example: {0}",                "Ejemplo: {0}",                      "Exemple : {0}",                          "Beispiel: {0}"],
        ["InvalidFormat"] =  ["Invalid format",              "Formato no válido",                 "Format non valide",                      "Ungültiges Format"],
        ["AutoSave"] =       ["Save automatically when I stop typing", "Guardar automáticamente al dejar de escribir", "Enregistrer automatiquement après la saisie", "Nach dem Tippen automatisch speichern"],
        ["AutoSaveDelay"] =  ["Delay (seconds):",            "Retraso (segundos):",               "Délai (secondes) :",                     "Verzögerung (Sekunden):"],
        ["ConfirmDelete"] =  ["Ask before deleting a note",  "Preguntar antes de eliminar una nota", "Demander avant de supprimer une note", "Vor dem Löschen einer Notiz nachfragen"],
        ["RememberWindow"] = ["Remember window size and position", "Recordar tamaño y posición de la ventana", "Mémoriser la taille et la position de la fenêtre", "Fenstergröße und -position merken"],
        ["Preview"] =        ["Preview:",                    "Vista previa:",                     "Aperçu :",                               "Vorschau:"],
        ["PreviewText"] =    ["The quick brown fox jumps over the lazy dog.\r\n0123456789  !@#$%^&*()  {}[]<>\r\n\tThis line starts with a tab."],
    };
}

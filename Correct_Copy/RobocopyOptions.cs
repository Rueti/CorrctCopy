using System.ComponentModel;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Correct_Copy;

public sealed class CopyOption : INotifyPropertyChanged
{
    public string Switch { get; }
    public string Description { get; }
    public string Group { get; }
    public string Hint { get; }
    public bool HasValue => Hint.Length > 0;
    private bool enabled;
    private string value;
    public bool Enabled { get => enabled; set { enabled = value; PropertyChanged?.Invoke(this, new(nameof(Enabled))); } }
    public string Value { get => value; set { this.value = value; PropertyChanged?.Invoke(this, new(nameof(Value))); } }
    public event PropertyChangedEventHandler? PropertyChanged;
    public CopyOption(string option, string description, string group, string value = "", string hint = "", bool enabled = false)
    {
        Switch = option; Description = description; Group = group; this.value = value; Hint = hint; this.enabled = enabled;
    }
}

public static class RobocopyOptions
{
    public static List<CopyOption> Create() =>
    [
        new("S", "Unterordner ohne leere Ordner", "Ordner"),
        new("E", "Unterordner inklusive leerer Ordner", "Ordner"),
        new("LEV", "Maximale Ordnertiefe", "Ordner", "1", "Ganze Zahl ab 1"),
        new("XJ", "Verknüpfungspunkte ausschließen", "Ordner"),
        new("Z", "Unterbrochene Dateien fortsetzen", "Kopieren"),
        new("B", "Backupmodus (benötigt passende Rechte)", "Kopieren"),
        new("ZB", "Fortsetzen, bei Zugriffsfehler Backupmodus", "Kopieren"),
        new("J", "Ungepuffert kopieren, für große Dateien", "Kopieren"),
        new("EFSRAW", "Verschlüsselte Dateien im EFS-RAW-Modus", "Kopieren"),
        new("COPY", "Dateieigenschaften", "Eigenschaften", "DAT", "D Daten · A Attribute · T Zeit · S Rechte · O Besitzer · U Überwachung · X ohne alternative Datenströme"),
        new("DCOPY", "Ordnereigenschaften", "Eigenschaften", "DA", "D Daten · A Attribute · T Zeit · E erweiterte Attribute · X ohne alternative Datenströme"),
        new("SEC", "Daten, Attribute, Zeiten und Zugriffsrechte", "Eigenschaften"),
        new("COPYALL", "Alle Dateieigenschaften inklusive Besitzer", "Eigenschaften"),
        new("NOCOPY", "Keine Dateiinformationen kopieren", "Eigenschaften"),
        new("SECFIX", "Rechte auch bei übersprungenen Dateien korrigieren", "Eigenschaften"),
        new("TIMFIX", "Zeiten auch bei übersprungenen Dateien korrigieren", "Eigenschaften"),
        new("A+", "Dateiattribute hinzufügen", "Eigenschaften", "A", "RASHCNET"),
        new("A-", "Dateiattribute entfernen", "Eigenschaften", "R", "RASHCNETO"),
        new("XO", "Ältere Quelldateien überspringen", "Auswahl"),
        new("XN", "Neuere Quelldateien überspringen", "Auswahl"),
        new("XC", "Gleiche Zeit, andere Größe überspringen", "Auswahl"),
        new("IS", "Identische Dateien erneut kopieren", "Auswahl"),
        new("FFT", "Dateizeiten mit zwei Sekunden Toleranz", "Auswahl"),
        new("MT", "Parallele Kopierthreads", "Leistung", "8", "Leer = 8, sonst 1 bis 128; nicht mit IPG oder EFSRAW"),
        new("IPG", "Pause zwischen Netzwerkpaketen (ms)", "Leistung", "10", "Ganze Zahl ab 0; nicht mit MT"),
        new("R", "Wiederholungen bei Fehlern", "Leistung", "3", "Ganze Zahl ab 0", true),
        new("W", "Wartezeit zwischen Wiederholungen (s)", "Leistung", "2", "Ganze Zahl ab 0", true),
        new("V", "Ausführliche Ausgabe", "Protokoll"),
        new("FP", "Vollständige Dateipfade ausgeben", "Protokoll"),
        new("TS", "Dateizeiten ausgeben", "Protokoll"),
        new("MIR", "Spiegeln – löscht zusätzliche Dateien im Ziel", "Löschen und Verschieben"),
        new("PURGE", "Zusätzliche Dateien und Ordner im Ziel löschen", "Löschen und Verschieben"),
        new("MOV", "Dateien verschieben – löscht sie aus der Quelle", "Löschen und Verschieben"),
        new("MOVE", "Dateien und Ordner verschieben – löscht Quelle", "Löschen und Verschieben"),
        new("CREATE", "Nur Ordnerstruktur und leere Dateien erstellen", "Kopieren"),
        new("FAT", "Zieldateien mit kurzen 8.3-Dateinamen erstellen", "Kopieren"),
        new("256", "Unterstützung für lange Pfade deaktivieren", "Kopieren"),
        new("SJ", "Verbindungspunkte als Verbindungen kopieren", "Ordner"),
        new("SL", "Symbolische Links als Links kopieren", "Ordner"),
        new("XJD", "Verzeichnislinks und Verbindungspunkte ausschließen", "Ordner"),
        new("XJF", "Dateilinks ausschließen", "Ordner"),
        new("NODCOPY", "Keine Ordnereigenschaften kopieren", "Eigenschaften"),
        new("NOOFFLOAD", "Windows Copy Offload deaktivieren", "Leistung"),
        new("COMPRESS", "Netzwerkkomprimierung anfordern", "Leistung"),
        new("MON", "Quelle überwachen: Schwelle für Änderungen", "Überwachung und Zeitfenster", "1", "Ganze Zahl ab 1; läuft bis zum Abbruch"),
        new("MOT", "Quelle überwachen: Intervall in Minuten", "Überwachung und Zeitfenster", "5", "Ganze Zahl ab 1; läuft bis zum Abbruch"),
        new("RH", "Kopieren nur in diesem Zeitfenster", "Überwachung und Zeitfenster", "0800-1800", "hhmm-hhmm, z. B. 2200-0600; kann bis zum Zeitfenster warten"),
        new("PF", "Zeitfenster vor jeder Datei prüfen", "Überwachung und Zeitfenster"),
        new("A", "Nur Dateien mit Archiv-Attribut kopieren", "Auswahl"),
        new("M", "Archivdateien kopieren und Archiv-Attribut zurücksetzen", "Auswahl"),
        new("IA", "Dateien mit diesen Attributen einschließen", "Auswahl", "A", "RASHCNETO"),
        new("XA", "Dateien mit diesen Attributen ausschließen", "Auswahl", "H", "RASHCNETO"),
        new("XF", "Dateien ausschließen", "Auswahl", "*.tmp; *.bak", "Namen, Pfade oder Muster; Einträge mit Semikolon oder Zeilenumbruch trennen"),
        new("XD", "Verzeichnisse ausschließen", "Auswahl", "", "Namen oder Pfade; Einträge mit Semikolon oder Zeilenumbruch trennen"),
        new("XX", "Zusätzliche Dateien und Ordner im Ziel ausschließen", "Auswahl"),
        new("XL", "Nur in der Quelle vorhandene Dateien ausschließen", "Auswahl"),
        new("IT", "Dateien mit abweichenden Attributen einschließen", "Auswahl"),
        new("IM", "Dateien mit abweichender Änderungszeit einschließen", "Auswahl"),
        new("DST", "Sommerzeit-Unterschied von einer Stunde ausgleichen", "Auswahl"),
        new("MAX", "Maximale Dateigröße in Bytes", "Größe und Alter", "104857600", "Ganze Zahl ab 0 (64 Bit)"),
        new("MIN", "Minimale Dateigröße in Bytes", "Größe und Alter", "0", "Ganze Zahl ab 0 (64 Bit)"),
        new("MAXAGE", "Ältere Dateien ausschließen", "Größe und Alter", "30", "0–1899 Tage oder gültiges Datum JJJJMMTT"),
        new("MINAGE", "Neuere Dateien ausschließen", "Größe und Alter", "1", "0–1899 Tage oder gültiges Datum JJJJMMTT"),
        new("MAXLAD", "Dateien mit älterem letztem Zugriff ausschließen", "Größe und Alter", "30", "0–1899 Tage oder gültiges Datum JJJJMMTT"),
        new("MINLAD", "Dateien mit neuerem letztem Zugriff ausschließen", "Größe und Alter", "1", "0–1899 Tage oder gültiges Datum JJJJMMTT"),
        new("REG", "R/W als Windows-Standardeinstellungen speichern", "Wiederholungen"),
        new("TBD", "Bei noch nicht verfügbarer Netzwerkfreigabe warten", "Wiederholungen"),
        new("LFSM", "Bei wenig freiem Zielspeicher pausieren", "Leistung", "", "Leer = 10 % frei halten; sonst Bytes oder z. B. 10G; nicht mit MT, EFSRAW, B, ZB"),
        new("L", "Testlauf: keine Dateien kopieren oder löschen", "Protokoll"),
        new("X", "Alle zusätzlichen Dateien melden", "Protokoll"),
        new("BYTES", "Dateigrößen in Bytes ausgeben", "Protokoll"),
        new("NS", "Keine Dateigrößen protokollieren", "Protokoll"),
        new("NC", "Keine Dateiklassen protokollieren", "Protokoll"),
        new("NFL", "Keine Dateinamen protokollieren", "Protokoll"),
        new("NDL", "Keine Verzeichnisnamen protokollieren", "Protokoll"),
        new("NP", "Prozentanzeige unterdrücken", "Protokoll"),
        new("ETA", "Geschätzte verbleibende Zeit anzeigen", "Protokoll"),
        new("LOG", "Protokolldatei schreiben (überschreibt)", "Protokolldateien", "", "Absoluter Dateipfad; wird auch beim Testlauf geschrieben"),
        new("LOG+", "An Protokolldatei anhängen", "Protokolldateien", "", "Absoluter Dateipfad; wird auch beim Testlauf geschrieben"),
        new("UNILOG", "Unicode-Protokoll schreiben (überschreibt)", "Protokolldateien", "", "Absoluter Dateipfad; wird auch beim Testlauf geschrieben"),
        new("UNILOG+", "An Unicode-Protokoll anhängen", "Protokolldateien", "", "Absoluter Dateipfad; wird auch beim Testlauf geschrieben"),
        new("TEE", "Ausgabe zusätzlich zum Protokoll im Fenster zeigen", "Protokolldateien"),
        new("NJH", "Auftragskopf ausblenden", "Protokoll"),
        new("NJS", "Auftragszusammenfassung ausblenden", "Protokoll"),
        new("UNICODE", "Konsolenausgabe in Unicode", "Protokoll", enabled: true),
        new("JOB", "Parameter aus Robocopy-Auftragsdatei laden", "Aufträge", "", "Absoluter Pfad zur .rcj-Datei; kann weitere Kopier-, Lösch- und Schreiboptionen enthalten"),
        new("SAVE", "Parameter als Robocopy-Auftrag speichern", "Aufträge", "", "Absoluter Pfad zur .rcj-Datei; mit QUIT nur speichern, auch im Testlauf"),
        new("QUIT", "Nur Parameter verarbeiten, dann beenden", "Aufträge"),
        new("NOSD", "Kein Quellverzeichnis in der Befehlszeile", "Aufträge"),
        new("NODD", "Kein Zielverzeichnis in der Befehlszeile", "Aufträge"),
        new("IF", "Zusätzliche Dateien einschließen", "Aufträge", "", "Dateinamen oder Muster; Einträge mit Semikolon oder Zeilenumbruch trennen")
    ];

    public static string[] SplitEntries(string value, bool fileNamesOnly = false)
    {
        var entries = value.Split([';', '\r', '\n'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (entries.Length == 0 || entries.Any(e => e.StartsWith('/') || e.Contains('"') || e.Any(char.IsControl) ||
            (fileNamesOnly && e.IndexOfAny(['/', '\\', ':']) >= 0)))
            throw new ArgumentException("Bitte Namen oder Muster ohne Anführungszeichen eingeben; mehrere Einträge mit Semikolon trennen.");
        return entries;
    }

    public static List<string> Build(string source, string destination, string filter, IEnumerable<CopyOption> options, bool preview)
    {
        var selected = options.Where(o => o.Enabled).ToDictionary(o => o.Switch);
        bool Has(string name) => selected.ContainsKey(name);
        bool noSource = Has("NOSD"), noDestination = Has("NODD");
        bool job = Has("JOB"), quit = Has("QUIT");
        if ((noSource || noDestination) && !job && !quit)
            throw new ArgumentException("/NOSD und /NODD benötigen /JOB oder /QUIT.");
        string Normalize(string path, string label, bool omitted)
        {
            if (omitted) return "";
            if (string.IsNullOrWhiteSpace(path) && job) return "";
            if (!Path.IsPathFullyQualified(path) || path.Contains('"') || path.Any(char.IsControl))
                throw new ArgumentException($"Bitte einen absoluten Pfad für {label} auswählen.");
            return Path.GetFullPath(path);
        }
        source = Normalize(source, "Quelle", noSource);
        destination = Normalize(destination, "Ziel", noDestination);
        if (source.Length > 0 && destination.Length > 0)
        {
            var src = source.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var dst = destination.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (src.StartsWith(dst, StringComparison.OrdinalIgnoreCase) || dst.StartsWith(src, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Quelle und Ziel dürfen weder identisch noch ineinander verschachtelt sein.");
        }
        void Exclusive(params string[] names)
        {
            if (names.Count(Has) > 1) throw new ArgumentException("Bitte nur eine dieser Optionen wählen: /" + string.Join(", /", names));
        }
        Exclusive("S", "E", "MIR"); Exclusive("Z", "B", "ZB");
        Exclusive("COPY", "SEC", "COPYALL", "NOCOPY"); Exclusive("MOV", "MOVE", "MIR");
        Exclusive("MT", "IPG"); Exclusive("MT", "EFSRAW");
        Exclusive("DCOPY", "NODCOPY"); Exclusive("A", "M");
        Exclusive("LOG", "LOG+", "UNILOG", "UNILOG+");
        foreach (var conflict in new[] { "MT", "EFSRAW", "B", "ZB" }) Exclusive("LFSM", conflict);
        if (Has("PF") && !Has("RH") && !job) throw new ArgumentException("/PF benötigt ein Zeitfenster (/RH).");
        if (Has("SECFIX") && !Has("SEC") && !Has("COPYALL") && !job &&
            !(selected.TryGetValue("COPY", out var copy) && copy.Value.ToUpperInvariant().IndexOfAny(['S', 'O', 'U']) >= 0))
            throw new ArgumentException("/SECFIX benötigt /SEC, /COPYALL oder /COPY mit S, O oder U.");
        var args = new List<string>();
        // Robocopy can print job-loading diagnostics before processing later switches.
        if (Has("UNICODE")) args.Add("/UNICODE");
        // Explicit omission flags keep a destination from being interpreted as a source.
        if (source.Length > 0) args.Add(source); else args.Add("/NOSD");
        if (destination.Length > 0) args.Add(destination); else args.Add("/NODD");
        if (!string.IsNullOrWhiteSpace(filter)) args.AddRange(SplitEntries(filter, true));
        // Load job settings before the explicit GUI options and /L.
        foreach (var o in selected.Values.OrderBy(o => o.Switch == "JOB" ? 0 : 1))
        {
            if (o.Switch is "NOSD" or "NODD" or "L" or "UNICODE") continue;
            var value = o.Value.Trim();
            if (!o.HasValue) { args.Add("/" + o.Switch); continue; }
            if (o.Switch is "XF" or "XD" or "IF")
            {
                args.Add("/" + o.Switch); args.AddRange(SplitEntries(value, o.Switch == "IF")); continue;
            }
            if (o.Switch is "LOG" or "LOG+" or "UNILOG" or "UNILOG+" or "JOB" or "SAVE")
            {
                if (!Path.IsPathFullyQualified(value) || value.IndexOfAny(['"', '*', '?']) >= 0 || value.Any(char.IsControl) ||
                    Path.EndsInDirectorySeparator(value))
                    throw new ArgumentException($"/{o.Switch}: Bitte einen absoluten Dateipfad ohne Anführungszeichen eingeben.");
                value = Path.GetFullPath(value);
            }
            else
            {
                value = value.ToUpperInvariant();
                string? allowed = o.Switch switch { "COPY" => "DATSOUX", "DCOPY" => "DATEX", "A+" => "RASHCNET", "A-" or "IA" or "XA" => "RASHCNETO", _ => null };
                bool valid;
                if (allowed != null) valid = value.Length > 0 && value.All(allowed.Contains);
                else if (o.Switch == "MT" && value.Length == 0) valid = true;
                else if (o.Switch == "RH") valid = Regex.IsMatch(value, @"^([01][0-9]|2[0-3])[0-5][0-9]-([01][0-9]|2[0-3])[0-5][0-9]$");
                else if (o.Switch == "LFSM")
                {
                    valid = value.Length == 0 || (Regex.IsMatch(value, @"^[0-9]+[KMG]?$") &&
                        ulong.TryParse(value.TrimEnd('K', 'M', 'G'), out var size) && size > 0 &&
                        size <= long.MaxValue / (value.EndsWith('G') ? 1073741824UL : value.EndsWith('M') ? 1048576UL : value.EndsWith('K') ? 1024UL : 1UL));
                }
                else if (o.Switch is "MAXAGE" or "MINAGE" or "MAXLAD" or "MINLAD")
                    valid = (uint.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var days) && days < 1900) ||
                        DateTime.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
                else
                {
                    valid = long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number) &&
                        number >= (o.Switch is "LEV" or "MT" or "MON" or "MOT" ? 1 : 0) &&
                        number <= (o.Switch == "MT" ? 128 : o.Switch is "MAX" or "MIN" ? long.MaxValue : int.MaxValue);
                }
                if (!valid) throw new ArgumentException($"/{o.Switch}: {o.Hint}");
            }
            args.Add("/" + o.Switch + (value.Length > 0 ? ":" + value : ""));
        }
        if (Has("MIN") && Has("MAX") && long.Parse(selected["MIN"].Value) > long.Parse(selected["MAX"].Value))
            throw new ArgumentException("/MIN darf nicht größer als /MAX sein.");
        if (preview || Has("L")) args.Add("/L");
        return args;
    }

    public static string DescribeEffects(IEnumerable<CopyOption> options)
    {
        var selected = options.Where(o => o.Enabled).Select(o => o.Switch).ToHashSet();
        var notes = new List<string>();
        if (selected.Overlaps(["MIR", "PURGE", "MOV", "MOVE"])) notes.Add("Enthält Lösch- oder Verschiebeoptionen.");
        if (selected.Contains("JOB")) notes.Add("Die Auftragsdatei kann zusätzliche Optionen und Pfade enthalten; diese sind nicht in den Checkboxen abgebildet. Enthält sie Quelle/Ziel, die entsprechenden Felder leer lassen oder /NOSD und /NODD aktivieren.");
        if (selected.Contains("NOSD")) notes.Add("Das Quellfeld wird wegen /NOSD nicht übergeben.");
        if (selected.Contains("NODD")) notes.Add("Das Zielfeld wird wegen /NODD nicht übergeben.");
        if (selected.Overlaps(["REG", "SAVE", "LOG", "LOG+", "UNILOG", "UNILOG+"])) notes.Add("Registry-, Auftrags- und Protokollausgaben werden auch beim Testlauf geschrieben.");
        if (selected.Overlaps(["MON", "MOT", "RH", "LFSM", "TBD"])) notes.Add("Der Auftrag kann überwachen oder warten; mit Abbrechen beenden.");
        if (selected.Overlaps(["LOG", "LOG+", "UNILOG", "UNILOG+"]) && !selected.Contains("TEE")) notes.Add("Für die Ausgabe auch im Fenster /TEE aktivieren.");
        return string.Join(" ", notes);
    }
    // Windows command-line quoting, including trailing backslashes in drive roots.
    public static string Quote(string value) => "\"" + Regex.Replace(value, "(\\\\*)\"", "$1$1\\\"").TrimEnd('\\') +
        new string('\\', value.Length - value.TrimEnd('\\').Length) + new string('\\', value.Length - value.TrimEnd('\\').Length) + "\"";
}

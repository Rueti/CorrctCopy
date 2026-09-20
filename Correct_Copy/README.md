# Correct Copy

Eine deutschsprachige Windows-Oberfläche für Robocopy auf Basis von WPF und .NET 8.

## Verwendung

1. Quellordner oder einzelne Datei auswählen, anschließend den Zielordner angeben.
2. Bei Bedarf Dateinamen oder Muster wie `*.pdf; *.docx; Mein Dokument.txt` eingeben. Einträge werden durch Semikolon getrennt; Leerzeichen innerhalb eines Eintrags bleiben erhalten. Anführungszeichen sind nicht erforderlich.
3. Eine Voreinstellung wählen oder unter **Optionen auswählen** die gewünschten Checkboxen aktivieren. Parameter wie `/MT`, `/R`, `/W` oder `/COPY` haben eigene Wertefelder und Tooltips.
4. Die Befehlsvorschau prüfen. **Testlauf** ist zunächst aktiviert und ergänzt `/L`: Robocopy zeigt die geplanten Aktionen an, ohne Dateien zu verändern.
5. Für den tatsächlichen Auftrag **Testlauf** deaktivieren und **Kopieren starten** wählen. Die Ausgabe erscheint im Fenster. **Abbrechen** beendet Robocopy; bereits erfolgte Änderungen werden nicht rückgängig gemacht.

Alle 91 unterschiedlichen Parameter der bereitgestellten Windows-Robocopy-Hilfe sind als Checkboxen nach Themen gruppiert. Das Suchfeld filtert nach Parameter (z. B. `/LFSM`), Beschreibung oder Gruppe. Die Voreinstellungen ersetzen die aktive Optionsauswahl; Werte und Testlauf-Einstellung bleiben erhalten. Standardmäßig gelten drei Wiederholungen mit zwei Sekunden Wartezeit, Unicode-Ausgabe und sichtbarer Dateifortschritt. Mit /NP kann die Prozentanzeige ausdrücklich ausgeschaltet werden. Der Fortschrittsbalken bezieht sich auf die zuletzt gemeldete Datei, nicht auf den gesamten Auftrag. Eine sichtbare Zieldatei ist noch kein Abschlussnachweis; maßgeblich ist das Ende von Robocopy. Quelle und Ziel dürfen nicht identisch oder ineinander verschachtelt sein; diese Prüfung vergleicht Pfade und löst keine Verknüpfungen oder Netzwerk-Aliasse auf.

Die Eingabefelder unterstützen Attributflags, Dateigrößen über 2 GB, Alter in Tagen oder als Kalenderdatum, Zeitfenster über Mitternacht und absolute Protokoll-/Auftragspfade. `/XF`, `/XD` und `/IF` nehmen mehrere Einträge mit Semikolon oder Zeilenumbruch an. Semikolons innerhalb eines Dateinamens sind in diesen Listen nicht unterstützt. `/MT` ohne Wert nutzt Robocopys Standardzahl von Threads. `/LFSM` ohne Wert verwendet die Standardreserve; alternativ sind Angaben wie `10G` möglich. Ungültige Werte und bekannte Konflikte, einschließlich `/LFSM` mit `/MT`, `/EFSRAW`, `/B` oder `/ZB`, werden vor dem Start abgefangen.

`/MON` und `/MOT` überwachen die Quelle bis zum Abbruch. `/RH`, `/TBD` und `/LFSM` können dazu führen, dass Robocopy wartet. Die Anwendung bleibt währenddessen bedienbar.

## Protokolle und Aufträge

- `/LOG`, `/LOG+`, `/UNILOG` und `/UNILOG+` benötigen einen absoluten Dateipfad. Mit `/TEE` erscheint die Ausgabe zusätzlich im Fenster.
- `/SAVE` speichert eine Robocopy-Auftragsdatei. Mit `/QUIT` werden nur Parameter verarbeitet, ohne zu kopieren. Für eine Vorlage ohne feste Verzeichnisse zusätzlich `/NOSD` und `/NODD` wählen.
- `/JOB` lädt eine vorhandene `.rcj`-Datei. Enthält diese bereits Quell-/Zielverzeichnisse, die entsprechenden Felder leer lassen oder `/NOSD` bzw. `/NODD` aktivieren; doppelt angegebene Pfade lehnt Robocopy ab. Vorlagen ohne Pfade können mit den Verzeichnisfeldern ergänzt werden.
- Geladene Auftragsoptionen werden von Robocopy verarbeitet, nicht in die Checkboxen importiert. Die Vorschau zeigt den `/JOB`-Verweis. Nicht angekreuzte Optionen entfernen keine bereits in der Datei enthaltenen Einstellungen, beispielsweise `/L`. Die GUI-Prüfung erfasst nicht die zusätzlichen Parameter oder Pfade aus Auftragsdateien; deren Auswertung übernimmt Robocopy. `/L` wird für einen expliziten Testlauf am Ende ergänzt.
- **Auch ein Testlauf kann Protokolle und Auftragsdateien schreiben; `/REG` kann die Robocopy-Standardeinstellungen in der Registry ändern.** Die Oberfläche zeigt hierfür einen Hinweis. Das Laden von Aufträgen, `/REG`, `/SAVE` und das Überschreiben von Protokollen erfordern vor dem Start eine Bestätigung.

`/MIR`, `/PURGE`, `/MOV` und `/MOVE` können Dateien löschen. Vor einem tatsächlichen Lauf mit diesen Optionen zeigt die Anwendung den konkreten Befehl zur Bestätigung. Backupmodus und bestimmte Sicherheitseigenschaften benötigen entsprechende Windows-Rechte.

Robocopy-Codes ab 8 werden als Fehler angezeigt; Codes von 2 bis 7 als Abschluss mit Hinweisen. Die Oberfläche zeigt die letzten ca. 100.000–200.000 Zeichen der Ausgabe und verarbeitet Robocopys gemischte OEM-/Unicode-Ausgabe. Robocopy läuft direkt ohne Shell, mit separat übergebenen Argumenten. Die Befehlsvorschau kann kopiert werden.

Referenz: [Microsofts Robocopy-Dokumentation](https://learn.microsoft.com/en-us/windows-server/administration/windows-commands/robocopy).

## Bauen und prüfen

Voraussetzungen: Windows, .NET SDK mit WPF-Unterstützung und .NET 8 Desktop Runtime.

```powershell
dotnet build Correct_Copy.csproj
pwsh -NoProfile -File ./Verify.ps1
```

`Verify.ps1` nutzt die lokal zwischengespeicherten NuGet-Pakete. Die 85 Prüfungen decken Katalogvollständigkeit, Eingabevalidierung, gemischte Ausgabekodierung, beide WPF-Fenster einschließlich gerenderter Optionsliste und Öffnen per Schaltfläche, Suche, Testlauf-Umschaltung, Dateifortschritt sowie den vollständigen Kopierlauf über die Oberfläche bis zur Freigabe der Schaltflächen ab. Robocopy läuft ausschließlich mit neu erzeugten temporären Testdateien: Spiegel-Testlauf, normales Kopieren, Ausschlusslisten, Protokolle, Auftragsdateien und Vorlagen. Registry-Änderungen und dauerhafte Überwachungsaufträge werden nicht ausgeführt. Testprojekte und Testdateien bleiben zur Nachprüfung im ausgegebenen temporären Verzeichnis. Start der gebauten Anwendung: `bin\Debug\net8.0-windows7.0\Correct_Copy.exe`.

# KeyFix

KeyFix ist eine Windows-Tray-App, die erkennt, wenn wahrscheinlich mit dem falschen Tastaturlayout getippt wird. Die App kann einen Hinweis abspielen, eine Benachrichtigung anzeigen, die Eingabesprache wechseln und das falsch getippte Wort nach dem Drücken der Leertaste korrigieren.

Languages: [English](README.md) | [فارسی](README.fa.md) | [العربية](README.ar.md) | [Deutsch](README.de.md)

## Unterstützte Tastaturlayouts

- Englisch
- Persisch
- Arabisch
- Deutsch

Wichtig: Öffne nach der Installation die **Settings** und lasse nur die Sprachen aktiviert, die du wirklich verwendest. Deaktiviere alle anderen Sprachen. Das verbessert die Erkennungsgenauigkeit und reduziert unnötige Korrekturen.

## Funktionen

- Windows-System-Tray-App
- Einstellungsfenster
- Sprachen einzeln aktivieren oder deaktivieren
- Modi für Warnung, Vorschlag und automatischen Sprachwechsel
- Automatische Korrektur des vorherigen falsch getippten Wortes im Modus `AutoSwitch`
- Entscheidung erst nach der Leertaste, nicht während des Tippens eines Wortes
- Optionaler Start mit Windows
- Standard-Warnton von Windows
- Optionaler eigener `.wav`-Warnton
- Windows-Benachrichtigungen
- Konfigurierbare Erkennungsschwelle und Mindestzeichenanzahl
- Ausschlussliste für Anwendungen
- Lokale, datenschutzfreundliche Erkennung
- GitHub-Actions-Build
- Inno-Setup-Skript für den Installer

## Funktionsweise

KeyFix hält nur einen kurzen Textpuffer im Arbeitsspeicher. Wenn die Leertaste gedrückt wird, prüft die App das vorherige Wort gegen die aktivierten Tastaturlayouts. Wenn ein anderes Layout deutlich wahrscheinlicher ist, kann KeyFix das falsche Wort samt Leerzeichen entfernen, den korrigierten Text einfügen und zur passenden Eingabesprache wechseln.

Beispiel:

```text
Gewollt: hello
Aktives Layout: Persisch
Falsch getippt: اثممخ
Korrigiert: hello
```

## Datenschutz

KeyFix ist so gebaut, dass getippter Text nicht gespeichert wird.

- Getippter Text wird nicht auf die Festplatte geschrieben.
- Getippter Text wird nicht hochgeladen.
- Es gibt keine Telemetrie und keinen Remote-Server.
- Es wird nur ein kurzer lokaler Speicherpuffer für die Erkennung genutzt.
- Einstellungen werden in `%APPDATA%\KeyFix\settings.json` gespeichert.
- Der Puffer wird nach Enter, Tab, nicht unterstützten Layouts, ausgeschlossenen Apps und automatischer Korrektur gelöscht.

Mehr dazu steht in [PRIVACY.md](PRIVACY.md).

## Installation

Lade den neuesten Installer von der GitHub-Releases-Seite herunter:

```text
KeyFixSetup-0.4.0.exe
```

Nach der Installation:

1. Starte KeyFix über das Startmenü.
2. Öffne KeyFix über das Windows-Tray.
3. Öffne **Settings**.
4. Aktiviere nur die Sprachen, die du verwendest.
5. Deaktiviere alle nicht verwendeten Sprachen.
6. Wähle, ob KeyFix nur warnen, Vorschläge machen oder automatisch Sprache und Text korrigieren soll.

## Entwicklung

```powershell
.\scripts\build.ps1
```

App starten:

```powershell
dotnet run --project .\src\KeyboardLanguageGuard.App\KeyboardLanguageGuard.App.csproj
```

Installer erstellen:

```powershell
.\scripts\package-installer.ps1
```

## Lizenz

Siehe [LICENSE](LICENSE).

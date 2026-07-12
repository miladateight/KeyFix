<p align="center">
  <img src="assets/keyfix-logo-512.png" alt="KeyFix logo" width="180">
</p>

<h1 align="center">KeyFix</h1>

<p align="center">
  Datenschutzfreundliche Windows-Tray-App, die zwei Arten von Tippfehlern behebt:
  Tippen mit der <b>falschen Tastatursprache</b> und gewöhnliche <b>Rechtschreibfehler</b>.
  Komplett offline und standardmäßig zurückhaltend.
</p>

<p align="center">
  <a href="https://github.com/miladateight/KeyFix/actions/workflows/build.yml"><img src="https://github.com/miladateight/KeyFix/actions/workflows/build.yml/badge.svg" alt="Build status"></a>
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4" alt=".NET 8">
  <img src="https://img.shields.io/badge/platform-Windows%2010%2F11-0078D6" alt="Windows 10/11">
  <a href="https://github.com/miladateight/KeyFix/releases/latest"><img src="https://img.shields.io/github/v/release/miladateight/KeyFix?sort=semver" alt="Latest release"></a>
  <a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-green" alt="MIT license"></a>
</p>

<p align="center">
  <a href="https://ateight.xyz/KeyFix/">Website</a> ·
  <a href="https://github.com/miladateight/KeyFix/releases/latest">Neueste Version</a> ·
  <a href="PRIVACY.md">Datenschutz</a> ·
  <a href="CHANGELOG.md">Änderungen</a>
</p>

<p align="center">
  <b>Sprachen:</b>
  <a href="README.md">English</a> ·
  <a href="README.fa.md">فارسی</a> ·
  <a href="README.ar.md">العربية</a> ·
  <a href="README.de.md">Deutsch</a>
</p>

## Warum KeyFix?

Wenn du beim Tippen zwischen Englisch, Persisch, Arabisch und Deutsch wechselst, vergisst man leicht das aktuelle Tastaturlayout. Du möchtest `hello` schreiben, aber weil das aktive Layout Persisch ist, wird daraus `اثممخ`. KeyFix erkennt solche Fehler vollständig lokal und hilft dir, sie zu korrigieren, bevor sie Teil des Satzes werden.

Zusätzlich kann KeyFix auch gewöhnliche Rechtschreibfehler beheben (wenn das Tastaturlayout bereits korrekt ist). Beide Funktionen sind vollständig getrennt, und die App weiß immer, welche Art von Korrektur sie vorschlägt.

## Unterstützte Sprachen

- Englisch
- Persisch
- Arabisch
- Deutsch

Wichtig: Öffne nach der Installation die **Settings** und lasse nur die Sprachen aktiviert, die du wirklich verwendest. Deaktiviere den Rest. Das verbessert die Erkennungsgenauigkeit und reduziert unnötige Korrekturen.

## Funktionen

- Windows-System-Tray-App mit kompaktem Einstellungsfenster
- Ersteinrichtung zur Auswahl der tatsächlich genutzten Sprachen
- Sprachen einzeln aktivieren oder deaktivieren
- Drei Erkennungsmodi: `AlertOnly`, `AlertAndSuggest` und `AutoSwitch`
- Korrektur des vorherigen Wortes nach der Leertaste (nicht während des Tippens)
- **Rückgängig machen**: direkt nach einer automatischen Korrektur einfach Backspace drücken, um sie umzukehren
- **Lokales Lernen**, das sich an deine akzeptierten und rückgängig gemachten Korrekturen anpasst
- Erkennung auf Basis häufigkeitssortierter Wortlisten für alle vier Sprachen
- Optionale, offline arbeitende Rechtschreibkorrektur (SymSpell-artiger Index), **standardmäßig aus**
- Ein offline arbeitendes Bigram-Kontextmodell, das dem Scoring hilft, das zum Umfeld passende Wort zu bevorzugen (bisher für Englisch)
- Zurückhaltende Entscheidungslogik mit Mehrdeutigkeits-Marge und Regler `Conservative`/`Balanced`/`Aggressive`
- Erkennung geschützter Tokens (URLs, E-Mails, Pfade, Versionen, Code-Bezeichner usw.) gegen Fehlkorrekturen
- Lokales, privates persönliches Wörterbuch mit Import/Export und optionalen Ersetzungspaaren
- Rekonstruktion des persischen Halb-Leerzeichens (ZWNJ) mit einstellbarem Schreibstil (umgangssprachlich/formell)
- Optionales lokales Diagnose-Log (standardmäßig aus; nur Metadaten, nie dein getippter Text)
- Schnelle Unicode-Textersetzung mit abgesichertem Zwischenablage-Fallback
- Optionaler Start mit Windows
- Standard-Warnton von Windows und optionaler eigener `.wav`-Ton
- Windows-Benachrichtigungen
- Ausschlussliste für Terminals, Passwort-Manager und andere sensible Apps
- Rein lokale Erkennung, ohne Telemetrie und ohne Remote-Server

## Zwei Arten von Korrektur

KeyFix trennt zwei Probleme; jedes lässt sich in den **Settings** unabhängig steuern:

| Einstellung | Was sie behebt | Beispiel | Standard |
| --- | --- | --- | --- |
| Tippen mit falscher Tastatursprache korrigieren | Richtige Tasten im falschen Layout getippt | `اثممخ` → `hello` | An |
| Gewöhnliche Rechtschreibfehler korrigieren | Echter Tippfehler bei korrektem Layout | `recieve` → `receive` | **Aus** |

Die automatische Rechtschreibkorrektur ist standardmäßig aus; aktiviere sie (und optional „automatisch anwenden") nur bei Bedarf.

## Erkennungsmodi

| Modus | Verhalten |
| --- | --- |
| `AlertOnly` | Nur Hinweis (Ton/Benachrichtigung), ohne Text zu ändern. |
| `AlertAndSuggest` | Weist hin und schlägt die passendere Sprache/das passendere Wort vor. |
| `AutoSwitch` | Korrigiert das Wort nach der Leertaste und wechselt die Eingabesprache. |

## Funktionsweise

KeyFix hält nur einen kurzen Textpuffer im Arbeitsspeicher. Bei der Leertaste prüft es das vorherige Wort. Ist ein anderes Layout deutlich wahrscheinlicher, kann KeyFix das falsche Wort entfernen, den korrekten Text einfügen und die Eingabesprache wechseln.

Beispiel falsche Sprache:

```text
Gewollt: hello
Aktives Layout: Persisch
Getippt: اثممخ
Korrigiert: hello
```

Rechtschreib-Beispiel (nur bei aktivierter Rechtschreibkorrektur):

```text
Aktives Layout: Englisch
Getippt: recieve
Vorschlag: receive
```

Der Puffer wird nach Enter, Tab, nicht unterstützten Layouts, ausgeschlossenen Apps und automatischer Korrektur gelöscht.

## Rückgängig machen

Automatische Korrekturen sind nicht endgültig. Drücke direkt danach **Backspace**, und KeyFix stellt genau das ursprüngliche Wort wieder her — bei Korrekturen der Tastatursprache auch das vorherige Layout. Das Zeitfenster dafür ist kurz und an dasselbe Fenster und denselben Tipp-Kontext gebunden: Es schließt sich, sobald du etwas anderes tippst, den Fokus wechselst, Enter/Tab drückst oder eine kurze Zeitspanne verstreicht. Das Rückgängigmachen einer Korrektur wird für das lokale Lernen zugleich als Ablehnung gewertet. Dafür wird kein Satz gespeichert — nur die beiden betroffenen Wörter und ein paar kurzlebige Kennungen.

## Lokales Lernen

KeyFix kann lokal daraus lernen, wie du mit Korrekturen umgehst: Das Akzeptieren einer automatischen Korrektur verstärkt sie leicht, das Rückgängigmachen schwächt sie leicht ab — beides innerhalb eines sicheren, begrenzten Bereichs. Eine Korrektur, die du wiederholt rückgängig machst, wird irgendwann nicht mehr automatisch angewendet. Das überschreibt nie die Regeln für geschützte Tokens und erzeugt nie Vertrauen für einen Kandidaten, der die Mehrdeutigkeits-Prüfung ohnehin nicht besteht. Gespeichert werden lokal nur normalisierte Wörter und kleine Zähler — niemals vollständige Sätze — und du kannst jederzeit in den Settings zurücksetzen, was KeyFix gelernt hat (vollständig oder nur für eine Sprache).

## Geschützte Tokens

Um Fehlkorrekturen zu vermeiden, korrigiert KeyFix nichts, was kein gewöhnliches Wort ist. Geschützte Tokens umfassen URLs, E-Mail-Adressen, Dateipfade, Kommandozeilen-Optionen (`--configuration`), Versionsnummern (`v0.7.0`), Domains, Hashtags, Erwähnungen, Code-Bezeichner (`camelCase`, `PascalCase`, `snake_case`, `SCREAMING_SNAKE`), Akronyme, Zahlen, gemischte alphanumerische Tokens und Emojis. Terminals und Passwort-Manager werden zudem über die Ausschlussliste vollständig ausgenommen.

## Persönliches Wörterbuch

Du kannst ein lokales, privates persönliches Wörterbuch mit eigenen Wörtern führen. Hinzugefügte Wörter gelten immer als gültig und werden nie „korrigiert"; optional kannst du Ersetzungspaare definieren (z. B. eine Abkürzung, die zu einer längeren Form expandiert). Das Wörterbuch unterstützt add, remove, list, import (UTF-8-Text) und export und wird lokal gespeichert unter:

```text
%APPDATA%\KeyFix\user-dictionary.json
```

## Korrektur-Eifer

Eine einzige Einstellung „How eager" steuert, wie sicher KeyFix sein muss, bevor es handelt:

| Stufe | Verhalten |
| --- | --- |
| `Conservative` | Korrigiert nur bei sehr hoher, eindeutiger Sicherheit. Standard. |
| `Balanced` | Ausgewogen zwischen Fehlererkennung und Vermeidung von Fehlkorrekturen. |
| `Aggressive` | Korrigiert offensiver; mehr Treffer, etwas mehr Risiko. |

Automatische Korrektur verlangt stets, dass der beste Kandidat die Schwelle überschreitet **und** den zweitbesten mit klarem Abstand schlägt, sodass mehrdeutige Fälle nie automatisch korrigiert werden.

## Persischer Korrekturstil

Die Rekonstruktion des Halb-Leerzeichens (ZWNJ) korrigiert die Worttrennung an bekannten Wortgrenzen — z. B. `میخوام → می‌خوام`, `کتابها → کتاب‌ها`, `خانهام → خانه‌ام` — ohne jemals versehentlich ein gebräuchliches Wort zu zerteilen. Eine Einstellung **Persischer Stil** steuert, ob Verbformen zusätzlich Richtung Hochsprache angeglichen werden:

| Stil | Verhalten |
| --- | --- |
| `PreserveUserStyle` | Korrigiert nur die Worttrennung; dein umgangssprachlicher oder formeller Wortlaut bleibt erhalten. Standard. |
| `Conversational` | Bevorzugt umgangssprachliche Standardformen. |
| `Formal` | Wandelt eine kleine, geprüfte Menge umgangssprachlicher Verbformen in die formelle Form um (z. B. `میخوام → می‌خواهم`). |

## Diagnose-Log

Zur Fehlersuche kann KeyFix optional lokale Log-Dateien schreiben, die nur die *Art* der Entscheidung beschreiben — Wortlänge, erkannte Schrift, Korrekturtyp, Vertrauens- und Mehrdeutigkeits-Kategorien, Verarbeitungsdauer — ohne jemals den Text selbst festzuhalten. Das ist **standardmäßig aus**. Logs rotieren automatisch, sind größenbegrenzt und lassen sich jederzeit in den Settings löschen.

## Installation

Lade den neuesten Installer von der [GitHub-Releases-Seite](https://github.com/miladateight/KeyFix/releases/latest):

```text
KeyFixSetup-0.7.0.exe
```

Nach der Installation:

1. Starte KeyFix über das Startmenü.
2. Öffne es über das Windows-Tray.
3. Öffne **Settings**.
4. Aktiviere nur die Sprachen, die du verwendest.
5. Deaktiviere alle nicht verwendeten Sprachen.
6. Wähle, ob KeyFix nur warnen, vorschlagen oder Sprache und Text automatisch korrigieren soll.

## Datenschutz

KeyFix ist so gebaut, dass getippter Text nicht gespeichert wird.

- Getippter Text wird nicht auf die Festplatte geschrieben.
- Getippter Text wird nicht hochgeladen.
- Es gibt keine Telemetrie, kein Analyse-SDK und keinen Remote-Server.
- Für die Erkennung wird nur ein kurzer lokaler Speicherpuffer genutzt.
- Der Undo-Zustand liegt nur im Arbeitsspeicher und wird bei Verwendung oder Ablauf sofort verworfen.
- Einstellungen werden in `%APPDATA%\KeyFix\settings.json` gespeichert.
- Das persönliche Wörterbuch wird lokal in `%APPDATA%\KeyFix\user-dictionary.json` gespeichert und nie hochgeladen.
- Lokale Lerndaten (nur normalisierte Wörter und Zähler, nie vollständige Sätze) werden in `%APPDATA%\KeyFix\learning.json` gespeichert und nie hochgeladen.
- Das Diagnose-Log ist standardmäßig aus; auch wenn aktiviert, wird nie getippter Text protokolliert.
- Die Standard-Ausschlussliste enthält Passwort-Manager und Terminals.

Mehr dazu in [PRIVACY.md](PRIVACY.md).

## Entwicklungsvoraussetzungen

- Windows 10 oder Windows 11
- .NET 8 SDK
- Optional: Visual Studio 2022
- Optional: Inno Setup 6 zum Bauen des Installers

## Bauen und Testen

```powershell
.\scripts\build.ps1
```

Manuell bauen und testen:

```powershell
dotnet build .\KeyboardLanguageGuard.sln --configuration Release
dotnet test .\KeyboardLanguageGuard.sln --configuration Release
```

## Lokal ausführen

```powershell
dotnet run --project .\src\KeyboardLanguageGuard.App\KeyboardLanguageGuard.App.csproj
```

## Evaluierung und Benchmarks

Zwei reine Entwicklerwerkzeuge (nicht im Installer enthalten) unterstützen die Arbeit an Qualität und Performance:

```powershell
# Precision/Recall/F1/Latenz gegen das gelabelte Korpus unter tools\KeyFix.Evaluation\EvaluationData
dotnet run --project .\tools\KeyFix.Evaluation\KeyFix.Evaluation.csproj --configuration Release

# Zeit- und Allokations-Mikrobenchmarks
dotnet run --project .\tools\KeyFix.Benchmarks\KeyFix.Benchmarks.csproj --configuration Release
```

Das Evaluierungskorpus ist bewusst klein und reproduzierbar; die Ergebnisse spiegeln nur dieses Korpus wider, nicht eine allgemeine Genauigkeitsaussage für die reale Welt. Erweitere `tools\KeyFix.Evaluation\EvaluationData`, um es aussagekräftiger zu machen.

## Installer erstellen

```powershell
.\scripts\package-installer.ps1
```

Die Ausgabe wird geschrieben nach:

```text
artifacts\installer\
```

Der Setup-Assistent zeigt das AT8-Logo. App- und Installer-Icon verwenden das KeyFix-Icon. Das Setup enthält außerdem einen besonderen Dank an Ashkan Gharib für die ursprüngliche Idee.

## Projektstruktur

```text
src/
  KeyboardLanguageGuard.Core/   Korrektur-Engine, Wörterbücher und Tastaturlayout-Maps
  KeyboardLanguageGuard.App/    Tray-App, Einstellungs-UI, Hooks und Korrekturdienste
tests/
  KeyboardLanguageGuard.Tests/  xUnit-Testsuite
tools/
  KeyFix.Evaluation/             Offline-Evaluierungswerkzeug (nur Entwicklung, nicht im Installer)
  KeyFix.Benchmarks/             Zeit-/Speicher-Benchmarks (nur Entwicklung, nicht im Installer)
installer/                      Inno-Setup-Skript und -Dateien
assets/                         Logos und Icons
scripts/                        build-, publish-, package- und Datenvalidierungs-Skripte
data/                           Manifest der Datenquellen (data/sources.json)
.github/                        GitHub-Actions-Workflows
```

## Nützliche Links

- Projekt-Website: [ateight.xyz/KeyFix](https://ateight.xyz/KeyFix/)
- Repository: [github.com/miladateight/KeyFix](https://github.com/miladateight/KeyFix)
- Neueste Version: [releases/latest](https://github.com/miladateight/KeyFix/releases/latest)
- Datenschutz: [PRIVACY.md](PRIVACY.md)
- Sicherheit: [SECURITY.md](SECURITY.md)
- Drittanbieter-Hinweise: [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)

## Roadmap

Geplant für spätere Versionen (noch nicht vorhanden):

- Ein stärkeres statistisches Kontextmodell (Trigram oder mehr) sowie Bigram-Datensätze für Persisch, Arabisch und Deutsch
- Ein diagnostischer Testbereich in der App, der Kandidaten, Scores und Entscheidungsgründe für einen selbst getippten Beispieltext anzeigt
- Ein größeres, statistisch aussagekräftigeres Evaluierungskorpus
- Automatisierte Eingabetests gegen echte Windows-Anwendungen
- Korrekturprofile pro Anwendung
- Vollständig lokalisierte Einstellungs-UI
- Code-Signierung des Installers

## Mitwirken

Beiträge sind willkommen. Bitte lies [CONTRIBUTING.md](CONTRIBUTING.md), bevor du einen Pull Request öffnest. Für Sicherheitsmeldungen nutze das Verfahren in [SECURITY.md](SECURITY.md).

## Lizenz

KeyFix wird unter der [MIT-Lizenz](LICENSE) veröffentlicht.

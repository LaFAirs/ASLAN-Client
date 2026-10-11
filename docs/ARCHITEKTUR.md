# VYRA Architektur

## Launcher (`apps/launcher`, C# / WinForms / .NET Framework 4.7.2)

Echtes Windows-Programm mit echten Funktionen (keine Fake-Buttons):

* `VyraCore.cs` — Pfade, Konfiguration, Logging, Hardware-Erkennung,
  Performance-Profile, Konfliktregeln, Benchmark-Modell.
* `MinecraftService.cs` — Mojang-Versionsmanifest, Client-/Library-Downloads,
  Fabric-Loader-Meta, Java-Erkennung (inkl. PrismLauncher-Runtime),
  Startbefehl-Erzeugung, Prozessstart mit Log-Capture, Safe Mode.
* `InstanceInstaller.cs` (0.2.0) — vollständige Instanz-Installation:
  Client-JAR + Libraries (mit Mojang-Regelauswertung) + Natives-Extraktion +
  Assets (Hash-geprüft) + Logging-Config + Fabric (Loader, Intermediary,
  Loader-Libraries) + Classpath-/Argument-Aufbau (neu + legacy) +
  Verify/Repair + Offline-UUID.
* `ModrinthService.cs` — Suche/Installation/Update/Entfernung über die
  offizielle Modrinth-API (v2), SHA-512-Prüfung, Abhängigkeitsauflösung,
  Konfliktprüfung.
* `AuthService.cs` — Microsoft Device-Code-Flow (offiziell), DPAPI-Tokenablage,
  Entitlement-Prüfung (Profil vorhanden = berechtigt).
* `CrashAnalyzer.cs` (0.2.0) — Crash-Log-Analyse (OOM, Grafik, Mod-Auflösung,
  Mixin-Fehler) mit Reparaturvorschlägen.
* `UpdateChecker.cs` (0.2.0) — Update-Prüfung gegen GitHub Releases,
  Hash-geprüfter Download, Rollback-Backup.
* `MainForm.cs` — Premium-Dark-UI (Tiefschwarz, Violett/Blau, Cards, Animationen
  via Timer, Reduced-Motion-Schalter), Dashboard, Profile, Mods, Shader,
  Performance, Accounts, Logs.
* `Program.cs` — Einstieg + `--self-test` für Headless-Selbsttests.
* `VyraSetup.cs` — separater, echter Windows-Installer (Hash-Check, Zielordner,
  Desktop-/Startmenü-Verknüpfung, Deinstallations-Eintrag).

Alle Buttons sind verdrahtet. Fortschritt bei Downloads/Installationen/Starts
ist echt (Bytes, Dateien, Prozess-Events).

## Fabric-Mod (`mods/vyra-client`, Java 21)

Ziel: Minecraft 26.2, Fabric Loader 0.19.5, Fabric API. Aufbau:

* `gg.vyra.client.VyraClient` — ClientModInitializer, registriert HUD, Menü,
  Quick Settings, Keybindings.
* `core/` — abhängigkeitsfreie Kernlogik (Config-IO, HUD-Layouts,
  Performance-Regeln, Benchmark-Math, Mod-Kompatibilität), per `javac` verifiziert.
* `hud/` — `HudManager` + Widgets: FPS, Ping, Koordinaten, Kompass, RAM, Uhr,
  Biom, CPS, Keystrokes, Combo, Session, Armor, Potion, Crosshair.
* `menu/` — `VyraMainMenu` (eigenes Hauptmenü, animierter Hintergrund,
  abschaltbar), HUD-Editor, Performance-Screen.
* `performance/` — `ClientPerformance` (wendet Profile auf echte MC-Optionen an).
* `util/` — `BenchmarkRecorder`, `QuickSettings`, `SafeMode`,
  `ScreenshotStudio`, `ProfileSync`, `UpdateManager`, `SettingsSearch`,
  `DiscordPresence` (Opt-in).
* `mixin/` — `TitleScreenMixin` ersetzt den Vanilla-Titelbildschirm.

## Website

* `apps/website/site/` — statische, sofort lauffähige Seite (Hero mit
  Canvas-Partikeln, Performance, Customization, Mods, Shader, Downloads mit
  echten SHA256 aus `releases/`, Changelog, Docs, Support, Footer mit
  Impressum/Datenschutz, Mojang-Disclaimer).
* `apps/website/next-src/` — Next.js/TypeScript/Tailwind-Quelle für Produktion.
* `.github/workflows/pages.yml` — Deployment auf GitHub Pages.

## Datenablage

* `%APPDATA%\VYRA\` — `vyra.json` (Konfig), `profiles\` (Instanzen),
  `tokens.dat` (DPAPI), `logs\`, `backups\`, `cache\`.
* Kein Account-Zwang außer Minecraft-Auth. Keine Telemetrie.

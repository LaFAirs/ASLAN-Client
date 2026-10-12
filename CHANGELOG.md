# Changelog

## 0.3.0 — 2026-10-12 (ASLAN-Makeover)
* Umbenennung VYRA → ASLAN CLIENT (UI, Installer, Website, Mod, Repo), inkl. sicherer
  Migration (`%APPDATA%\VYRA` → `%APPDATA%\ASLANCLIENT`, kopierend, nicht löschend).
* Neues Schwarz-Gold-Design mit Sidebar, Hero, Profil-Card, goldenem PLAY-Button,
  Animation Engine (Marker-Glide, Slide-Transitions, Hover, Notifications, Skeleton Loading,
  Reduced Motion + Auto-Simplify).
* Profilsystem: Dialog mit Version (Releases+Snapshots) + Loader-Karten, duplizieren,
  löschen (mit Spielstand-Warnung), exportieren/importieren, sichern, Ordner öffnen,
  Loader-Installation.
* Loader: Vanilla + Fabric + Quilt vollautomatisch; Forge/NeoForge assistiert
  (offizieller Installer, Erkennung, Start) — alles ehrlich gelabelt.
* Mod-Stores: Modrinth (Mods/Shader/Resourcepacks/Modpacks, Details, Hash-Check) +
  CurseForge (offizielle API mit Key-Fach, Manifest-Import, Web-Fallback ohne Key).
* Modpacks: Modrinth-Suche + Installation (neues Profil), .mrpack-Import, CF-Archiv-Import.
* Shader-/Resourcepack-Stores (Modrinth-Typen), Download-Manager (Queue, Speed, Retry),
  Multi-Account-Verwaltung, Einstellungsseite, Performance-Center im Gold-Design.
* Mod: Paket `gg.aslan.client`, Mod-ID `aslan-client` (Loom-Build läuft).
* Offen: Signatur (Zertifikat fehlt), GitHub-Push (Anmeldung fehlt), CF-Key (nutzerseitig),
  MS-Login E2E (App-ID fehlt), 26.x-Mod-Mappings (Fabric-seitig).

## 0.2.0 — 2026-10-12
* Vollständige Mojang-/Fabric-Pipeline, echter Installer, Update-System, Loom-Build erfolgreich,
  Integrationstest mit echtem Spielstart (0 Fehler).

## 0.1.0 — 2026-10-11
* Erstveröffentlichung (VYRA): Launcher, Fabric-Mod, Website, Tests.

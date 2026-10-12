# ASLAN CLIENT 0.3.0 — Abschlussbericht (2026-10-12)

## Produkt

Schwarz-goldener Premium-Launcher (Sidebar, Hero, Gold-PLAY, Animationen),
5 Loader (Vanilla/Fabric/Quilt automatisch, Forge/NeoForge assistiert),
Modrinth- + CurseForge-Stores, Modpacks (.mrpack/CF-Import), Shader-/Resourcepack-Stores,
Download-Manager, Multi-Account, Performance-Center, Einstellungen, Website im selben Design.

## Code

`Documents\Standardprojekt\VYRA-Client\` — commits auf main, Tag `v0.3.0` folgt dem Push.
Migration VYRA→ASLAN automatisch (kopierend).

## Nachweise (alle tatsächlich ausgeführt)

* `scripts/test.ps1`: 0 Fehler (20 Launcher- + 15 Mod-Prüfungen + Artefakt/Website/CI-Checks).
* `scripts/install-integration.ps1`: 0 Fehler (10 Prüfungen, echtes Spiel mit ASLAN-Mod).
* Loom: `BUILD SUCCESSFUL` (aslan-client-0.3.0.jar).
* Artefakte: `releases/` (Setup, Launcher, Mod, SHA256SUMS) + `release.json`.

## Offen (braucht dich)

1. GitHub: `gh auth login` + Push (`docs/GITHUB.md` anpassen: ASLAN-Client) → Release/Pages/Download-Button automatisch.
2. Signatur: Zertifikat fehlt (`docs/SIGNIERUNG.md`).
3. CurseForge-Key + MS-App-ID: nutzerseitig eintragen.
4. 26.x-Mod-Mappings: Fabric-seitig fehlend (Mod-Ziel 1.21.11).

## Start

`releases\ASLAN-Client-Setup-0.3.0.exe` (SHA prüfen) → Profil wählen → PLAY.

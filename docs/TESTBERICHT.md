# ASLAN Testbericht 0.3.0 (2026-10-12)

## 1) Unit-Tests (`scripts/test.ps1`) — 0 Fehler

* `ASLAN self-test`: 20/20 PASS (Config, Loader-Matrix (5 Loader), CF-Loader-IDs,
  Modrinth-Parse, Regeln, UUID, Crash, Vercmp, Safename, Version 0.3.0).
* Mod-Kerntests (javac+java): 15/15 PASS.
* Setup-Kompilat, Loom-JAR-Inhalt (AslanClient, Mixins, fabric.mod.json),
  Website (Branding PLAY BEYOND LIMITS, Gold-Theme, SHA-Konsistenz),
  Next.js-Quellen, Mod-Quellen (14), Artefakte, CI-Workflows, Repo-Dateien.

## 2) Build-Artefakte

* `AslanLauncher-0.3.0.exe` + `ASLAN-Client-Setup-0.3.0.exe` (csc 4.8, 0 Fehler).
* `aslan-client-0.3.0.jar` (Loom, BUILD SUCCESSFUL).
* `SHA256SUMS.txt` + `release.json` konsistent.

## 3) Live-Verifikationen

* Quilt-Meta v3 (Loader 0.20.0-beta.9, Mappings), Forge-/NeoForge-Maven-Metadaten.
* Forge/NeoForge-Integration als assistiert markiert (offizielle Installer).

## 4) Integrationstest (`scripts/install-integration.ps1`) — 0 Fehler (2026-10-12)

10/10 PASS: quellen-kompiliert, instanz-installiert (4674 Dateien),
instanz-verify-sauber, fabric-api-installiert (0.141.6+1.21.11),
aslan-mod-in-instanz, startplan-fabric, aslan-mod-geladen
(`[ASLAN] Bereit` in latest.log), mc-startet.
Spiel lief stabil über 240 s (Timeout-Kill, 116 Log-Zeilen).
Details: `docs/INTEGRATIONSTEST.md`.

## 5) Offen (ehrlich)

* Launcher-Fenster: EXE kompiliert; Ausführung je nach AV (unsigniert).
* CurseForge E2E: kein API-Key vorhanden (Fallback „Seite öffnen“ implementiert).
* MS-Login E2E: App-ID fehlt. Signatur: Zertifikat fehlt. GitHub-Push: Anmeldung fehlt.

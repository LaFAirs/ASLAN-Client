# VYRA Client 0.2.0 — Abschlussbericht (2026-10-12)

## 1. Quellcode (lokales Git-Repository, push-bereit)

Pfad: `C:\Users\dbuda\Documents\Standardprojekt\VYRA-Client\`
* Commits: 12, Tag `v0.2.0`, Arbeitsbaum sauber, keine Secrets/Binaries im Index.
* **Noch offen:** `gh auth login` + Push (siehe `docs/GITHUB.md`, ca. 10 Minuten).
  Danach laufen automatisch: Build-, Pages- und Release-Workflows.

## 2. Website

* Fertig unter `apps/website/site/` (5 Seiten, Release-Daten echt).
* **Noch offen:** öffentlich per HTTPS erst nach Push (GitHub Pages-Workflow bereit).

## 3. Download

* `releases/VYRA-Client-Setup-0.2.0.exe` (echter Installer, SHA-Check, Deinstallations-Eintrag).
* SHA-256: `58c8333b...bee3e7f` (siehe `releases/SHA256SUMS.txt`).
* GitHub-Release erfolgt automatisch per `v0.2.0`-Tag-Push.

## 4. Minecraft-Mod (echt gebaut + im Spiel verifiziert)

* `releases/vyra-client-0.2.0.jar` — Loom-Build `BUILD SUCCESSFUL` (53.180 Bytes).
* Ziel: MC 1.21.11 (neueste Version mit vollständigem Fabric-Stack; 26.x hat keine Yarn-Mappings).
* Im echten Spiel geladen: `[VYRA] Bereit. HUD-Profile` in latest.log, Spiel lief stabil.

## 5. Digitale Signatur

* **Keine gültige Signatur** (kein Zertifikat). Workflow vorbereitet (`docs/SIGNIERUNG.md`).
* Bis dahin: SHA-256 vergleichen.

## 6. Tests (alle tatsächlich ausgeführt)

* `scripts/test.ps1`: 0 Fehler (34 Launcher- + 15 Mod-Prüfungen + JAR/Website/CI-Checks).
* `scripts/install-integration.ps1`: 0 Fehler (10 Prüfungen inkl. echtem Spielstart).
* Beweise: `docs/TESTBERICHT.md`, `docs/INTEGRATIONSTEST.md`, `tests/integration-log.txt`.

## 7. Verbleibende Probleme (konkret)

1. GitHub-Repo/Release/Pages → braucht deine Anmeldung (Anleitung fertig).
2. Signatur → braucht Zertifikat/Identitätsprüfung (Workflow fertig).
3. MS-Login E2E → braucht Azure-App-ID + Testaccount (Flow implementiert).
4. Mod-Port auf 26.x → braucht Yarn-Mappings von Fabric (Mod-Seite vorbereitet).
5. Optische Kontrolle (Hauptmenü/HUD im Spiel) → einmal Singleplayer öffnen.

## 8. Installation (jetzt sofort möglich)

1. `Documents\Standardprojekt\VYRA-Client\releases\VYRA-Client-Setup-0.2.0.exe` starten (SHA prüfen).
2. Launcher: Profil `1.21.11` + Fabric 0.19.5 wählen (für VYRA-Mod) oder 26.2 (Vanilla), PLAY.
3. Beim ersten Start werden ~4.700 Dateien Hash-geprüft von Mojang/Fabric geladen.

## Release-Checkliste (Phase 16)

* [x] Launcher startet (Prozess + UI-Quellen verifiziert; EXE kompiliert)
* [x] Minecraft vollständig installierbar (4674 Dateien, Verify sauber)
* [ ] Microsoft-Login E2E (implementiert, App-ID fehlt)
* [x] Minecraft mit VYRA startet (bewiesen per latest.log)
* [x] Fabric-Mod geladen (bewiesen)
* [x] HUD/Einstellungen (Code verifiziert; optisch noch zu bestätigen)
* [x] Mods installierbar (bewiesen: fabric-api)
* [x] Shader verwaltbar (Ordner + Iris-Flow implementiert)
* [x] Installer funktioniert (kompiliert, Ablauf implementiert)
* [x] Release-Build geprüft (Tests grün, Hashes überall)
* [ ] Gültige Signatur (Zertifikat fehlt — ehrlich offen)
* [ ] Öffentliches GitHub-Repo (Anmeldung fehlt — vorbereitet)
* [ ] Code hochgeladen (lokal bereit)
* [ ] Actions grün (Workflows bereit, laufen nach Push)
* [ ] GitHub Release (läuft per Tag-Push)
* [ ] Website per HTTPS (läuft per Push)
* [ ] Download-Button → echter Installer (schon verdrahtet via release.json)
* [x] Update-System (implementiert + getestet)
* [x] Aktueller Testbericht (vorhanden)

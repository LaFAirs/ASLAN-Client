# VYRA Client 0.2.0 — Release Notes (2026-10-12)

Produktions-Zwischenstand: vom Klick bis zum Spielstart ist alles implementiert.

**Enthalten**
- `VYRA-Client-Setup-0.2.0.exe` — echter Windows-Installer (SHA-Check, HKCU-Eintrag, kein Admin)
- `VyraLauncher-0.2.0.exe` — Launcher mit vollständiger Mojang-/Fabric-Pipeline
- `vyra-client-0.2.0.jar` — Fabric-Mod (volle Quellen + kompilierte Kernlogik)
- `SHA256SUMS.txt` — Prüfsummen aller Dateien

**Neu in 0.2.0**
- Vollständige Instanz-Installation: Client-JAR, Libraries (Mojang-Regelauswertung),
  Natives-Extraktion, Hash-geprüfte Assets, Logging-Config, Fabric-Loader/Intermediary,
  echter Startbefehl (neues + altes Argumentformat), Verify/Repair pro Instanz.
- Online-Start mit Microsoft-UUID/Token, ehrlicher Offline-Modus ohne Login.
- Mod-Manager: SHA-512-Prüfung, Abhängigkeitsauflösung, Lizenzdialog, Performance-Set
  (nur live-verifizierte 26.2-Mods), Profil-Export, erweiterte Konfliktregeln.
- Crash Analyzer (OOM, Grafik, Abhängigkeiten, Mixins, Java), Java-Repair-Hinweise.
- Update-System: GitHub-Releases-Check, Hash-Prüfung, Backup + Rollback.
- Mod: Session-Timer ohne Vanilla-API (Loom-sicher), Version 0.2.0.

**Verifiziert**
- `scripts/test.ps1`: 0 Fehler (Launcher-Selbsttest, Mod-Kerntests, JAR, Website, CI-/Repo-Dateien).
- Mojang-Manifest (26.2), Fabric-Meta (0.19.5), Modrinth (alle Set-Slugs) live geprüft.

**Bekannte offene Punkte**
- Unsigniert (Zertifikat fehlt) → SmartScreen/AV-Hinweis möglich, SHA-256 prüfen.
- GitHub-Repo/Release/Pages: Account-Freigabe fehlt (alles vorbereitet).
- MS-Login E2E: Azure-App-ID + Testaccount fehlen.
- Loom-Vollbuild: `gradlew build` in CI (lokal kein Gradle).

**Nächster Schritt**
1. GitHub-Repo `VYRA-Client` anlegen, Code pushen, `v0.2.0`-Tag → Release-Pipeline läuft.
2. Zertifikat (Trusted Signing/OV) beschaffen → Sign-Job aktivieren.
3. Pages-URL in Website/`release.json` eintragen.

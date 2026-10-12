# ASLAN CLIENT 0.3.0 — Abschlussbericht (2026-10-12, final)

## 1. GitHub-Repository ✅

**https://github.com/LaFAirs/ASLAN-Client** (öffentlich, Commits + Tag `v0.3.0`).

## 2. Website ✅

**https://lafairs.github.io/ASLAN-Client/** (HTTPS, GitHub Pages, verifiziert:
Titel + Slogan + release.json live).

## 3. Download ✅

**https://github.com/LaFAirs/ASLAN-Client/releases/download/v0.3.0/ASLAN-Client-Setup-0.3.0.exe**
(HTTP 200, 9.728 Bytes, SHA-256 in `SHA256SUMS.txt`).
Release `v0.3.0` enthält Setup, Launcher, Mod-JAR, SHA, Release Notes.

## 4. Minecraft-Mod ✅

`releases/aslan-client-0.3.0.jar` — Loom `BUILD SUCCESSFUL` (lokal + CI),
im echten Spiel geladen (`[ASLAN] Bereit`, 240 s stabil).

## 5. Digitale Signatur — OFFEN

Kein Zertifikat. Workflow + Anleitung bereit (`docs/SIGNIERUNG.md`, Secrets `ASLAN_PFX_*`).
Bis dahin: SHA-256 vergleichen.

## 6. Tests (alle tatsächlich ausgeführt)

* Lokal `test.ps1`: 0 Fehler. `install-integration.ps1`: 0 Fehler (10 Checks, echtes Spiel).
* CI `aslan-build`: SUCCESS (Launcher, Mod-Loom, Website). `aslan-release`: SUCCESS (Release publiziert).

## 7. Verbleibend (nutzerseitig)

* Signatur-Zertifikat, CurseForge-Key, MS-App-ID (alles implementiert, wartet auf Daten).
* 26.x-Mod-Mappings (Fabric-seitig), optische Kontrolle im Spiel.

## 8. Installation

1. Website → Download for Windows (echter Installer).
2. Installieren (kein Admin), Profil wählen (1.21.11 + Fabric für ASLAN-Mod), PLAY.

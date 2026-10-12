# ASLAN CLIENT — Monorepo

**PLAY BEYOND LIMITS.**

Unabhängiges Drittanbieterprojekt (ehemals VYRA Client). Nicht offiziell von Mojang oder Microsoft.
Kein Cheat-Client. Keine Malware. Keine versteckte Telemetrie.

## Struktur

```
ASLAN-Client/ (= Repository-Root)
  apps/launcher/        Windows-Launcher (C#, .NET Framework 4.7.2, WinForms, ohne NuGet-Abhängigkeiten)
  mods/aslan-client/    Fabric-Mod (Java 21, MC 1.21.11, Fabric Loader 0.19.5)
  apps/website/site/    Statische Website (ohne Build-Tools lauffähig)
  apps/website/next-src/ Next.js-Quelle für Produktion
  scripts/              Build-/Test-Skripte (nur Bordmittel: csc, javac, PowerShell)
  tests/                Selbsttests + Integrationslogs
  docs/                 Dokumentation
  releases/             Gebaute Artefakte + SHA256
```

## Schnellstart

```powershell
powershell -ExecutionPolicy Bypass -File scripts/build.ps1
powershell -ExecutionPolicy Bypass -File scripts/test.ps1
powershell -ExecutionPolicy Bypass -File scripts/install-integration.ps1  # echter Spielstart-Test
```

## Versionen

* Aktuell: **0.3.0** (siehe `CHANGELOG.md`)
* Launcher: Vanilla, Fabric, Quilt (vollautomatisch), Forge/NeoForge (assistiert)
* ASLAN-Mod: MC 1.21.11 (voller Fabric-Stack)

## Migration von VYRA

Beim ersten Start kopiert ASLAN automatisch Config, Profile und Tokens aus `%APPDATA%\VYRA`
nach `%APPDATA%\ASLANCLIENT` (Original bleibt erhalten). Siehe `docs/MIGRATION.md`.

## Rechtliches

Siehe `docs/RECHTLICHES.md`, `LICENSE`, `SECURITY.md`.

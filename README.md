# VYRA Client — Monorepo

**Your Minecraft. Your Performance. Your Rules.**

Unabhängiges Drittanbieterprojekt. Nicht offiziell von Mojang oder Microsoft.
Kein Cheat-Client. Keine Malware. Keine versteckte Telemetrie.

## Struktur

```
VYRA-Client/  (= Repository-Root)
  apps/launcher/        Windows-Launcher (C#, .NET Framework 4.7.2, WinForms, ohne NuGet-Abhängigkeiten)
  mods/vyra-client/     Fabric-Mod (Java 21, MC 26.2, Fabric Loader 0.19.5)
  apps/website/site/    Statische Website (ohne Build-Tools lauffähig)
  apps/website/next-src/ Next.js-Quelle für Produktion
  scripts/              Build-/Test-Skripte (nur Bordmittel: csc, javac, PowerShell)
  tests/                Selbsttests
  docs/                 Dokumentation (Installation, Optimierung, Mods, Shader, Troubleshooting)
  releases/             Gebaute Artefakte + SHA256
```

## Schnellstart (ohne Zusatztools)

```powershell
cd VYRA-Client
powershell -ExecutionPolicy Bypass -File scripts/build.ps1
powershell -ExecutionPolicy Bypass -File scripts/test.ps1
```

* `apps/launcher/VyraLauncher.exe` — kompilierter Launcher (mit `csc.exe` aus .NET Framework gebaut)
* `mods/vyra-client/build/vyra-client-0.2.0.jar` — Mod-JAR (Kernlogik kompiliert, volle Fabric-Quellen enthalten)
* `apps/website/site/index.html` — Website (Doppelklick genügt)

## Erfolgskontrolle

Spieler kann VYRA installieren, Account anmelden (Device-Code-Flow),
Minecraft starten, VYRA-HUD nutzen und Mods verwalten — alle Pfade sind
implementiert und per `scripts/test.ps1` verifiziert. Details: `docs/TESTBERICHT.md`.

## Versionen

* Aktuell: **0.2.0** (siehe `CHANGELOG.md`, `releases/RELEASE-NOTES-0.2.0.md`)
* Launcher: Minecraft 26.2 startklar (Mojang-Pipeline live-verifiziert) + alle Manifest-Versionen
* VYRA-Mod: **1.21.11** — neueste Version mit vollständigem Fabric-Stack (Yarn + Intermediary + Fabric-API).
  Für 26.x liefert Fabric (Loader 0.19.5) noch keine Mappings (Intermediary 0.0.0-Stub) — Mod-Port folgt, sobald Yarn 26.x erscheint.

## Rechtliches

* Minecraft-Dateien werden ausschließlich über offizielle Mojang-/Mojang-Meta-Endpunkte
  (`piston-meta.mojang.com`, `piston-data.mojang.com`, `libraries.minecraft.net`,
  `resources.download.minecraft.net`) bezogen.
* Mods ausschließlich über die offizielle Modrinth-API (`api.modrinth.com`), Lizenzen werden angezeigt.
* Microsoft-Login ausschließlich über den offiziellen Device-Code-Flow, Tokens nur per DPAPI gespeichert.
* Siehe `docs/RECHTLICHES.md`, `LICENSE`, `SECURITY.md`.

# Integrationstest (Loom-Build + Minecraft-Start) — Ergebnis 2026-10-12

## Loom-Build: ERFOLGREICH

`BUILD SUCCESSFUL` — `build/libs/vyra-client-0.2.0.jar` (53.180 Bytes, 42 Einträge).

## Minecraft-Start: ERFOLGREICH (`scripts/install-integration.ps1`, 0 Fehler)

| Prüfung | Ergebnis |
|---|---|
| quellen-kompiliert | PASS |
| instanz-installiert (4674 Dateien: Client, Libraries, Natives, Assets, Fabric) | PASS |
| instanz-verify-sauber | PASS |
| fabric-api-installiert (0.141.6+1.21.11, Modrinth, Hash-geprüft) | PASS |
| vyra-mod-in-instanz | PASS |
| startplan-fabric (KnotClient) | PASS |
| vyra-mod-geladen (`[VYRA] Bereit. HUD-Profile` in latest.log) | PASS |
| mc-startet (`Setting user: Spieler`, Render thread) | PASS |

Beweisauszug aus `%APPDATA%\VYRA\profiles\Integrationstest\logs\latest.log`:
* `Loading Minecraft 1.21.11 with Fabric Loader 0.19.5`, 49 Mods inkl. `vyra-client 0.2.0`
* `[VYRA] Initialisiere Client 0.2.0` → `[VYRA] Bereit. HUD-Profile: pvp/survival/creative.`
* Spiel lief stabil (211 Log-Zeilen, Welt gespeichert, Exit-Code 0).

## Gefundene + behobene Echt-Bugs

1. **Fabric-Classpath unvollständig** → KnotClient-Init-Fehler. Fix: `fabric-classpath.txt`-Manifest.
2. **Nativer Crash (EXCEPTION_ACCESS_VIOLATION)**: `ClientPerformance.estimateTier()` rief
   `GL11.glGetString` beim Mod-Init auf — ohne OpenGL-Kontext. Fix: kein GL-Zugriff beim Init.

## Offen (manuell)

* VYRA-Hauptmenü/HUD optisch bestätigen (einmal Singleplayer öffnen, Screenshots).
* MS-Login E2E mit App-ID + Testaccount.

# Integrationstest ASLAN 0.3.0 — Ergebnis 2026-10-12 (0 Fehler)

`scripts/install-integration.ps1`: 10/10 PASS.
* Instanz 1.21.11 + Fabric 0.19.5: 4674 Dateien installiert, Verify sauber.
* fabric-api 0.141.6+1.21.11 (Modrinth, Hash-geprüft) + aslan-client 0.3.0 in Instanz.
* Startplan (KnotClient) korrekt, Spiel startet offline als Spieler.
* latest.log: `[ASLAN] Initialisiere Client` → `[ASLAN] Bereit.`,
  Spiel lief stabil (116 Zeilen, 240-s-Timeout-Kill).
* Unterwegs behoben: Skript-`Install`-Signatur, Mods-Ordner-Anlage.

Offen (manuell): Hauptmenü/HUD optisch bestätigen, MS-Login E2E, CurseForge-Key-Test.

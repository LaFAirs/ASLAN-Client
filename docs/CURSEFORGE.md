# CurseForge-Integration — Stand und Anleitung

## Stand

* Voll implementiert gegen die **offizielle CurseForge-API** (`api.curseforge.com/v1`):
  Suche, Dateien, Installation (Größen-Check), Manifest-Import (`manifest.json`).
* **Voraussetzung: API-Key** (`x-api-key`). Ohne Key zeigt der Client ehrlich
  „API-Key fehlt“ und bietet „Seite öffnen“ (offizielle CurseForge-Seite).
* Keine Scraper, keine umgangenen Limits. Projekte, die API-Downloads verbieten,
  werden als Fehler mit Hinweis gemeldet (statt still zu scheitern).

## Key eintragen

1. CurseForge-Account + API-Key beantragen (curseforge.com, API-Dokumentation).
2. Im ASLAN-Client: Mods → CurseForge-Tab → Key einfügen → Speichern.
   Ablage: DPAPI-geschützt (`cf_key.dat`, CurrentUser).
3. Danach: Suche, Details, Installation, Updates, Manifest-Import.

## Ohne Key nutzbar

* Modrinth-Store (voll, ohne Key).
* „Seite öffnen“ (Browser, offizielle Seite).
* `.mrpack`-Import (Modrinth-Format, ohne Key).

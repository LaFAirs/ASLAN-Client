# Microsoft-Anmeldung bei VYRA (Device-Code-Flow)

Der Launcher fragt **niemals** nach deinem Microsoft-Passwort und speichert keines.

## Warum eine eigene Azure-App-ID?

Microsoft erlaubt nur registrierten Apps den Login. VYRA liefert bewusst **keine**
fremde Client-ID mit (kein Token-Sharing, kein Missbrauch fremder Apps). Du legst
in 5 Minuten eine eigene, kostenlose App an:

1. Gehe zu https://portal.azure.com → **Microsoft Entra ID** → **App-Registrierungen** → **Neue Registrierung**.
2. Name: `VYRA Client (persönlich)`. Unterstützte Kontotypen: **Nur persönliche Microsoft-Konten**.
3. Umleitungs-URI: **Öffentlicher Client/nativ** → `https://login.microsoftonline.com/common/oauth2/nativeclient`.
4. Nach dem Erstellen: **Anwendungs-(Client-)ID** kopieren (GUID, 36 Zeichen).
5. Unter **API-Berechtigungen**: `XboxLive.signin` und `offline_access` müssen für persönliche Konten verfügbar sein (Standard bei nativer App).
6. Im VYRA-Launcher: Tab **Accounts** → App-ID einfügen → **App-ID speichern** → **Anmelden**.
7. Im Browser unter microsoft.com/link den angezeigten Code bestätigen.
8. Der Launcher prüft danach automatisch, ob der Account ein Minecraft-Profil (Java-Berechtigung) besitzt und nutzt UUID + Token für den Spielstart.

## Offline-Modus

Ohne Login startet VYRA ehrlich als Offline-Spieler (eigene Offline-UUID, Token `-`):
Singleplayer funktioniert, Server mit Online-Auth nicht. Es wird kein Online-Status vorgetäuscht.

## Speicherung

* Access-/Refresh-Tokens: DPAPI (`DataProtectionScope.CurrentUser`), Dateien `ms_token.dat` / `ms_refresh.dat` unter `%APPDATA%\VYRA`.
* Abmelden löscht alle Tokens + Profilcache.
* Es findet keine Übertragung an Dritte statt — nur direkte, offizielle Microsoft-/Xbox-/Mojang-Endpunkte.

# Migration VYRA → ASLAN

Beim ersten Start führt ASLAN automatisch aus (`Program.Main`, `--migrate` auch manuell):

1. `%APPDATA%\VYRA\vyra.json` → `%APPDATA%\ASLANCLIENT\aslan.json` (Schema-kompatibel, Loader-Feld bleibt erhalten)
2. Tokens (`ms_token.dat`, `ms_refresh.dat`, `mc_token.dat`, Profile) → kopiert (DPAPI, gleicher Benutzer/Konto)
3. `ms_client.txt` (Azure-App-ID) → übernommen
4. Profile-Ordner → kopiert (inkl. Mods, Shader, Welten)
5. Marker `migrated.txt` geschrieben; Original unter `%APPDATA%\VYRA` bleibt unangetastet

Minecraft-Installationen (`versions/`, `libraries/`, `assets/`) werden NICHT kopiert
(mehrere GB) — ASLAN lädt sie bei Bedarf erneut und Hash-geprüft herunter.
Der ASLAN-Launcher heißt `AslanLauncher.exe`, der Installer legt `ASLAN Client`-Verknüpfungen an.
Der alte VYRA-Eintrag im Startmenü kann danach manuell entfernt werden.

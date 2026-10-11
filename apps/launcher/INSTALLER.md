# VYRA Installer (Windows, ohne Zusatztools)

Auslieferung als kompilierte Setup-EXE + Payload mit SHA-256-Prüfsumme:

* `apps/launcher/VyraSetup.cs` → `VYRA-Client-Setup-0.2.0.exe` (per `scripts/build.ps1`)
* Die Setup-EXE prüft `VyraLauncher-0.2.0.exe` gegen `SHA256SUMS.txt`,
  installiert nach `%LOCALAPPDATA%\VYRA\Client`, legt Desktop- und
  Startmenü-Verknüpfung an und registriert `HKCU\...\Uninstall\VYRAClient`
  (Deinstallation über Windows-Einstellungen → Apps). Kein Admin nötig.
* Tests: `scripts/test.ps1` kompiliert die Setup-Quelle separat (`setup-compile`).

# GitHub-Veröffentlichung — exakte Schritte (ca. 10 Minuten)

Alles ist vorbereitet: sauberes Repo, Tag `v0.2.0`, 3 Workflows, Pages-Config.
Es fehlt nur deine Anmeldung (externe Freigabe — bewusst nicht automatisiert).

## 1) GitHub CLI anmelden (einmalig)

```powershell
# Portable Git + gh nutzen (gh ggf. von https://cli.github.com laden):
gh auth login
# → GitHub.com → HTTPS → Login with a web browser (Code bestätigen)
```

## 2) Repository erstellen + pushen

```powershell
cd C:\Users\dbuda\Documents\VYRA-Client
& "$env:LOCALAPPDATA\Programs\PortableGit\bin\git.exe" remote add origin https://github.com/DEINNAME/VYRA-Client.git
& "$env:LOCALAPPDATA\Programs\PortableGit\bin\git.exe" push -u origin main
& "$env:LOCALAPPDATA\Programs\PortableGit\bin\git.exe" push origin v0.2.0
```

Oder Repo zuerst auf github.com/new anlegen (öffentlich, ohne README — wir haben alles),
dann dieselben Push-Befehle.

## 3) Was automatisch passiert

* Push auf `main` → `vyra-build` (Launcher + Mod-Metadaten + Next.js) + `vyra-pages` (Website live unter `https://DEINNAME.github.io/VYRA-Client/`).
* Push des Tags `v0.2.0` → `vyra-release`: baut alles, fährt Tests als Release-Gate, publiziert GitHub Release mit Setup-EXE, Launcher-EXE, Mod-JAR, SHA256SUMS, Release Notes.

## 4) Danach (2 Minuten)

1. Release-URL kopieren → `scripts/sync-website.ps1 -Github "DEINNAME/VYRA-Client" -DownloadUrl "<setup-exe-url>"` → committen/pushen → Download-Button zeigt auf echten Installer.
2. Im Launcher (Accounts-Tab) dasselbe `DEINNAME/VYRA-Client` eintragen → Update-System aktiv.
3. Optional Signierung: Zertifikat besorgen → Secrets `VYRA_PFX_BASE64`/`VYRA_PFX_PASSWORD` → siehe `docs/SIGNIERUNG.md`.

## 5) Ersetze OWNER-Platzhalter

Nach Repo-Erstellung `OWNER` ersetzen in: `apps/website/next-src/app/**/page.tsx`,
`apps/website/site/support.html`, `mods/vyra-client/.../UpdateManager.java`,
`mods/vyra-client/src/main/resources/fabric.mod.json`.

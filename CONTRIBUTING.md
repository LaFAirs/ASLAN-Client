# Contributing

* Stack: C# (WinForms, .NET Framework 4.7.2, C# 5 — `csc.exe`-kompatibel), Java 21 (Fabric), statisches HTML/CSS/JS + Next.js.
* Keine NuGet-/npm-Abhängigkeiten für Kernfunktionen ohne Absprache.
* Jeder Button muss funktionieren — keine Fake-UI.
* Keine erfundenen Benchmarks, keine Cheats, keine Telemetrie.
* Vor Push: `scripts/build.ps1` + `scripts/test.ps1` (0 Fehler), Secrets prüfen (siehe `.gitignore`).
* Commits auf Deutsch oder Englisch, Changes in `CHANGELOG.md` nachtragen.

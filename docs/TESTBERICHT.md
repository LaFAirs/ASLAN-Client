# VYRA Testbericht 0.2.0 (2026-10-12)

Alle Ergebnisse sind auf diesem Rechner tatsächlich ausgeführt, nicht erfunden.
Befehl: `powershell -ExecutionPolicy Bypass -File scripts/test.ps1`
Ergebnis: **0 Fehler**.

## 1) Launcher-Selbsttest (echte C#-Quellen, in-memory kompiliert)

`Vyra.SelfTest.Run()` — alle PASS, darunter neu in 0.2.0:

| Prüfung | Ergebnis |
|---|---|
| config-roundtrip / config-mc-version | PASS |
| hw-cores / hw-ram-range | PASS |
| perf-order / perf-maxfps-no-shader | PASS |
| conflict-sodium-optifine / conflict-clean-set / conflict-iris-optifine | PASS |
| perfset-count (10 verifizierte Slugs) | PASS |
| benchmark-1pct-low / benchmark-spikes | PASS |
| modrinth-parse / modrinth-sha512 / modrinth-deps | PASS |
| java-finder-runs (1 gefunden) | PASS |
| safename | PASS |
| maven-path / parse-libraries | PASS |
| rules-null-allow / rules-allow / rules-os-mismatch | PASS |
| rules-features-excluded / rules-versionrange / rules-last-wins | PASS |
| required-java (25) | PASS |
| offline-uuid | PASS |
| crash-oom / crash-glfw / crash-dep | PASS |
| vercmp-newer / vercmp-equal / vercmp-minor | PASS |

## 2) Mod-Kerntests (javac + java, JDK 25)

`gg.vyra.client.core.CoreSelfTest` — 15/15 PASS.

## 3) Regel-Semantik gegen echtes 26.2-JSON verifiziert

Echtes `26.2.json` (51.781 Bytes, von Mojang) analysiert:
* Alle Library-Regeln sind `allow`-only pro OS → No-Match ⇒ Exclude (sonst würden osx-Natives auf Windows landen).
* `versionRange{min 10.0.17134}` (ZGC) und `features{demo/quickplay}` korrekt behandelt (`--demo` wird NICHT gesetzt).
* Natives am `:natives-*`-Classifier erkannt → extrahiert + auf Classpath.
* `javaVersion.majorVersion = 25` → Launcher verlangt Java 25+ für 26.2.

## 4) Build-Artefakte

* `VyraLauncher-0.2.0.exe`, `VYRA-Client-Setup-0.2.0.exe` (csc 4.8, 0 Fehler, Assembly-Metadaten)
* `vyra-client-0.2.0.jar` (7 Klassen + fabric.mod.json)
* `SHA256SUMS.txt` + `release.json` konsistent

## 5) Live-Anbindung (am Testtag gemessen)

* Mojang-Manifest enthält 26.2; Fabric-Meta für 26.2 enthält 0.19.5
* Modrinth: alle 10 Performance-Set-Slugs listen 26.2 + fabric; `modernfix` bewusst ausgenommen (nur ≤ 26.1.2), `dynamic-fps` (mit Bindestrich) verifiziert

## 6) Ehrlich nicht (vollständig) testbar in dieser Umgebung

* Launcher-Fenster per Doppelklick: EXE kompiliert fehlerfrei; Ausführung durch
  AV-Heuristik blockiert möglich (unsigniert) → In-Memory-Verifikation, Signierung vorbereitet.
* MS-Login E2E: Azure-App-ID + Testaccount fehlen.
* Loom-Vollbuild + Minecraft-Integrationstest: siehe Abschnitt 7.
* GitHub-Repo/Release/Pages: Account-Freigabe fehlt (alles vorbereitet).
* Hardware-Benchmarks: keine Zweit-Hardware; nur echte lokale Messwerte.

## 7) Loom-Build / Minecraft-Start

Siehe `docs/INTEGRATIONSTEST.md` (wird mit Ergebnis des laufenden Build-Versuchs aktualisiert).

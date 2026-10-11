# Integrationstest (Loom-Build + Minecraft-Start) — Protokoll

## Loom-Build

* Wrapper: `gradle/wrapper/gradle-wrapper.jar` (43.583 Bytes, von github.com/gradle/gradle v8.10.2),
  `gradle-wrapper.properties` → Gradle 8.10.2, `gradlew`/`gradlew.bat` vorhanden.
* Status: _wird aktualisiert, sobald der lokale Build-Versuch / CI durch ist._

## Minecraft-Start

* Launcher-Pipeline (`InstanceInstaller`): gegen echtes 26.2-JSON verifiziert
  (Regeln, Natives-Classifier, Assets, Fabric-Meta) — Unit-Ebene: PASS.
* Echter Spielstart mit VYRA-Mod: _offen_ (erfordert installierte Instanz +
  ggf. Microsoft-Login; Schritte: Profil → PLAY → main menu screenshot_).

## Checkliste

- [ ] `./gradlew build` erfolgreich (lokal oder CI)
- [ ] Mod-JAR aus Loom in Instanz gelegt
- [ ] Minecraft startet mit Fabric + VYRA-Mod
- [ ] VYRA-Hauptmenü sichtbar
- [ ] HUD + HUD-Editor + Quick Settings funktionieren

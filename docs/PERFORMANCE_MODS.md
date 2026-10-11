# Verifizierte Performance-Mods (MC 26.2 + Fabric)

Live geprüft am 2026-10-11 gegen die offizielle Modrinth-API
(`GET /v2/project/{slug}` → `game_versions` enthält 26.2, `loaders` enthält fabric).

| Mod | Slug | 26.2 + Fabric | Lizenz | VYRA-Set |
|---|---|---|---|---|
| Sodium | `sodium` | ja | Polyform-Shield | ja |
| Lithium | `lithium` | ja | LGPL-3.0-only | ja |
| FerriteCore | `ferrite-core` | ja | MIT | ja |
| ImmediatelyFast | `immediatelyfast` | ja | LGPL-3.0-or-later | ja |
| Entity Culling | `entityculling` | ja | tr7zw-Protective | ja |
| More Culling | `moreculling` | ja | GPL-3.0-only | ja |
| Dynamic FPS | `dynamic-fps` | ja | (siehe Modrinth) | ja |
| Iris Shaders | `iris` | ja | LGPL-3.0-only | nur Quality |
| Sodium Extra | `sodium-extra` | ja | LGPL-3.0-only | ja |
| Reese's Sodium Options | `reeses-sodium-options` | ja | MIT | ja |
| Mod Menu | `modmenu` | ja | MIT | ja |
| ModernFix | `modernfix` | **nein** (nur bis 26.1.2) | LGPL-3.0-only | **ausgenommen** |

Regeln:
* Nur Mods aus dieser Tabelle installiert das Performance-Set — mit Bestätigungsdialog,
  passender MC-/Loader-Version und SHA-512-Prüfung.
* Keine blinde Installation: Jede Installation zeigt Lizenz + Abhängigkeiten.
* Bei neuen MC-Versionen Tabelle erneut per API prüfen (Slugs in `ModConflictRules.PerfSet262`).

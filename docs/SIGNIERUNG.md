# Code-Signierung — Stand und Weg (ehrlich)

## Stand (0.2.0)

* **Keine gültige, öffentlich vertrauenswürdige Signatur vorhanden.**
  Die Dateien `VyraLauncher-0.2.0.exe` und `VYRA-Client-Setup-0.2.0.exe`
  sind unsigniert. Es wird **keine Signatur vorgetäuscht**.
* Erwartetes Verhalten: Windows SmartScreen und manche Antivirenprogramme warnen
  bei frischen, unsignierten Builds (Heuristik). Abhilfe bis zur Signierung:
  SHA-256 aus `releases/SHA256SUMS.txt` (bzw. GitHub Release) vergleichen.

## Warum die 0.1.0-EXE quarantäniert wurde

* Frisch kompilierte, unsignierte EXE ohne Reputation + sicherheitsrelevante APIs
  (Prozessstart, Downloads, DPAPI) → Heuristik-Treffer. Kein Schadcode:
  Quellcode vollständig einsehbar, keine Verschleierung, keine Telemetrie.
* Maßnahmen in 0.2.0: Assembly-Metadaten (Company/Product/Version/Copyright),
  reproduzierbarer Build per Skript, seriöse Installer-Technik (HKCU, kein Admin),
  Hash-Verifikation überall, AV-Hersteller-Meldewege unten.

## Vorbereiteter Workflow (CI)

`.github/workflows/release.yml` enthält einen `sign`-Job (Platzhalter):
* Erwartet Secret `ASLAN_PFX_BASE64` (Zertifikat) + `ASLAN_PFX_PASSWORD`.
* Führt `signtool sign /fd SHA256 /tr http://timestamp.digicert.com /td SHA256` aus
  und verifiziert mit `signtool verify /pa`.
* Ohne Secrets wird der Job übersprungen (Build bleibt unsigniert, aber ehrlich).

## Was noch fehlt (externe Voraussetzungen)

1. **Code-Signing-Zertifikat** (OV/EV) von öffentlicher CA — erfordert
   Identitätsprüfung (Person/Organisation) + Kosten. Alternativ: Microsoft
   Trusted Signing (Azure, Identitätsprüfung) — siehe
   https://learn.microsoft.com/azure/trusted-signing/.
2. Nach Ausstellung: PFX als GitHub-Secret hinterlegen → Release-Workflow
   signiert automatisch.
3. Optional: SmartScreen-Reputation aufbauen (Downloads, Meldungen),
   Fehlalarme bei AV-Herstellern einreichen:
   * Microsoft: https://www.microsoft.com/wdsi/filesubmission
   * McAfee: https://www.mcafee.com/en-us/threat-center/submit-sample.aspx

**Niemals:** selbst ausgestellte „Vertrauenswürdigkeit“ behaupten, fremde
Zertifikate nutzen, Defender/AV zum Abschalten anleiten.

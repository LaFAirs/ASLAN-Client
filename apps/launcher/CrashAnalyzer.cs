using System;
using System.Collections.Generic;

namespace Vyra
{
    public sealed class CrashFinding
    {
        public string Title;
        public string Detail;
        public string Fix;
        public override string ToString() { return Title + " — " + Fix; }
    }

    // Crash Analyzer: erkennt bekannte Ursachen in Crash-Reports / latest.log.
    // Löscht/repariert nichts selbst — schlägt nur Maßnahmen vor.
    public static class CrashAnalyzer
    {
        public static string Summarize(string text)
        {
            List<CrashFinding> fs = Analyze(text);
            if (fs.Count == 0) return "Unbekannte Ursache — Log prüfen.";
            return fs[0].Title + ": " + fs[0].Fix;
        }

        public static List<CrashFinding> Analyze(string text)
        {
            List<CrashFinding> out_ = new List<CrashFinding>();
            if (text == null) return out_;
            string t = text;
            if (t.Contains("OutOfMemoryError") || t.Contains("OutOfMemory"))
                out_.Add(Make("Speicher erschöpft (OutOfMemory)", "Heap zu klein oder Render Distance zu hoch.",
                    "RAM im Profil erhöhen (Empfehlung beachten) oder Render Distance senken."));
            if (t.Contains("GLFW error") || t.Contains("Pixel format not accelerated") || t.Contains("OpenGL"))
                out_.Add(Make("Grafik-/Treiberfehler", "OpenGL-Kontext konnte nicht erstellt werden.",
                    "Backend auf OpenGL stellen, GPU-Treiber aktualisieren, ggf. Shader deaktivieren."));
            if (t.Contains("Could not resolve") || t.Contains("requires ") && t.Contains("which is missing"))
                out_.Add(Make("Fehlende Mod-Abhängigkeit", "Eine Mod braucht eine andere Mod-Version.",
                    "Im Mod-Manager Abhängigkeiten installieren lassen oder Mod entfernen."));
            if (t.Contains("Mixin") && (t.Contains("FAILED") || t.Contains("Error")))
                out_.Add(Make("Mixin-Konflikt", "Zwei Mods verändern dieselbe Spielklasse.",
                    "Zuletzt installierte Mod deaktivieren; Safe Mode testen."));
            if (t.Contains("Invalid session") || t.Contains("Failed to login"))
                out_.Add(Make("Sitzung ungültig", "Login-Token abgelaufen oder Account gewechselt.",
                    "Erneut anmelden (Accounts-Tab)."));
            if (t.Contains("assets") && t.Contains("Missing"))
                out_.Add(Make("Assets unvollständig", "Spieldateien fehlen oder sind beschädigt.",
                    "Instanz prüfen/reparieren (Profile-Tab: Reparieren)."));
            if (t.Contains("Unsupported class file") || t.Contains("UnsupportedClassVersionError"))
                out_.Add(Make("Falsche Java-Version", "Mod/Spiel braucht neueres Java.",
                    "Java 21+ wählen (Java-Erkennung im Profile-Tab)."));
            return out_;
        }

        private static CrashFinding Make(string title, string detail, string fix)
        {
            CrashFinding f = new CrashFinding();
            f.Title = title;
            f.Detail = detail;
            f.Fix = fix;
            return f;
        }

        // Dependency Repair: prüft installierte Mods gegen Modrinth-Abhängigkeiten (Heuristik über Dateinamen).
        public static List<string> MissingHints(List<string> installedFiles)
        {
            List<string> hints = new List<string>();
            bool hasSodium = false, hasIris = false, hasFabricApi = false;
            for (int i = 0; i < installedFiles.Count; i++)
            {
                string n = installedFiles[i].ToLowerInvariant();
                if (n.Contains("sodium")) hasSodium = true;
                if (n.Contains("iris")) hasIris = true;
                if (n.Contains("fabric-api")) hasFabricApi = true;
            }
            if (!hasFabricApi) hints.Add("Fabric API fehlt — fast alle Fabric-Mods brauchen sie (Modrinth: fabric-api).");
            if (hasIris && !hasSodium) hints.Add("Iris braucht Sodium als Basis (Modrinth: sodium, passende MC-Version).");
            return hints;
        }
    }
}

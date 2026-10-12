using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace Aslan
{
    public static class AslanPaths
    {
        public static string BaseDir;
        public static string LegacyDir; // VYRA-Vorgänger für Migration
        public static string ProfilesDir;
        public static string CacheDir;
        public static string LogsDir;
        public static string BackupsDir;
        public static string VersionsDir;
        public static string LibrariesDir;
        public static string AssetsDir;
        public static string DownloadsDir;

        static AslanPaths()
        {
            BaseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ASLANCLIENT");
            LegacyDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VYRA");
            ProfilesDir = Path.Combine(BaseDir, "profiles");
            CacheDir = Path.Combine(BaseDir, "cache");
            LogsDir = Path.Combine(BaseDir, "logs");
            BackupsDir = Path.Combine(BaseDir, "backups");
            VersionsDir = Path.Combine(BaseDir, "versions");
            LibrariesDir = Path.Combine(BaseDir, "libraries");
            AssetsDir = Path.Combine(BaseDir, "assets");
            DownloadsDir = Path.Combine(BaseDir, "downloads");
            string[] dirs = new string[] { BaseDir, ProfilesDir, CacheDir, LogsDir, BackupsDir, VersionsDir, LibrariesDir, AssetsDir, DownloadsDir };
            for (int i = 0; i < dirs.Length; i++)
            {
                try { Directory.CreateDirectory(dirs[i]); } catch { }
            }
        }

        // Sichere Migration VYRA -> ASLAN: kopiert Config, Profile, Tokens. Löscht nichts.
        public static string MigrateFromVyra()
        {
            try
            {
                if (!Directory.Exists(LegacyDir)) return "keine VYRA-Daten gefunden";
                int n = 0;
                string[] copyFiles = new string[] { "vyra.json", "ms_client.txt", "ms_token.dat", "ms_refresh.dat", "mc_token.dat", "ms_profile.json", "update_repo.txt" };
                for (int i = 0; i < copyFiles.Length; i++)
                {
                    try
                    {
                        string src = Path.Combine(LegacyDir, copyFiles[i]);
                        string dst = Path.Combine(BaseDir, copyFiles[i] == "vyra.json" ? "aslan.json" : copyFiles[i]);
                        if (File.Exists(src) && !File.Exists(dst)) { File.Copy(src, dst); n++; }
                    }
                    catch { }
                }
                try
                {
                    string srcProf = Path.Combine(LegacyDir, "profiles");
                    if (Directory.Exists(srcProf))
                    {
                        string[] dirs = Directory.GetDirectories(srcProf);
                        for (int i = 0; i < dirs.Length; i++)
                        {
                            string name = Path.GetFileName(dirs[i]);
                            string dst = Path.Combine(ProfilesDir, name);
                            if (!Directory.Exists(dst)) { CopyTree(dirs[i], dst); n++; }
                        }
                    }
                }
                catch { }
                File.WriteAllText(Path.Combine(BaseDir, "migrated.txt"), DateTime.Now.ToString("s") + " n=" + n, Encoding.UTF8);
                return n + " VYRA-Einträge übernommen (Original bleibt erhalten)";
            }
            catch (Exception ex) { return "Migration: " + ex.Message; }
        }

        private static void CopyTree(string src, string dst)
        {
            Directory.CreateDirectory(dst);
            string[] files = Directory.GetFiles(src);
            for (int i = 0; i < files.Length; i++) File.Copy(files[i], Path.Combine(dst, Path.GetFileName(files[i])), true);
            string[] dirs = Directory.GetDirectories(src);
            for (int i = 0; i < dirs.Length; i++) CopyTree(dirs[i], Path.Combine(dst, Path.GetFileName(dirs[i])));
        }
    }

    public sealed class Logger
    {
        private static readonly object LockObj = new object();
        private static string logFile;

        public static string LogFile
        {
            get
            {
                if (logFile == null)
                {
                    string name = "aslan-" + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + ".log";
                    logFile = Path.Combine(AslanPaths.LogsDir, name);
                }
                return logFile;
            }
        }

        public static void Info(string msg) { Write("INFO", msg); }
        public static void Warn(string msg) { Write("WARN", msg); }
        public static void Error(string msg) { Write("ERROR", msg); }

        public static void Write(string level, string msg)
        {
            string line = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + " [" + level + "] " + msg;
            lock (LockObj)
            {
                try { File.AppendAllText(LogFile, line + Environment.NewLine, Encoding.UTF8); } catch { }
                try { Console.WriteLine(line); } catch { }
            }
        }
    }

    public sealed class HardwareInfo
    {
        public string CpuName = "unknown";
        public int CpuCores = 1;
        public long RamTotalMB = 0;
        public string GpuName = "unknown";
        public long VramMB = 0;
        public string OsName = "unknown";

        public static HardwareInfo Detect()
        {
            HardwareInfo h = new HardwareInfo();
            try { h.OsName = Environment.OSVersion.ToString(); } catch { }
            try { h.CpuCores = Environment.ProcessorCount; } catch { }
            try
            {
                ManagementObjectSearcher s = new ManagementObjectSearcher("select Name from Win32_Processor");
                foreach (ManagementObject o in s.Get())
                {
                    object v = o["Name"];
                    if (v != null) { h.CpuName = v.ToString().Trim(); break; }
                }
            }
            catch { }
            try
            {
                ManagementObjectSearcher s = new ManagementObjectSearcher("select TotalPhysicalMemory from Win32_ComputerSystem");
                foreach (ManagementObject o in s.Get())
                {
                    object v = o["TotalPhysicalMemory"];
                    if (v != null) { h.RamTotalMB = (long)(Convert.ToDouble(v) / 1048576.0); break; }
                }
            }
            catch { }
            try
            {
                ManagementObjectSearcher s = new ManagementObjectSearcher("select Name, AdapterRAM from Win32_VideoController");
                foreach (ManagementObject o in s.Get())
                {
                    object n = o["Name"];
                    if (n != null) { h.GpuName = n.ToString().Trim(); }
                    try
                    {
                        object r = o["AdapterRAM"];
                        if (r != null)
                        {
                            long b = Convert.ToInt64(r);
                            if (b > 0) { h.VramMB = b / 1048576; }
                        }
                    }
                    catch { }
                    break;
                }
            }
            catch { }
            return h;
        }

        public int RecommendedRamMB()
        {
            if (RamTotalMB <= 0) return 2048;
            if (RamTotalMB <= 4096) return 2048;
            if (RamTotalMB <= 8192) return 3072;
            if (RamTotalMB <= 16384) return 4096;
            return 6144;
        }

        public string Tier()
        {
            bool weakCpu = CpuCores <= 4;
            bool weakRam = RamTotalMB > 0 && RamTotalMB <= 6144;
            string g = GpuName.ToLowerInvariant();
            bool weakGpu = g.Contains("intel") && (g.Contains("hd ") || g.Contains("uhd")) || g.Contains("vega 3") || g.Contains("vega 6");
            if (weakCpu && (weakRam || weakGpu)) return "LOW";
            if (weakCpu || weakRam || weakGpu || VramMB == 3 * 1024 || (VramMB > 0 && VramMB < 4096)) return "MEDIUM";
            return "HIGH";
        }
    }

    public enum PerfProfile { MaxFps, Balanced, Quality, Custom }

    public sealed class PerfSettings
    {
        public PerfProfile Profile = PerfProfile.Balanced;
        public int RenderDistance = 8;
        public int SimulationDistance = 6;
        public bool FancyGraphics = false;
        public bool SmoothLighting = true;
        public bool Particles = true;
        public bool Vsync = false;
        public int MaxFps = 120;
        public bool ShadersAllowed = false;
        public string Backend = "opengl";
        public int AllocatedRamMB = 3072;

        public static PerfSettings ForProfile(PerfProfile p, HardwareInfo hw)
        {
            PerfSettings s = new PerfSettings();
            s.Profile = p;
            if (hw != null) s.AllocatedRamMB = hw.RecommendedRamMB();
            switch (p)
            {
                case PerfProfile.MaxFps:
                    s.RenderDistance = 5; s.SimulationDistance = 4; s.FancyGraphics = false;
                    s.SmoothLighting = false; s.Particles = false; s.Vsync = false;
                    s.MaxFps = 0; s.ShadersAllowed = false; break;
                case PerfProfile.Balanced:
                    s.RenderDistance = 8; s.SimulationDistance = 6; s.FancyGraphics = false;
                    s.SmoothLighting = true; s.Particles = true; s.Vsync = false;
                    s.MaxFps = 120; s.ShadersAllowed = false; break;
                case PerfProfile.Quality:
                    s.RenderDistance = 12; s.SimulationDistance = 8; s.FancyGraphics = true;
                    s.SmoothLighting = true; s.Particles = true; s.Vsync = true;
                    s.MaxFps = 60; s.ShadersAllowed = true; break;
                default:
                    break;
            }
            if (hw != null && hw.Tier() == "LOW" && p != PerfProfile.Custom)
            {
                s.RenderDistance = Math.Min(s.RenderDistance, 6);
                s.SimulationDistance = Math.Min(s.SimulationDistance, 4);
                s.ShadersAllowed = false;
            }
            return s;
        }
    }

    public sealed class AslanProfile
    {
        public string Name = "Standard";
        public string MinecraftVersion = "1.21.11";
        public string Loader = "fabric"; // vanilla | fabric | forge | neoforge | quilt
        public string LoaderVersion = "0.19.5";
        public int RamMB = 3072;
        public string PerfProfileName = "Balanced";
        public string JavaArgs = "";
        public bool SafeMode = false;
        public bool DisableCosmetics = false;
        public DateTime LastPlayed = DateTime.MinValue;
        public string ModpackName = "";
        public string ModpackVersion = "";
    }

    public sealed class AslanConfig
    {
        public List<AslanProfile> Profiles = new List<AslanProfile>();
        public string ActiveProfile = "Standard";
        public string PlayerName = "";
        public bool ReducedMotion = false;
        public int AnimSpeed = 100;
        public bool DiscordPresence = false;
        public bool HideHudOnScreenshot = true;
        public bool AutoSimplifyOnWeakHardware = true;
        public int GlowIntensity = 100;

        public AslanProfile GetActive()
        {
            for (int i = 0; i < Profiles.Count; i++)
                if (Profiles[i].Name == ActiveProfile) return Profiles[i];
            if (Profiles.Count == 0)
            {
                AslanProfile p = new AslanProfile();
                Profiles.Add(p);
                ActiveProfile = p.Name;
                return p;
            }
            return Profiles[0];
        }

        public static string ConfigPath()
        {
            return Path.Combine(AslanPaths.BaseDir, "aslan.json");
        }

        public void Save()
        {
            try { File.WriteAllText(ConfigPath(), Serialize(), Encoding.UTF8); }
            catch (Exception ex) { Logger.Warn("Config save failed: " + ex.Message); }
        }

        public static AslanConfig Load()
        {
            AslanConfig c = new AslanConfig();
            try
            {
                string path = ConfigPath();
                if (!File.Exists(path))
                {
                    // Migration aus VYRA versuchen
                    string legacy = Path.Combine(AslanPaths.LegacyDir, "vyra.json");
                    if (File.Exists(legacy))
                    {
                        string lj = File.ReadAllText(legacy, Encoding.UTF8);
                        MiniJson.ParseConfig(lj, c);
                    }
                    if (c.Profiles.Count == 0) c.Profiles.Add(new AslanProfile());
                    c.Save();
                    return c;
                }
                string json = File.ReadAllText(path, Encoding.UTF8);
                MiniJson.ParseConfig(json, c);
                if (c.Profiles.Count == 0) c.Profiles.Add(new AslanProfile());
            }
            catch (Exception ex)
            {
                Logger.Warn("Config load failed, using defaults: " + ex.Message);
                c = new AslanConfig();
                c.Profiles.Add(new AslanProfile());
            }
            return c;
        }

        public string Serialize()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("{\n");
            sb.Append("  \"activeProfile\": \"").Append(Escape(ActiveProfile)).Append("\",\n");
            sb.Append("  \"playerName\": \"").Append(Escape(PlayerName)).Append("\",\n");
            sb.Append("  \"reducedMotion\": ").Append(ReducedMotion ? "true" : "false").Append(",\n");
            sb.Append("  \"animSpeed\": ").Append(AnimSpeed).Append(",\n");
            sb.Append("  \"discordPresence\": ").Append(DiscordPresence ? "true" : "false").Append(",\n");
            sb.Append("  \"hideHudOnScreenshot\": ").Append(HideHudOnScreenshot ? "true" : "false").Append(",\n");
            sb.Append("  \"glow\": ").Append(GlowIntensity).Append(",\n");
            sb.Append("  \"profiles\": [\n");
            for (int i = 0; i < Profiles.Count; i++)
            {
                AslanProfile p = Profiles[i];
                sb.Append("    {\"name\":\"").Append(Escape(p.Name)).Append("\",");
                sb.Append("\"mc\":\"").Append(Escape(p.MinecraftVersion)).Append("\",");
                sb.Append("\"loader\":\"").Append(Escape(p.Loader)).Append("\",");
                sb.Append("\"loaderVersion\":\"").Append(Escape(p.LoaderVersion)).Append("\",");
                sb.Append("\"ram\":").Append(p.RamMB).Append(",");
                sb.Append("\"perf\":\"").Append(Escape(p.PerfProfileName)).Append("\",");
                sb.Append("\"javaArgs\":\"").Append(Escape(p.JavaArgs)).Append("\",");
                sb.Append("\"safeMode\":").Append(p.SafeMode ? "true" : "false").Append("}");
                if (i + 1 < Profiles.Count) sb.Append(",");
                sb.Append("\n");
            }
            sb.Append("  ]\n}\n");
            return sb.ToString();
        }

        private static string Escape(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
        }
    }

    public static class MiniJson
    {
        public static void ParseConfig(string json, AslanConfig c)
        {
            c.ActiveProfile = ExtractString(json, "activeProfile", c.ActiveProfile);
            c.PlayerName = ExtractString(json, "playerName", "");
            c.ReducedMotion = ExtractBool(json, "reducedMotion", false);
            c.DiscordPresence = ExtractBool(json, "discordPresence", false);
            c.HideHudOnScreenshot = ExtractBool(json, "hideHudOnScreenshot", true);
            string a = ExtractNumber(json, "animSpeed", "100");
            int n;
            if (int.TryParse(a, out n)) c.AnimSpeed = Math.Max(25, Math.Min(200, n));
            string gl = ExtractNumber(json, "glow", "100");
            if (int.TryParse(gl, out n)) c.GlowIntensity = Math.Max(0, Math.Min(150, n));
            c.Profiles.Clear();
            int arr = json.IndexOf("\"profiles\"");
            if (arr >= 0)
            {
                int i = json.IndexOf('[', arr);
                StringBuilder cur = new StringBuilder();
                bool inObj = false;
                int brace = 0;
                for (; i >= 0 && i < json.Length; i++)
                {
                    char ch = json[i];
                    if (ch == '{') { brace++; inObj = true; }
                    if (inObj) cur.Append(ch);
                    if (ch == '}')
                    {
                        brace--;
                        if (brace == 0 && inObj)
                        {
                            string o = cur.ToString();
                            cur.Length = 0;
                            inObj = false;
                            AslanProfile p = new AslanProfile();
                            p.Name = ExtractString(o, "name", "Standard");
                            p.MinecraftVersion = ExtractString(o, "mc", "1.21.11");
                            p.Loader = ExtractString(o, "loader", "fabric");
                            p.LoaderVersion = ExtractString(o, "loaderVersion", "0.19.5");
                            int ram;
                            if (!int.TryParse(ExtractNumber(o, "ram", "3072"), out ram)) ram = 3072;
                            p.RamMB = Math.Max(1024, Math.Min(16384, ram));
                            p.PerfProfileName = ExtractString(o, "perf", "Balanced");
                            p.JavaArgs = ExtractString(o, "javaArgs", "");
                            p.SafeMode = ExtractBool(o, "safeMode", false);
                            c.Profiles.Add(p);
                        }
                    }
                    if (ch == ']' && brace == 0 && cur.Length == 0 && i > arr + 20) { break; }
                }
            }
        }

        public static string ExtractString(string json, string key, string def)
        {
            try
            {
                string k = "\"" + key + "\"";
                int i = json.IndexOf(k);
                if (i < 0) return def;
                i = json.IndexOf(':', i);
                if (i < 0) return def;
                i++;
                while (i < json.Length && char.IsWhiteSpace(json[i])) i++;
                if (i >= json.Length || json[i] != '"') return def;
                i++;
                StringBuilder sb = new StringBuilder();
                while (i < json.Length)
                {
                    char ch = json[i];
                    if (ch == '\\' && i + 1 < json.Length)
                    {
                        char nx = json[i + 1];
                        if (nx == '"') sb.Append('"');
                        else if (nx == 'n') sb.Append('\n');
                        else if (nx == '\\') sb.Append('\\');
                        else sb.Append(nx);
                        i += 2;
                        continue;
                    }
                    if (ch == '"') break;
                    sb.Append(ch);
                    i++;
                }
                return sb.ToString();
            }
            catch { return def; }
        }

        public static bool ExtractBool(string json, string key, bool def)
        {
            try
            {
                string k = "\"" + key + "\"";
                int i = json.IndexOf(k);
                if (i < 0) return def;
                int c = json.IndexOf(':', i);
                if (c < 0) return def;
                string rest = json.Substring(c + 1).TrimStart();
                if (rest.StartsWith("true")) return true;
                if (rest.StartsWith("false")) return false;
                return def;
            }
            catch { return def; }
        }

        public static string ExtractNumber(string json, string key, string def)
        {
            try
            {
                string k = "\"" + key + "\"";
                int i = json.IndexOf(k);
                if (i < 0) return def;
                int c = json.IndexOf(':', i);
                if (c < 0) return def;
                c++;
                while (c < json.Length && char.IsWhiteSpace(json[c])) c++;
                StringBuilder sb = new StringBuilder();
                while (c < json.Length && (char.IsDigit(json[c]) || json[c] == '-')) { sb.Append(json[c]); c++; }
                if (sb.Length == 0) return def;
                return sb.ToString();
            }
            catch { return def; }
        }

        public static List<string> ExtractAllStrings(string json, string key)
        {
            List<string> out_ = new List<string>();
            try
            {
                int i = 0;
                string k = "\"" + key + "\"";
                while (true)
                {
                    i = json.IndexOf(k, i);
                    if (i < 0) break;
                    int c = json.IndexOf(':', i);
                    if (c < 0) break;
                    c++;
                    while (c < json.Length && char.IsWhiteSpace(json[c])) c++;
                    if (c < json.Length && json[c] == '"')
                    {
                        c++;
                        StringBuilder sb = new StringBuilder();
                        while (c < json.Length && json[c] != '"') { sb.Append(json[c]); c++; }
                        out_.Add(sb.ToString());
                    }
                    i = c + 1;
                }
            }
            catch { }
            return out_;
        }
    }

    // Loader-Kompatibilität: konservativ, live geprüft wo möglich, nie erfunden.
    public static class LoaderMatrix
    {
        public sealed class LoaderInfo
        {
            public string Id; // vanilla | fabric | forge | neoforge | quilt
            public string Title;
            public string Desc;
            public bool AutoInstall; // vollautomatisch (sonst assistiert)
        }

        public static readonly LoaderInfo[] All = new LoaderInfo[]
        {
            Info("vanilla", "Vanilla", "Offizielles Minecraft ohne Mod-Loader. Immer kompatibel.", true),
            Info("fabric", "Fabric", "Leichtgewichtig, schnell, große Mod-Auswahl. Volle Auto-Installation.", true),
            Info("quilt", "Quilt", "Fabric-Fork mit eigener Toolchain. Volle Auto-Installation.", true),
            Info("forge", "Forge", "Der Klassiker mit riesigem Mod-Katalog. Assistierte Installation.", false),
            Info("neoforge", "NeoForge", "Moderner Forge-Nachfolger. Assistierte Installation.", false),
        };

        private static LoaderInfo Info(string id, string t, string d, bool auto)
        {
            LoaderInfo i = new LoaderInfo();
            i.Id = id; i.Title = t; i.Desc = d; i.AutoInstall = auto;
            return i;
        }

        // Welche Loader für eine MC-Version sinnvoll sind (Versionsvergleich, keine Fantasie).
        public static List<string> SuggestFor(string mc)
        {
            List<string> l = new List<string>();
            l.Add("vanilla");
            l.Add("fabric");
            l.Add("quilt");
            // Forge/NeoForge nur für plausible MC-Versionen anbieten
            if (IsAtLeast(mc, 1, 16)) { l.Add("forge"); }
            if (IsAtLeast(mc, 1, 20)) { l.Add("neoforge"); }
            return l;
        }

        private static bool IsAtLeast(string mc, int major, int minor)
        {
            try
            {
                string[] p = mc.Split('.');
                int ma = int.Parse(p[0]);
                if (ma != 1 && ma >= 20) return true; // 26.x-Nummerierung: alles modern
                if (ma != major) return ma > major;
                if (p.Length < 2) return true;
                return int.Parse(p[1]) >= minor;
            }
            catch { return true; }
        }
    }

    public static class ModConflictRules
    {
        public static string CheckPair(string a, string b)
        {
            string x = (a + "|" + b).ToLowerInvariant();
            if (x.Contains("sodium") && x.Contains("optifine")) return "Sodium und OptiFine verändern beide das Rendering und sind inkompatibel.";
            if (x.Contains("optifabric") && x.Contains("sodium")) return "OptiFabric + Sodium ist instabil; stattdessen Iris für Shader nutzen.";
            if (x.Contains("phosphor") && x.Contains("lithium")) return "Phosphor ist veraltet; Lithium übernimmt diesen Bereich stabiler.";
            if (x.Contains("hydrogen") && x.Contains("ferrite")) return "Hydrogen ist veraltet; FerriteCore ist der stabile Ersatz.";
            if (x.Contains("iris") && x.Contains("optifine")) return "Iris und OptiFine schließen sich aus (beide Shader-Pipelines).";
            if (x.Contains("sodium") && x.Contains("canvas")) return "Sodium und Canvas Renderer sind zwei Render-Pipelines — nur eine wählen.";
            return null;
        }

        public static string CheckSet(List<string> ids)
        {
            for (int i = 0; i < ids.Count; i++)
                for (int j = i + 1; j < ids.Count; j++)
                {
                    string r = CheckPair(ids[i], ids[j]);
                    if (r != null) return ids[i] + " + " + ids[j] + ": " + r;
                }
            return null;
        }

        public static readonly string[] PerfSet = new string[]
        {
            "sodium", "lithium", "ferrite-core", "immediatelyfast",
            "entityculling", "moreculling", "dynamic-fps",
            "sodium-extra", "reeses-sodium-options", "modmenu"
        };
    }

    public sealed class BenchmarkResult
    {
        public double AvgFps;
        public double Low1Fps;
        public double AvgFrametimeMs;
        public long RamUsedMB;
        public int Samples;
        public TimeSpan LoadTime;
        public int Spikes;

        public static BenchmarkResult FromFpsSamples(List<double> fps, long ramMB, TimeSpan load)
        {
            BenchmarkResult r = new BenchmarkResult();
            r.Samples = fps.Count;
            r.RamUsedMB = ramMB;
            r.LoadTime = load;
            if (fps.Count == 0) return r;
            List<double> sorted = new List<double>(fps);
            sorted.Sort();
            double sum = 0;
            for (int i = 0; i < sorted.Count; i++) sum += sorted[i];
            r.AvgFps = sum / sorted.Count;
            int n = Math.Max(1, (int)Math.Ceiling(sorted.Count * 0.01));
            double lowSum = 0;
            for (int i = 0; i < n; i++) lowSum += sorted[i];
            r.Low1Fps = lowSum / n;
            double ftSum = 0;
            int spikes = 0;
            for (int i = 0; i < sorted.Count; i++)
            {
                double ft = sorted[i] > 0 ? 1000.0 / sorted[i] : 1000.0;
                ftSum += ft;
                if (ft > 50.0) spikes++;
            }
            r.AvgFrametimeMs = ftSum / sorted.Count;
            r.Spikes = spikes;
            return r;
        }
    }

    public static class DpapiStore
    {
        public static void Save(string name, string secret)
        {
            try
            {
                byte[] plain = Encoding.UTF8.GetBytes(secret);
                byte[] enc = ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser);
                File.WriteAllBytes(Path.Combine(AslanPaths.BaseDir, name), enc);
            }
            catch (Exception ex) { Logger.Warn("DPAPI save failed: " + ex.Message); }
        }

        public static string Load(string name)
        {
            try
            {
                string p = Path.Combine(AslanPaths.BaseDir, name);
                if (!File.Exists(p)) return null;
                byte[] enc = File.ReadAllBytes(p);
                byte[] plain = ProtectedData.Unprotect(enc, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plain);
            }
            catch { return null; }
        }

        public static void Delete(string name)
        {
            try
            {
                string p = Path.Combine(AslanPaths.BaseDir, name);
                if (File.Exists(p)) File.Delete(p);
            }
            catch { }
        }
    }
}

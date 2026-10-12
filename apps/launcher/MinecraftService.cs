using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace Aslan
{
    public sealed class JavaInstall
    {
        public string Path;
        public string Version;
        public override string ToString() { return Version + " — " + Path; }
    }

    public static class JavaFinder
    {
        public static List<JavaInstall> FindAll()
        {
            List<JavaInstall> list = new List<JavaInstall>();
            List<string> candidates = new List<string>();
            try
            {
                string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string prism = Path.Combine(appdata, "PrismLauncher", "java");
                if (Directory.Exists(prism))
                {
                    string[] exes = Directory.GetFiles(prism, "java.exe", SearchOption.AllDirectories);
                    for (int i = 0; i < exes.Length; i++) candidates.Add(exes[i]);
                }
                string alauncher = Path.Combine(appdata, "ASLANCLIENT", "java");
                if (Directory.Exists(alauncher))
                {
                    try
                    {
                        string[] exes = Directory.GetFiles(alauncher, "java.exe", SearchOption.AllDirectories);
                        for (int i = 0; i < exes.Length; i++) candidates.Add(exes[i]);
                    }
                    catch { }
                }
                string mc = Path.Combine(appdata, ".minecraft", "runtime");
                if (Directory.Exists(mc))
                {
                    try
                    {
                        string[] exes = Directory.GetFiles(mc, "java.exe", SearchOption.AllDirectories);
                        for (int i = 0; i < exes.Length; i++) candidates.Add(exes[i]);
                    }
                    catch { }
                }
            }
            catch { }
            string[] roots = new string[] {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles) ,
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            };
            string[] vendors = new string[] { "Eclipse Adoptium", "Microsoft", "Java", "Zulu", "BellSoft" };
            for (int r = 0; r < roots.Length; r++)
            {
                for (int v = 0; v < vendors.Length; v++)
                {
                    try
                    {
                        string d = Path.Combine(roots[r], vendors[v]);
                        if (!Directory.Exists(d)) continue;
                        string[] exes = Directory.GetFiles(d, "java.exe", SearchOption.AllDirectories);
                        for (int i = 0; i < exes.Length && list.Count + candidates.Count < 12; i++) candidates.Add(exes[i]);
                    }
                    catch { }
                }
            }
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("where", "java");
                psi.UseShellExecute = false;
                psi.RedirectStandardOutput = true;
                psi.CreateNoWindow = true;
                Process p = Process.Start(psi);
                string so = p.StandardOutput.ReadToEnd();
                p.WaitForExit(5000);
                string[] lines = so.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < lines.Length; i++)
                {
                    string t = lines[i].Trim();
                    if (t.Length > 0 && File.Exists(t) && candidates.IndexOf(t) < 0) candidates.Add(t);
                }
            }
            catch { }
            for (int i = 0; i < candidates.Count; i++)
            {
                try
                {
                    JavaInstall ji = new JavaInstall();
                    ji.Path = candidates[i];
                    ji.Version = ProbeVersion(candidates[i]);
                    list.Add(ji);
                }
                catch { }
            }
            return list;
        }

        public static string ProbeVersion(string javaExe)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(javaExe, "-version");
                psi.UseShellExecute = false;
                psi.RedirectStandardError = true;
                psi.RedirectStandardOutput = true;
                psi.CreateNoWindow = true;
                Process p = Process.Start(psi);
                string err = p.StandardError.ReadToEnd();
                string so = p.StandardOutput.ReadToEnd();
                p.WaitForExit(8000);
                string all = (so + "\n" + err).Trim();
                string[] lines = all.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 0) return lines[0].Trim();
                return "unknown";
            }
            catch { return "unknown"; }
        }

        public static int MajorOf(string versionLine)
        {
            try
            {
                int q1 = versionLine.IndexOf('"');
                int q2 = versionLine.IndexOf('"', q1 + 1);
                if (q1 < 0 || q2 < 0) return 0;
                string v = versionLine.Substring(q1 + 1, q2 - q1 - 1);
                if (v.StartsWith("1.8")) return 8;
                int dot = v.IndexOf('.');
                string head = dot > 0 ? v.Substring(0, dot) : v;
                int dash = head.IndexOf('-');
                if (dash > 0) head = head.Substring(0, dash);
                int m;
                if (int.TryParse(head, out m)) return m;
                return 0;
            }
            catch { return 0; }
        }

        public static string RepairHint(int needMajor, List<JavaInstall> found)
        {
            int best = 0;
            for (int i = 0; i < found.Count; i++)
                best = Math.Max(best, MajorOf(found[i].Version));
            if (best >= needMajor) return null;
            return "Kein Java " + needMajor + "+ gefunden (bestes: " + (best == 0 ? "keins" : ("Java " + best)) + "). " +
                "Bitte ein OpenJDK " + needMajor + " (LTS) installieren.";
        }
    }

    public static class Http
    {
        public static string Get(string url, int timeoutMs)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            req.Timeout = timeoutMs;
            req.UserAgent = "ASLAN-Client/0.3 (+independent project)";
            req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            using (Stream s = resp.GetResponseStream())
            using (StreamReader r = new StreamReader(s, Encoding.UTF8))
            {
                return r.ReadToEnd();
            }
        }

        public static void Download(string url, string dest, DownloadProgress prog, CancellationToken ct)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            req.Timeout = 30000;
            req.UserAgent = "ASLAN-Client/0.3 (+independent project)";
            req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            using (Stream src = resp.GetResponseStream())
            {
                long total = resp.ContentLength;
                string tmp = dest + ".part";
                using (FileStream fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    byte[] buf = new byte[65536];
                    long done = 0;
                    while (true)
                    {
                        if (ct.IsCancellationRequested) throw new OperationCanceledException();
                        int n = src.Read(buf, 0, buf.Length);
                        if (n <= 0) break;
                        fs.Write(buf, 0, n);
                        done += n;
                        if (prog != null) prog.Report(done, total);
                    }
                }
                if (File.Exists(dest)) File.Delete(dest);
                File.Move(tmp, dest);
            }
        }
    }

    public sealed class DownloadProgress
    {
        public long Done;
        public long Total;
        public string Label;
        public long LastTime;
        public long LastDone;
        public double SpeedBps;
        public void Report(long done, long total)
        {
            long now = DateTime.UtcNow.Ticks / 10000L;
            if (LastTime > 0 && now > LastTime)
            {
                double dt = (now - LastTime) / 1000.0;
                if (dt > 0.2) { SpeedBps = (done - LastDone) / dt; LastTime = now; LastDone = done; }
            }
            else { LastTime = now; LastDone = done; }
            Done = done; Total = total;
        }
        public int Percent()
        {
            if (Total <= 0) return 0;
            return (int)(Done * 100L / Total);
        }
        public string SpeedText()
        {
            if (SpeedBps < 1024) return ((int)SpeedBps) + " B/s";
            if (SpeedBps < 1048576) return (SpeedBps / 1024.0).ToString("0.0") + " KB/s";
            return (SpeedBps / 1048576.0).ToString("0.0") + " MB/s";
        }
    }

    public sealed class McVersionEntry
    {
        public string Id;
        public string Type;
        public string Url;
    }

    public static class MinecraftService
    {
        public const string VersionManifest = "https://piston-meta.mojang.com/mc/game/version_manifest_v2.json";
        public const string FabricMeta = "https://meta.fabricmc.net/v2/versions/loader/";
        public const string QuiltMeta = "https://meta.quiltmc.org/v3/versions/loader/";
        public const string ModMinecraftVersion = "1.21.11"; // ASLAN-Mod-Ziel (voller Fabric-Stack)

        public static List<McVersionEntry> GetVersions(bool snapshots)
        {
            string json = Http.Get(VersionManifest, 20000);
            List<McVersionEntry> list = new List<McVersionEntry>();
            int vi = json.IndexOf("\"versions\"");
            if (vi < 0) return list;
            int i = json.IndexOf('[', vi);
            int brace = 0;
            StringBuilder cur = new StringBuilder();
            bool inObj = false;
            for (; i >= 0 && i < json.Length && list.Count < 600; i++)
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
                        McVersionEntry e = new McVersionEntry();
                        e.Id = MiniJson.ExtractString(o, "id", null);
                        e.Type = MiniJson.ExtractString(o, "type", "");
                        e.Url = MiniJson.ExtractString(o, "url", null);
                        if (e.Id != null && e.Url != null)
                        {
                            if (e.Type == "release" || (snapshots && e.Type == "snapshot")) list.Add(e);
                        }
                    }
                }
                if (ch == ']' && brace == 0 && list.Count > 0) break;
            }
            return list;
        }

        public static string GetLatestRelease(List<McVersionEntry> all)
        {
            for (int i = 0; i < all.Count; i++)
                if (all[i].Type == "release") return all[i].Id;
            return null;
        }

        public static string EnsureVersionJson(string mcVersion)
        {
            string dir = Path.Combine(AslanPaths.VersionsDir, mcVersion);
            Directory.CreateDirectory(dir);
            string dest = Path.Combine(dir, mcVersion + ".json");
            if (File.Exists(dest)) return dest;
            List<McVersionEntry> all = GetVersions(true);
            string url = null;
            for (int i = 0; i < all.Count; i++)
                if (all[i].Id == mcVersion) url = all[i].Url;
            if (url == null) throw new Exception("Minecraft-Version " + mcVersion + " nicht im Mojang-Manifest gefunden.");
            string json = Http.Get(url, 20000);
            File.WriteAllText(dest, json, Encoding.UTF8);
            return dest;
        }

        public static string SafeName(string s)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (char.IsLetterOrDigit(c) || c == '-' || c == '_') sb.Append(c);
                else if (c == ' ') sb.Append('-');
            }
            if (sb.Length == 0) return "profil";
            return sb.ToString();
        }

        public static List<string> ReadCrashHints(string instanceDir)
        {
            List<string> hints = new List<string>();
            try
            {
                string crashDir = Path.Combine(instanceDir, "crash-reports");
                if (Directory.Exists(crashDir))
                {
                    string[] files = Directory.GetFiles(crashDir, "*.txt");
                    Array.Sort(files);
                    for (int i = Math.Max(0, files.Length - 3); i < files.Length; i++)
                        hints.Add(Path.GetFileName(files[i]) + ": " + CrashAnalyzer.Summarize(File.ReadAllText(files[i])));
                }
            }
            catch { }
            return hints;
        }
    }
}

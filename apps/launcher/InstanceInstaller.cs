using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Vyra
{
    public sealed class InstallProgress
    {
        public string Phase = "";
        public string CurrentFile = "";
        public long DoneFiles;
        public long TotalFiles;
        public long DoneBytes;
        public long TotalBytes;
        public bool CancelRequested;

        public int Percent()
        {
            if (TotalBytes > 0) return (int)(DoneBytes * 100L / TotalBytes);
            if (TotalFiles > 0) return (int)(DoneFiles * 100L / TotalFiles);
            return 0;
        }

        public string Text()
        {
            if (TotalFiles > 0) return Phase + " — " + DoneFiles + "/" + TotalFiles + " Dateien (" + Percent() + " %)";
            return Phase + " — " + Percent() + " %";
        }
    }

    internal static class JsonScan
    {
        // Teilt ein JSON-Array (ab Index von '[') in seine Top-Level-Objekte.
        public static List<string> SplitObjects(string json, string arrayKey, int max)
        {
            List<string> list = new List<string>();
            int k = json.IndexOf("\"" + arrayKey + "\"");
            if (k < 0) return list;
            int i = json.IndexOf('[', k);
            if (i < 0) return list;
            i++;
            int brace = 0;
            StringBuilder cur = new StringBuilder();
            bool inStr = false;
            bool esc = false;
            for (; i < json.Length && list.Count < max; i++)
            {
                char ch = json[i];
                if (inStr)
                {
                    cur.Append(ch);
                    if (esc) esc = false;
                    else if (ch == '\\') esc = true;
                    else if (ch == '"') inStr = false;
                    continue;
                }
                if (ch == '"') { inStr = true; if (brace > 0) cur.Append(ch); continue; }
                if (ch == '{') { brace++; }
                if (brace > 0) cur.Append(ch);
                if (ch == '}')
                {
                    brace--;
                    if (brace == 0)
                    {
                        list.Add(cur.ToString());
                        cur.Length = 0;
                    }
                }
                if (ch == ']' && brace == 0) break;
            }
            return list;
        }

        public static string Block(string obj, string key)
        {
            int k = obj.IndexOf("\"" + key + "\"");
            if (k < 0) return null;
            int c = obj.IndexOf(':', k);
            if (c < 0) return null;
            c++;
            while (c < obj.Length && char.IsWhiteSpace(obj[c])) c++;
            if (c >= obj.Length) return null;
            char open = obj[c];
            char close = open == '{' ? '}' : (open == '[' ? ']' : '\0');
            if (close == '\0') return null;
            int depth = 0;
            bool inStr = false;
            bool esc = false;
            for (int i = c; i < obj.Length; i++)
            {
                char ch = obj[i];
                if (inStr)
                {
                    if (esc) esc = false;
                    else if (ch == '\\') esc = true;
                    else if (ch == '"') inStr = false;
                    continue;
                }
                if (ch == '"') { inStr = true; continue; }
                if (ch == open) depth++;
                else if (ch == close)
                {
                    depth--;
                    if (depth == 0) return obj.Substring(c, i - c + 1);
                }
            }
            return null;
        }
    }

    public sealed class LibEntry
    {
        public string Name;
        public string Path;
        public string Sha1;
        public long Size;
        public string Url;
        public string NativesKey;
        public string RulesBlock;
    }

    public static class HashCheck
    {
        public static string Sha1Of(string file)
        {
            using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (SHA1Managed h = new SHA1Managed())
            {
                byte[] b = h.ComputeHash(fs);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < b.Length; i++) sb.Append(b[i].ToString("x2"));
                return sb.ToString();
            }
        }

        public static string Sha512Of(string file)
        {
            using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (SHA512Managed h = new SHA512Managed())
            {
                byte[] b = h.ComputeHash(fs);
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < b.Length; i++) sb.Append(b[i].ToString("x2"));
                return sb.ToString();
            }
        }

        public static bool Matches(string file, string wantSha1)
        {
            try
            {
                if (wantSha1 == null || wantSha1.Length == 0) return File.Exists(file);
                if (!File.Exists(file)) return false;
                return string.Equals(Sha1Of(file), wantSha1, StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }
    }

    public static class InstanceInstaller
    {
        public static string InstanceDir(GameProfile p)
        {
            string d = Path.Combine(VyraPaths.ProfilesDir, MinecraftService.SafeName(p.Name));
            Directory.CreateDirectory(d);
            return d;
        }

        public static string NativesDir(GameProfile p, string mcVersion)
        {
            string d = Path.Combine(InstanceDir(p), "natives", mcVersion);
            Directory.CreateDirectory(d);
            return d;
        }

        // Regeln (Mojang-Semantik, verifiziert gegen echtes 26.2-JSON):
        // - keine Regeln -> erlaubt
        // - mit Regeln -> letzte zutreffende Regel entscheidet
        // - keine zutreffende Regel -> NICHT erlaubt (z. B. osx-only Natives auf Windows)
        // - features-Regeln (demo/quickplay) treffen bei VYRA nie zu
        // - os.versionRange {min,max} wird numerisch verglichen
        public static bool AllowedByRules(string rulesArrayOrNull)
        {
            if (rulesArrayOrNull == null) return true;
            bool allowed = false;
            bool anyMatch = false;
            List<string> rules = JsonScan.SplitObjects("{\"rules\":" + rulesArrayOrNull + "}", "rules", 50);
            for (int i = 0; i < rules.Count; i++)
            {
                string r = rules[i];
                bool match = true;
                if (r.IndexOf("\"features\"") >= 0) match = false;
                string os = JsonScan.Block(r, "os");
                if (os != null && match)
                {
                    string name = MiniJson.ExtractString(os, "name", null);
                    if (name != null && name != "windows") match = false;
                    string ver = MiniJson.ExtractString(os, "version", null);
                    if (ver != null)
                    {
                        try
                        {
                            string osv = Environment.OSVersion.Version.ToString();
                            if (!Regex.IsMatch(osv, ver)) match = false;
                        }
                        catch { }
                    }
                    string vr = JsonScan.Block(os, "versionRange");
                    if (vr != null && !VersionInRange(Environment.OSVersion.Version, vr)) match = false;
                    string arch = MiniJson.ExtractString(os, "arch", null);
                    if (arch != null && arch != "64" && arch != "x64") match = false;
                }
                if (match)
                {
                    anyMatch = true;
                    string action = MiniJson.ExtractString(r, "action", "allow");
                    allowed = (action == "allow");
                }
            }
            if (!anyMatch) return false;
            return allowed;
        }

        private static bool VersionInRange(Version osv, string rangeBlock)
        {
            try
            {
                string min = MiniJson.ExtractString(rangeBlock, "min", null);
                string max = MiniJson.ExtractString(rangeBlock, "max", null);
                if (min != null && CompareVersions(osv, min) < 0) return false;
                if (max != null && CompareVersions(osv, max) > 0) return false;
                return true;
            }
            catch { return true; }
        }

        private static int CompareVersions(Version a, string b)
        {
            string[] parts = b.Split('.');
            int[] av = new int[] { a.Major, a.Minor, a.Build, a.Revision };
            for (int i = 0; i < parts.Length && i < 4; i++)
            {
                int bn;
                if (!int.TryParse(parts[i], out bn)) bn = 0;
                int an = av[i] < 0 ? 0 : av[i];
                if (an != bn) return an > bn ? 1 : -1;
            }
            return 0;
        }

        public static int RequiredJavaMajor(string versionJson)
        {
            try
            {
                string jb = JsonScan.Block(versionJson, "javaVersion");
                if (jb != null)
                {
                    int n;
                    if (int.TryParse(MiniJson.ExtractNumber(jb, "majorVersion", "21"), out n)) return n;
                }
            }
            catch { }
            return 21;
        }

        public static List<LibEntry> ParseLibraries(string versionJson)
        {
            List<LibEntry> libs = new List<LibEntry>();
            List<string> objs = JsonScan.SplitObjects(versionJson, "libraries", 500);
            for (int i = 0; i < objs.Count; i++)
            {
                string o = objs[i];
                LibEntry e = new LibEntry();
                e.Name = MiniJson.ExtractString(o, "name", null);
                if (e.Name == null) continue;
                int rb = o.IndexOf("\"rules\"");
                e.RulesBlock = rb >= 0 ? JsonScan.Block(o, "rules") : null;
                string dl = JsonScan.Block(o, "downloads");
                if (dl != null)
                {
                    string art = JsonScan.Block(dl, "artifact");
                    if (art != null)
                    {
                        e.Path = MiniJson.ExtractString(art, "path", null);
                        e.Sha1 = MiniJson.ExtractString(art, "sha1", null);
                        string sz = MiniJson.ExtractNumber(art, "size", "0");
                        long n;
                        if (long.TryParse(sz, out n)) e.Size = n;
                        e.Url = MiniJson.ExtractString(art, "url", null);
                    }
                }
                string nat = JsonScan.Block(o, "natives");
                if (nat != null)
                {
                    e.NativesKey = MiniJson.ExtractString(nat, "windows", null);
                    if (e.NativesKey != null) e.NativesKey = e.NativesKey.Replace("${arch}", "64");
                }
                if (e.Path == null)
                {
                    e.Path = MavenPath(e.Name, ".jar");
                    e.Url = "https://libraries.minecraft.net/" + e.Path;
                }
                libs.Add(e);
            }
            return libs;
        }

        public static bool IsNativesLib(LibEntry e)
        {
            if (e == null || e.Name == null) return false;
            return e.Name.ToLowerInvariant().Contains(":natives-");
        }

        public static string MavenPath(string coords, string ext)
        {
            string[] parts = coords.Split(':');
            string group = parts[0].Replace('.', '/');
            string art = parts[1];
            string ver = parts[2];
            string cls = parts.Length > 3 ? "-" + parts[3].Split('@')[0] : "";
            string file = art + "-" + ver + cls + ext;
            return group + "/" + art + "/" + ver + "/" + file;
        }

        private static void ThrowIfCancel(InstallProgress pr)
        {
            if (pr != null && pr.CancelRequested) throw new OperationCanceledException();
        }

        private static void Dl(string url, string dest, InstallProgress pr, CancellationToken ct)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(dest));
            DownloadProgress dp = new DownloadProgress();
            dp.Label = Path.GetFileName(dest);
            if (pr != null) pr.CurrentFile = dp.Label;
            Http.Download(url, dest, dp, ct);
            if (pr != null)
            {
                pr.DoneFiles++;
                try { pr.DoneBytes += new FileInfo(dest).Length; } catch { }
            }
        }

        public static void EnsureInstalled(GameProfile p, string loaderVersion, InstallProgress pr, CancellationToken ct)
        {
            if (pr != null) { pr.Phase = "Versionsmanifest"; }
            string versionJsonPath = MinecraftService.EnsureVersionJson(p.MinecraftVersion);
            string vj = File.ReadAllText(versionJsonPath, Encoding.UTF8);

            if (pr != null) pr.Phase = "Client-JAR";
            string dlBlock = JsonScan.Block(vj, "downloads");
            string clientBlock = dlBlock != null ? JsonScan.Block(dlBlock, "client") : null;
            string clientDir = Path.Combine(VyraPaths.VersionsDir, p.MinecraftVersion);
            string clientJar = Path.Combine(clientDir, p.MinecraftVersion + ".jar");
            if (clientBlock != null)
            {
                string url = MiniJson.ExtractString(clientBlock, "url", null);
                string sha1 = MiniJson.ExtractString(clientBlock, "sha1", null);
                if (url != null && !HashCheck.Matches(clientJar, sha1))
                {
                    if (pr != null) pr.TotalFiles++;
                    Dl(url, clientJar, pr, ct);
                    if (!HashCheck.Matches(clientJar, sha1)) throw new Exception("Client-JAR Hash mismatch.");
                }
            }

            List<LibEntry> libs = ParseLibraries(vj);
            List<LibEntry> wanted = new List<LibEntry>();
            for (int i = 0; i < libs.Count; i++)
                if (AllowedByRules(libs[i].RulesBlock)) wanted.Add(libs[i]);
            if (pr != null) { pr.Phase = "Libraries"; pr.TotalFiles += wanted.Count; }
            string nativesDir = NativesDir(p, p.MinecraftVersion);
            for (int i = 0; i < wanted.Count; i++)
            {
                ThrowIfCancel(pr);
                LibEntry e = wanted[i];
                string dest = Path.Combine(VyraPaths.LibrariesDir, e.Path.Replace('/', Path.DirectorySeparatorChar));
                if (e.NativesKey != null)
                {
                    string o = FindLibObject(vj, e.Name);
                    string cl = o != null ? JsonScan.Block(o, "downloads") : null;
                    cl = cl != null ? JsonScan.Block(cl, "classifiers") : null;
                    string nativeObj = ExtractClassifier(cl, e.NativesKey);
                    if (nativeObj != null)
                    {
                        string npath = MiniJson.ExtractString(nativeObj, "path", null);
                        string nsha = MiniJson.ExtractString(nativeObj, "sha1", null);
                        string nurl = MiniJson.ExtractString(nativeObj, "url", null);
                        string ndest = Path.Combine(VyraPaths.LibrariesDir, (npath ?? e.Path).Replace('/', Path.DirectorySeparatorChar));
                        if (nurl != null && !HashCheck.Matches(ndest, nsha)) Dl(nurl, ndest, pr, ct);
                        if (File.Exists(ndest)) ExtractNatives(ndest, nativesDir);
                        else if (pr != null) pr.DoneFiles++;
                    }
                    else if (pr != null) pr.DoneFiles++;
                    continue;
                }
                if (e.Url != null && !HashCheck.Matches(dest, e.Sha1)) Dl(e.Url, dest, pr, ct);
                else if (pr != null) pr.DoneFiles++;
                // Natives im neuen Stil (Classifier :natives-*) zusätzlich extrahieren
                // (sie bleiben zusätzlich auf dem Classpath — wie beim Vanilla-Launcher).
                if (IsNativesLib(e) && File.Exists(dest)) ExtractNatives(dest, nativesDir);
            }

            if (pr != null) pr.Phase = "Assets";
            string aiBlock = JsonScan.Block(vj, "assetIndex");
            if (aiBlock != null)
            {
                string aid = MiniJson.ExtractString(aiBlock, "id", null);
                string asha = MiniJson.ExtractString(aiBlock, "sha1", null);
                string aurl = MiniJson.ExtractString(aiBlock, "url", null);
                string idxPath = Path.Combine(VyraPaths.AssetsDir, "indexes", aid + ".json");
                if (aurl != null && !HashCheck.Matches(idxPath, asha))
                {
                    if (pr != null) pr.TotalFiles++;
                    Dl(aurl, idxPath, pr, ct);
                }
                string idx = File.ReadAllText(idxPath, Encoding.UTF8);
                string objects = JsonScan.Block(idx, "objects");
                List<string[]> assets = ParseAssetObjects(objects);
                if (pr != null) pr.TotalFiles += assets.Count;
                for (int i = 0; i < assets.Count; i++)
                {
                    ThrowIfCancel(pr);
                    string hash = assets[i][0];
                    string sub = Path.Combine(VyraPaths.AssetsDir, "objects", hash.Substring(0, 2), hash);
                    if (!HashCheck.Matches(sub, hash))
                        Dl("https://resources.download.minecraft.net/" + hash.Substring(0, 2) + "/" + hash, sub, pr, ct);
                    else if (pr != null) pr.DoneFiles++;
                }
            }

            try
            {
                string lg = JsonScan.Block(vj, "logging");
                if (lg != null)
                {
                    string cl = JsonScan.Block(lg, "client");
                    string file = cl != null ? JsonScan.Block(cl, "file") : null;
                    if (file != null)
                    {
                        string id = MiniJson.ExtractString(file, "id", null);
                        string sha = MiniJson.ExtractString(file, "sha1", null);
                        string url = MiniJson.ExtractString(file, "url", null);
                        string dest = Path.Combine(VyraPaths.AssetsDir, "log_configs", id);
                        if (url != null && id != null && !HashCheck.Matches(dest, sha))
                        {
                            if (pr != null) { pr.Phase = "Logging-Config"; pr.TotalFiles++; }
                            Dl(url, dest, pr, ct);
                        }
                    }
                }
            }
            catch { }

            if (p.Loader == "fabric")
            {
                if (pr != null) pr.Phase = "Fabric";
                EnsureFabric(p, loaderVersion, pr, ct);
            }

            // VYRA-Mod nur in Instanzen ihrer Zielversion einlegen (ehrlich, kein Fake).
            try
            {
                string selfMod = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vyra-client.jar");
                if (File.Exists(selfMod))
                {
                    if (p.MinecraftVersion == MinecraftService.ModMinecraftVersion)
                    {
                        string dest = Path.Combine(InstanceDir(p), "mods", "vyra-client.jar");
                        Directory.CreateDirectory(Path.GetDirectoryName(dest));
                        File.Copy(selfMod, dest, true);
                    }
                    else Logger.Info("VYRA-Mod: Instanz ist MC " + p.MinecraftVersion + ", Mod braucht " + MinecraftService.ModMinecraftVersion + " — übersprungen.");
                }
            }
            catch (Exception ex) { Logger.Warn("Mod copy: " + ex.Message); }

            if (pr != null) { pr.Phase = "Fertig"; pr.CurrentFile = ""; }
        }

        private static string FindLibObject(string vj, string name)
        {
            List<string> objs = JsonScan.SplitObjects(vj, "libraries", 500);
            for (int i = 0; i < objs.Count; i++)
                if (MiniJson.ExtractString(objs[i], "name", "") == name) return objs[i];
            return null;
        }

        private static string ExtractClassifier(string classifiersBlockOrNull, string keyPrefix)
        {
            if (classifiersBlockOrNull == null) return null;
            int i = classifiersBlockOrNull.IndexOf("\"" + keyPrefix + "\"");
            if (i < 0)
            {
                i = classifiersBlockOrNull.IndexOf("\"natives-");
                if (i < 0) return null;
            }
            int c = classifiersBlockOrNull.IndexOf(':', i);
            int o = classifiersBlockOrNull.IndexOf('{', c);
            if (o < 0) return null;
            int depth = 0;
            for (int k = o; k < classifiersBlockOrNull.Length; k++)
            {
                if (classifiersBlockOrNull[k] == '{') depth++;
                else if (classifiersBlockOrNull[k] == '}')
                {
                    depth--;
                    if (depth == 0) return classifiersBlockOrNull.Substring(o, k - o + 1);
                }
            }
            return null;
        }

        private static void ExtractNatives(string jar, string nativesDir)
        {
            try
            {
                using (FileStream fs = new FileStream(jar, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Read))
                {
                    foreach (ZipArchiveEntry e in zip.Entries)
                    {
                        if (e.FullName.StartsWith("META-INF")) continue;
                        if (e.FullName.EndsWith("/")) continue;
                        string dest = Path.Combine(nativesDir, e.FullName.Replace('/', Path.DirectorySeparatorChar));
                        Directory.CreateDirectory(Path.GetDirectoryName(dest));
                        using (Stream s = e.Open())
                        using (FileStream o = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            s.CopyTo(o);
                        }
                    }
                }
            }
            catch (Exception ex) { Logger.Warn("Natives extract: " + ex.Message); }
        }

        private static List<string[]> ParseAssetObjects(string objectsBlockOrNull)
        {
            List<string[]> list = new List<string[]>();
            if (objectsBlockOrNull == null) return list;
            Regex r = new Regex("\"hash\"\\s*:\\s*\"([0-9a-f]{40})\"", RegexOptions.Compiled);
            MatchCollection ms = r.Matches(objectsBlockOrNull);
            foreach (Match m in ms)
            {
                list.Add(new string[] { m.Groups[1].Value });
                if (list.Count > 20000) break;
            }
            return list;
        }

        public static void EnsureFabric(GameProfile p, string loaderVersion, InstallProgress pr, CancellationToken ct)
        {
            string loader = loaderVersion != null && loaderVersion.Length > 0 ? loaderVersion : p.LoaderVersion;
            string meta = Http.Get("https://meta.fabricmc.net/v2/versions/loader/" + Uri.EscapeDataString(p.MinecraftVersion) + "/" + Uri.EscapeDataString(loader), 20000);
            string lm = JsonScan.Block(meta, "launcherMeta");
            string mcBlock = lm != null ? JsonScan.Block(lm, "mainClass") : null;
            string mainClient = mcBlock != null ? MiniJson.ExtractString(mcBlock, "client", "net.fabricmc.loader.impl.launch.knot.KnotClient") : "net.fabricmc.loader.impl.launch.knot.KnotClient";
            File.WriteAllText(Path.Combine(InstanceDir(p), "fabric-mainclass.txt"), mainClient, Encoding.UTF8);

            string libsBlock = lm != null ? JsonScan.Block(lm, "libraries") : null;
            List<string> allLibObjs = new List<string>();
            if (libsBlock != null)
            {
                string common = JsonScan.Block(libsBlock, "common");
                string client = JsonScan.Block(libsBlock, "client");
                if (common != null) allLibObjs.AddRange(JsonScan.SplitObjects("{\"libraries\":" + common + "}", "libraries", 200));
                if (client != null) allLibObjs.AddRange(JsonScan.SplitObjects("{\"libraries\":" + client + "}", "libraries", 200));
            }
            string loaderCoords = "net.fabricmc:fabric-loader:" + loader;
            allLibObjs.Add("{\"name\":\"" + loaderCoords + "\"}");
            string im = Http.Get("https://meta.fabricmc.net/v2/versions/intermediary/" + Uri.EscapeDataString(p.MinecraftVersion), 20000);
            string imMaven = null;
            {
                Regex r = new Regex("\"maven\"\\s*:\\s*\"([^\"]+)\"");
                Match m = r.Match(im);
                if (m.Success) imMaven = m.Groups[1].Value;
            }
            if (imMaven != null) allLibObjs.Add("{\"name\":\"" + imMaven + "\"}");

            if (pr != null) pr.TotalFiles += allLibObjs.Count;
            for (int i = 0; i < allLibObjs.Count; i++)
            {
                ThrowIfCancel(pr);
                string name = MiniJson.ExtractString(allLibObjs[i], "name", null);
                string url = MiniJson.ExtractString(allLibObjs[i], "url", "https://maven.fabricmc.net/");
                if (name == null) { if (pr != null) pr.DoneFiles++; continue; }
                if (!url.EndsWith("/")) url += "/";
                string rel = MavenPath(name, ".jar");
                string dest = Path.Combine(VyraPaths.LibrariesDir, rel.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(dest) || new FileInfo(dest).Length < 1000)
                    Dl(url + rel, dest, pr, ct);
                else if (pr != null) pr.DoneFiles++;
            }
        }

        public static List<string> Verify(GameProfile p)
        {
            List<string> problems = new List<string>();
            try
            {
                string versionJsonPath = Path.Combine(VyraPaths.VersionsDir, p.MinecraftVersion, p.MinecraftVersion + ".json");
                if (!File.Exists(versionJsonPath)) { problems.Add("Versions-JSON fehlt: " + p.MinecraftVersion); return problems; }
                string vj = File.ReadAllText(versionJsonPath, Encoding.UTF8);
                string dlBlock = JsonScan.Block(vj, "downloads");
                string clientBlock = dlBlock != null ? JsonScan.Block(dlBlock, "client") : null;
                string clientJar = Path.Combine(VyraPaths.VersionsDir, p.MinecraftVersion, p.MinecraftVersion + ".jar");
                if (clientBlock != null)
                {
                    string sha1 = MiniJson.ExtractString(clientBlock, "sha1", null);
                    if (!HashCheck.Matches(clientJar, sha1)) problems.Add("Client-JAR fehlt oder Hash falsch.");
                }
                List<LibEntry> libs = ParseLibraries(vj);
                int missing = 0;
                for (int i = 0; i < libs.Count; i++)
                {
                    if (!AllowedByRules(libs[i].RulesBlock)) continue;
                    if (libs[i].NativesKey != null) continue;
                    string dest = Path.Combine(VyraPaths.LibrariesDir, libs[i].Path.Replace('/', Path.DirectorySeparatorChar));
                    if (!HashCheck.Matches(dest, libs[i].Sha1)) missing++;
                }
                if (missing > 0) problems.Add(missing + " Libraries fehlen oder sind beschädigt.");
                string natDir = NativesDir(p, p.MinecraftVersion);
                if (!Directory.Exists(natDir) || Directory.GetFiles(natDir).Length == 0)
                    problems.Add("Natives fehlen.");
            }
            catch (Exception ex) { problems.Add("Verify-Fehler: " + ex.Message); }
            return problems;
        }

        public sealed class LaunchPlan
        {
            public string JavaExe;
            public string Arguments;
            public string WorkDir;
            public string MainClass;
        }

        public static LaunchPlan BuildPlan(GameProfile p, string playerName, string uuid, string accessToken, string javaExe, string loaderVersion)
        {
            string versionJsonPath = Path.Combine(VyraPaths.VersionsDir, p.MinecraftVersion, p.MinecraftVersion + ".json");
            string vj = File.ReadAllText(versionJsonPath, Encoding.UTF8);
            string instDir = InstanceDir(p);
            string nativesDir = NativesDir(p, p.MinecraftVersion);
            string assetsRoot = VyraPaths.AssetsDir;

            string assetId = "legacy";
            try
            {
                string ai = JsonScan.Block(vj, "assetIndex");
                if (ai != null) assetId = MiniJson.ExtractString(ai, "id", "legacy");
            }
            catch { }

            List<string> cp = new List<string>();
            cp.Add(Path.Combine(VyraPaths.VersionsDir, p.MinecraftVersion, p.MinecraftVersion + ".jar"));
            List<LibEntry> libs = ParseLibraries(vj);
            for (int i = 0; i < libs.Count; i++)
            {
                if (!AllowedByRules(libs[i].RulesBlock)) continue;
                if (libs[i].NativesKey != null) continue;
                cp.Add(Path.Combine(VyraPaths.LibrariesDir, libs[i].Path.Replace('/', Path.DirectorySeparatorChar)));
            }
            try
            {
                string[] jars = Directory.GetFiles(VyraPaths.LibrariesDir, "*.jar", SearchOption.AllDirectories);
                for (int i = 0; i < jars.Length; i++)
                {
                    string n = jars[i].ToLowerInvariant();
                    if ((n.Contains("fabric-loader") || n.Contains("intermediary")) && cp.IndexOf(jars[i]) < 0)
                        cp.Add(jars[i]);
                }
            }
            catch { }

            string mainClass = MiniJson.ExtractString(vj, "mainClass", "net.minecraft.client.main.Main");
            if (p.Loader == "fabric")
            {
                try
                {
                    string mc = Path.Combine(instDir, "fabric-mainclass.txt");
                    if (File.Exists(mc)) mainClass = File.ReadAllText(mc, Encoding.UTF8).Trim();
                    else mainClass = "net.fabricmc.loader.impl.launch.knot.KnotClient";
                }
                catch { mainClass = "net.fabricmc.loader.impl.launch.knot.KnotClient"; }
            }

            Dictionary<string, string> vars = new Dictionary<string, string>();
            vars["auth_player_name"] = playerName;
            vars["version_name"] = p.MinecraftVersion;
            vars["game_directory"] = instDir;
            vars["assets_root"] = assetsRoot;
            vars["assets_index_name"] = assetId;
            vars["auth_uuid"] = uuid;
            vars["auth_access_token"] = accessToken;
            vars["clientid"] = "vyra-client";
            vars["auth_xuid"] = "0";
            vars["user_type"] = accessToken == "-" ? "legacy" : "msa";
            vars["version_type"] = MiniJson.ExtractString(vj, "type", "release");
            vars["natives_directory"] = nativesDir;
            vars["launcher_name"] = "VYRA-Client";
            vars["launcher_version"] = "0.2.0";
            vars["classpath"] = string.Join(";", cp.ToArray());
            vars["classpath_separator"] = ";";

            StringBuilder jvm = new StringBuilder();
            jvm.Append("-Xmx").Append(p.RamMB).Append("M -Xms512M ");
            jvm.Append("-Djava.library.path=\"").Append(nativesDir).Append("\" ");
            jvm.Append("-Dvyra.profile=\"").Append(p.Name).Append("\" ");
            jvm.Append("-Dvyra.safeMode=").Append(p.SafeMode ? "true" : "false").Append(" ");
            if (p.JavaArgs != null && p.JavaArgs.Trim().Length > 0) jvm.Append(p.JavaArgs).Append(" ");

            string argsBlock = JsonScan.Block(vj, "arguments");
            if (argsBlock != null)
            {
                string jvmArr = JsonScan.Block(argsBlock, "jvm");
                string gameArr = JsonScan.Block(argsBlock, "game");
                jvm.Append(FlattenArgArray(jvmArr, vars)).Append(" ");
                jvm.Append("-cp \"").Append(vars["classpath"]).Append("\" ");
                jvm.Append(mainClass).Append(" ");
                jvm.Append(FlattenArgArray(gameArr, vars));
            }
            else
            {
                string legacy = MiniJson.ExtractString(vj, "minecraftArguments", "");
                jvm.Append("-cp \"").Append(vars["classpath"]).Append("\" ");
                jvm.Append(mainClass).Append(" ");
                jvm.Append(Subst(legacy, vars));
            }

            LaunchPlan plan = new LaunchPlan();
            plan.JavaExe = javaExe;
            plan.Arguments = jvm.ToString();
            plan.WorkDir = instDir;
            plan.MainClass = mainClass;
            return plan;
        }

        private static string Subst(string s, Dictionary<string, string> vars)
        {
            if (s == null) return "";
            Regex r = new Regex("\\$\\{([^}]+)\\}");
            return r.Replace(s, delegate(Match m)
            {
                string k = m.Groups[1].Value;
                string v;
                if (vars.TryGetValue(k, out v)) return v;
                return m.Value;
            });
        }

        private static string FlattenArgArray(string arrayOrNull, Dictionary<string, string> vars)
        {
            if (arrayOrNull == null) return "";
            StringBuilder sb = new StringBuilder();
            int i = 0;
            while (i < arrayOrNull.Length)
            {
                while (i < arrayOrNull.Length && arrayOrNull[i] != '"' && arrayOrNull[i] != '{') i++;
                if (i >= arrayOrNull.Length) break;
                if (arrayOrNull[i] == '{')
                {
                    int start = i;
                    int depth = 0;
                    bool inS = false;
                    bool esc = false;
                    for (; i < arrayOrNull.Length; i++)
                    {
                        char ch = arrayOrNull[i];
                        if (inS)
                        {
                            if (esc) esc = false;
                            else if (ch == '\\') esc = true;
                            else if (ch == '"') inS = false;
                            continue;
                        }
                        if (ch == '"') inS = true;
                        else if (ch == '{') depth++;
                        else if (ch == '}')
                        {
                            depth--;
                            if (depth == 0) { i++; break; }
                        }
                    }
                    string obj = arrayOrNull.Substring(start, i - start);
                    string rules = JsonScan.Block(obj, "rules");
                    if (!AllowedByRules(rules)) continue;
                    string val = JsonScan.Block(obj, "value");
                    if (val != null)
                    {
                        if (val.StartsWith("[")) sb.Append(FlattenArgArray(val, vars)).Append(" ");
                        else
                        {
                            string vs = MiniJson.ExtractString(obj, "value", null);
                            if (vs != null) sb.Append(Subst(vs, vars)).Append(" ");
                        }
                    }
                }
                else
                {
                    i++;
                    StringBuilder cur = new StringBuilder();
                    bool esc = false;
                    for (; i < arrayOrNull.Length; i++)
                    {
                        char ch = arrayOrNull[i];
                        if (esc) { cur.Append(ch); esc = false; continue; }
                        if (ch == '\\') { esc = true; continue; }
                        if (ch == '"') { i++; break; }
                        cur.Append(ch);
                    }
                    sb.Append(Subst(cur.ToString(), vars)).Append(" ");
                }
            }
            return sb.ToString().Trim();
        }

        public static string OfflineUuid(string playerName)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes("OfflinePlayer:" + playerName));
                hash[6] = (byte)((hash[6] & 0x0F) | 0x30);
                hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
                Guid g = new Guid(hash);
                return g.ToString("D");
            }
        }
    }
}

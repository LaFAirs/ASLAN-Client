using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace Vyra
{
    public sealed class ModrinthHit
    {
        public string Id;
        public string Slug;
        public string Title;
        public string Description;
        public string License;
        public string Versions;
        public string Downloads;
    }

    public sealed class ModrinthVersion
    {
        public string Id;
        public string VersionNumber;
        public string GameVersion;
        public string Loader;
        public string FileUrl;
        public string FileName;
        public long FileSize;
        public string Sha512;
        public List<string> RequiredDeps = new List<string>(); // Projekt-Slugs erforderlicher Abhängigkeiten
    }

    public static class ModrinthService
    {
        public const string Api = "https://api.modrinth.com/v2";

        public static List<ModrinthHit> Search(string query, string loader, string mcVersion, int limit)
        {
            string url = Api + "/search?query=" + Uri.EscapeDataString(query)
                + "&facets=[[\"categories:" + Uri.EscapeDataString(loader) + "\"],[\"versions:" + Uri.EscapeDataString(mcVersion) + "\"]]"
                + "&limit=" + limit;
            string json;
            try { json = Http.Get(url, 20000); }
            catch
            {
                url = Api + "/search?query=" + Uri.EscapeDataString(query) + "&limit=" + limit;
                json = Http.Get(url, 20000);
            }
            List<ModrinthHit> hits = new List<ModrinthHit>();
            int hi = json.IndexOf("\"hits\"");
            if (hi < 0) return hits;
            int i = json.IndexOf('[', hi);
            int brace = 0;
            StringBuilder cur = new StringBuilder();
            bool inObj = false;
            for (; i >= 0 && i < json.Length && hits.Count < limit; i++)
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
                        ModrinthHit h = new ModrinthHit();
                        h.Slug = MiniJson.ExtractString(o, "slug", null);
                        h.Title = MiniJson.ExtractString(o, "title", h.Slug);
                        h.Description = MiniJson.ExtractString(o, "description", "");
                        h.Id = MiniJson.ExtractString(o, "project_id", h.Slug);
                        h.License = MiniJson.ExtractString(o, "license", "");
                        if (h.Slug != null) hits.Add(h);
                    }
                }
                if (ch == ']' && brace == 0 && hits.Count > 0) break;
            }
            return hits;
        }

        public static string GetLicense(string slug)
        {
            try
            {
                string json = Http.Get(Api + "/project/" + Uri.EscapeDataString(slug), 15000);
                return MiniJson.ExtractString(json, "license", "");
            }
            catch { return ""; }
        }

        public static List<ModrinthVersion> GetVersions(string projectSlug, string loader, string mcVersion)
        {
            string url = Api + "/project/" + Uri.EscapeDataString(projectSlug) + "/version?game_versions=[\"" + Uri.EscapeDataString(mcVersion) + "\"]&loaders=[\"" + Uri.EscapeDataString(loader) + "\"]";
            string json = Http.Get(url, 20000);
            return ParseVersions(json);
        }

        public static List<ModrinthVersion> ParseVersions(string json)
        {
            List<ModrinthVersion> list = new List<ModrinthVersion>();
            int i = json.IndexOf('[');
            int brace = 0;
            StringBuilder cur = new StringBuilder();
            bool inObj = false;
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
                        ModrinthVersion v = ParseOne(o);
                        if (v != null) list.Add(v);
                    }
                }
            }
            return list;
        }

        private static ModrinthVersion ParseOne(string o)
        {
            ModrinthVersion v = new ModrinthVersion();
            v.Id = MiniJson.ExtractString(o, "id", null);
            v.VersionNumber = MiniJson.ExtractString(o, "version_number", "?");
            int gv = o.IndexOf("\"game_versions\"");
            if (gv >= 0)
            {
                int q1 = o.IndexOf('"', o.IndexOf('[', gv) + 1);
                int q2 = q1 >= 0 ? o.IndexOf('"', q1 + 1) : -1;
                if (q1 >= 0 && q2 > q1) v.GameVersion = o.Substring(q1 + 1, q2 - q1 - 1);
            }
            int fi = o.IndexOf("\"files\"");
            if (fi >= 0)
            {
                int u = o.IndexOf("\"url\"", fi);
                if (u >= 0)
                {
                    int q1 = o.IndexOf('"', o.IndexOf(':', u) + 1);
                    int q2 = q1 >= 0 ? o.IndexOf('"', q1 + 1) : -1;
                    if (q1 >= 0 && q2 > q1) v.FileUrl = o.Substring(q1 + 1, q2 - q1 - 1);
                }
                int fn = o.IndexOf("\"filename\"", fi);
                if (fn >= 0)
                {
                    int q1 = o.IndexOf('"', o.IndexOf(':', fn) + 1);
                    int q2 = q1 >= 0 ? o.IndexOf('"', q1 + 1) : -1;
                    if (q1 >= 0 && q2 > q1) v.FileName = o.Substring(q1 + 1, q2 - q1 - 1);
                }
                int hs = o.IndexOf("\"sha512\"", fi);
                if (hs >= 0)
                {
                    int q1 = o.IndexOf('"', o.IndexOf(':', hs) + 1);
                    int q2 = q1 >= 0 ? o.IndexOf('"', q1 + 1) : -1;
                    if (q1 >= 0 && q2 > q1) v.Sha512 = o.Substring(q1 + 1, q2 - q1 - 1);
                }
            }
            // dependencies: [{"project_id": "...", "dependency_type": "required", ...}]
            int di = o.IndexOf("\"dependencies\"");
            if (di >= 0)
            {
                int arr = o.IndexOf('[', di);
                if (arr >= 0)
                {
                    int depth = 1;
                    int k = arr + 1;
                    StringBuilder dep = new StringBuilder();
                    int bdepth = 0;
                    for (; k < o.Length && depth > 0; k++)
                    {
                        char ch = o[k];
                        if (ch == '{') bdepth++;
                        if (bdepth > 0) dep.Append(ch);
                        if (ch == '}')
                        {
                            bdepth--;
                            if (bdepth == 0)
                            {
                                string d = dep.ToString();
                                dep.Length = 0;
                                if (d.Contains("\"required\""))
                                {
                                    string pid = MiniJson.ExtractString(d, "project_id", null);
                                    if (pid != null) v.RequiredDeps.Add(pid);
                                }
                            }
                        }
                        if (ch == '[') depth++;
                        else if (ch == ']') depth--;
                    }
                }
            }
            if (v.FileUrl != null && v.FileName != null) return v;
            return null;
        }

        public static string ModsDir(GameProfile p)
        {
            string d = Path.Combine(VyraPaths.ProfilesDir, MinecraftService.SafeName(p.Name), "mods");
            Directory.CreateDirectory(d);
            return d;
        }

        public static void Install(ModrinthVersion v, GameProfile p, DownloadProgress prog, CancellationToken ct)
        {
            string dir = ModsDir(p);
            string dest = Path.Combine(dir, v.FileName);
            if (!(v.FileUrl.StartsWith("https://cdn.modrinth.com/") || v.FileUrl.StartsWith("https://edge.forgecdn.net/")))
                throw new Exception("Download-Quelle nicht vertrauenswürdig: " + v.FileUrl);
            if (prog != null) prog.Label = v.FileName;
            Http.Download(v.FileUrl, dest, prog, ct);
            // SHA-512-Prüfung, wenn vom Index geliefert
            if (v.Sha512 != null && v.Sha512.Length == 128)
            {
                string got = HashCheck.Sha512Of(dest);
                if (!string.Equals(got, v.Sha512, StringComparison.OrdinalIgnoreCase))
                {
                    try { File.Delete(dest); } catch { }
                    throw new Exception("Hash mismatch bei " + v.FileName + " — Download verworfen.");
                }
            }
            // Erforderliche Abhängigkeiten (rekursiv, mit Bestätigung durch Aufrufer)
            for (int i = 0; i < v.RequiredDeps.Count; i++)
            {
                string depSlug = ResolveSlug(v.RequiredDeps[i]);
                if (depSlug == null) continue;
                if (IsPresent(p, depSlug)) continue;
                List<ModrinthVersion> dvs = GetVersions(depSlug, "fabric", p.MinecraftVersion);
                if (dvs.Count == 0) throw new Exception("Abhängigkeit " + depSlug + " hat keine passende Version für MC " + p.MinecraftVersion + ".");
                Install(dvs[0], p, prog, ct);
            }
        }

        private static string ResolveSlug(string projectIdOrSlug)
        {
            try
            {
                string json = Http.Get(Api + "/project/" + Uri.EscapeDataString(projectIdOrSlug), 15000);
                return MiniJson.ExtractString(json, "slug", projectIdOrSlug);
            }
            catch { return projectIdOrSlug; }
        }

        private static bool IsPresent(GameProfile p, string slug)
        {
            try
            {
                string[] files = Directory.GetFiles(ModsDir(p), "*.jar*");
                for (int i = 0; i < files.Length; i++)
                    if (Path.GetFileName(files[i]).ToLowerInvariant().Contains(slug.Replace("-", "")) ||
                        Path.GetFileName(files[i]).ToLowerInvariant().Contains(slug))
                        return true;
            }
            catch { }
            return false;
        }

        public static List<string> ListInstalled(GameProfile p)
        {
            List<string> files = new List<string>();
            try
            {
                string dir = ModsDir(p);
                string[] jars = Directory.GetFiles(dir, "*.jar");
                for (int i = 0; i < jars.Length; i++) files.Add(Path.GetFileName(jars[i]));
                string[] dis = Directory.GetFiles(dir, "*.jar.disabled");
                for (int i = 0; i < dis.Length; i++) files.Add(Path.GetFileName(dis[i]) + " (deaktiviert)");
            }
            catch { }
            return files;
        }

        public static void SetEnabled(GameProfile p, string fileName, bool enabled)
        {
            string dir = ModsDir(p);
            string a = Path.Combine(dir, fileName);
            string b = Path.Combine(dir, fileName + ".disabled");
            if (enabled && File.Exists(b) && !File.Exists(a)) File.Move(b, a);
            else if (!enabled && File.Exists(a)) File.Move(a, b);
        }

        public static void Remove(GameProfile p, string fileName)
        {
            string dir = ModsDir(p);
            string a = Path.Combine(dir, fileName);
            string b = Path.Combine(dir, fileName + ".disabled");
            try
            {
                string backup = Path.Combine(VyraPaths.BackupsDir, DateTime.Now.ToString("yyyyMMdd-HHmmss"));
                Directory.CreateDirectory(backup);
                if (File.Exists(a)) { File.Copy(a, Path.Combine(backup, fileName), true); File.Delete(a); }
                if (File.Exists(b)) { File.Copy(b, Path.Combine(backup, fileName + ".disabled"), true); File.Delete(b); }
            }
            catch (Exception ex) { throw new Exception("Entfernen fehlgeschlagen: " + ex.Message); }
        }

        // Modprofile exportieren/importieren (Liste von Slugs + Versionen)
        public static string ExportProfile(GameProfile p)
        {
            StringBuilder sb = new StringBuilder("{\"profile\":\"");
            sb.Append(p.Name.Replace("\\", "\\\\").Replace("\"", "\\\""));
            sb.Append("\",\"mods\":[");
            List<string> mods = ListInstalled(p);
            bool first = true;
            for (int i = 0; i < mods.Count; i++)
            {
                if (mods[i].Contains("(deaktiviert)")) continue;
                if (!first) sb.Append(",");
                sb.Append("\"").Append(mods[i].Replace("\"", "")).Append("\"");
                first = false;
            }
            sb.Append("]}");
            string dir = Path.Combine(VyraPaths.BaseDir, "modprofiles");
            Directory.CreateDirectory(dir);
            string dest = Path.Combine(dir, MinecraftService.SafeName(p.Name) + ".vyramods.json");
            File.WriteAllText(dest, sb.ToString(), Encoding.UTF8);
            return dest;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;

namespace Aslan
{
    public sealed class ModrinthHit
    {
        public string Id;
        public string Slug;
        public string Title;
        public string Description;
        public string License;
        public string IconUrl;
        public string Downloads;
        public string Categories;
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
        public List<string> RequiredDeps = new List<string>();
    }

    public sealed class ModrinthProject
    {
        public string Slug;
        public string Title;
        public string Description;
        public string Body;
        public string License;
        public string IconUrl;
        public string Downloads;
        public string Categories;
        public string ProjectType;
    }

    public static class ModrinthService
    {
        public const string Api = "https://api.modrinth.com/v2";

        public static List<ModrinthHit> Search(string query, string loader, string mcVersion, string projectType, int limit)
        {
            string facets = "[[\"project_type:" + Uri.EscapeDataString(projectType) + "\"]";
            if (loader != null && loader.Length > 0 && loader != "vanilla")
                facets += ",[\"categories:" + Uri.EscapeDataString(loader) + "\"]";
            if (mcVersion != null && mcVersion.Length > 0)
                facets += ",[\"versions:" + Uri.EscapeDataString(mcVersion) + "\"]";
            facets += "]";
            string url = Api + "/search?query=" + Uri.EscapeDataString(query) + "&facets=" + facets + "&limit=" + limit;
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
                        h.IconUrl = MiniJson.ExtractString(o, "icon_url", null);
                        h.Downloads = MiniJson.ExtractNumber(o, "downloads", "?");
                        if (h.Slug != null) hits.Add(h);
                    }
                }
                if (ch == ']' && brace == 0 && hits.Count > 0) break;
            }
            return hits;
        }

        public static ModrinthProject GetProject(string slug)
        {
            string json = Http.Get(Api + "/project/" + Uri.EscapeDataString(slug), 15000);
            ModrinthProject p = new ModrinthProject();
            p.Slug = MiniJson.ExtractString(json, "slug", slug);
            p.Title = MiniJson.ExtractString(json, "title", slug);
            p.Description = MiniJson.ExtractString(json, "description", "");
            p.Body = MiniJson.ExtractString(json, "body", "");
            p.License = MiniJson.ExtractString(json, "license", "");
            p.IconUrl = MiniJson.ExtractString(json, "icon_url", null);
            p.Downloads = MiniJson.ExtractNumber(json, "downloads", "?");
            p.ProjectType = MiniJson.ExtractString(json, "project_type", "");
            return p;
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

        public static string ModsDir(AslanProfile p)
        {
            string d = Path.Combine(AslanPaths.ProfilesDir, MinecraftService.SafeName(p.Name), "mods");
            Directory.CreateDirectory(d);
            return d;
        }

        public static string TargetDir(AslanProfile p, string projectType)
        {
            string sub = "mods";
            if (projectType == "shader") sub = "shaderpacks";
            else if (projectType == "resourcepack") sub = "resourcepacks";
            string d = Path.Combine(AslanPaths.ProfilesDir, MinecraftService.SafeName(p.Name), sub);
            Directory.CreateDirectory(d);
            return d;
        }

        public static void Install(ModrinthVersion v, AslanProfile p, string projectType, DownloadProgress prog, CancellationToken ct)
        {
            string dir = TargetDir(p, projectType);
            string dest = Path.Combine(dir, v.FileName);
            if (!(v.FileUrl.StartsWith("https://cdn.modrinth.com/") || v.FileUrl.StartsWith("https://edge.forgecdn.net/")))
                throw new Exception("Download-Quelle nicht vertrauenswürdig: " + v.FileUrl);
            if (prog != null) prog.Label = v.FileName;
            DownloadCenter.Download(v.FileUrl, dest, prog, ct);
            if (v.Sha512 != null && v.Sha512.Length == 128)
            {
                string got = HashCheck.Sha512Of(dest);
                if (!string.Equals(got, v.Sha512, StringComparison.OrdinalIgnoreCase))
                {
                    try { File.Delete(dest); } catch { }
                    throw new Exception("Hash mismatch bei " + v.FileName + " — Download verworfen.");
                }
            }
            if (projectType == "mod" || projectType == null)
            {
                for (int i = 0; i < v.RequiredDeps.Count; i++)
                {
                    string depSlug = ResolveSlug(v.RequiredDeps[i]);
                    if (depSlug == null) continue;
                    if (IsPresent(p, depSlug)) continue;
                    List<ModrinthVersion> dvs = GetVersions(depSlug, p.Loader, p.MinecraftVersion);
                    if (dvs.Count == 0) throw new Exception("Abhängigkeit " + depSlug + " hat keine passende Version für MC " + p.MinecraftVersion + ".");
                    Install(dvs[0], p, "mod", prog, ct);
                }
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

        private static bool IsPresent(AslanProfile p, string slug)
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

        public static List<string> ListInstalled(AslanProfile p)
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

        public static List<string> ListFiles(AslanProfile p, string sub, string pattern)
        {
            List<string> files = new List<string>();
            try
            {
                string dir = Path.Combine(AslanPaths.ProfilesDir, MinecraftService.SafeName(p.Name), sub);
                if (!Directory.Exists(dir)) return files;
                string[] fs = Directory.GetFiles(dir, pattern);
                for (int i = 0; i < fs.Length; i++) files.Add(Path.GetFileName(fs[i]));
            }
            catch { }
            return files;
        }

        public static void SetEnabled(AslanProfile p, string fileName, bool enabled)
        {
            string dir = ModsDir(p);
            string a = Path.Combine(dir, fileName);
            string b = Path.Combine(dir, fileName + ".disabled");
            if (enabled && File.Exists(b) && !File.Exists(a)) File.Move(b, a);
            else if (!enabled && File.Exists(a)) File.Move(a, b);
        }

        public static void Remove(AslanProfile p, string fileName)
        {
            string dir = ModsDir(p);
            string a = Path.Combine(dir, fileName);
            string b = Path.Combine(dir, fileName + ".disabled");
            try
            {
                string backup = Path.Combine(AslanPaths.BackupsDir, DateTime.Now.ToString("yyyyMMdd-HHmmss"));
                Directory.CreateDirectory(backup);
                if (File.Exists(a)) { File.Copy(a, Path.Combine(backup, fileName), true); File.Delete(a); }
                if (File.Exists(b)) { File.Copy(b, Path.Combine(backup, fileName + ".disabled"), true); File.Delete(b); }
            }
            catch (Exception ex) { throw new Exception("Entfernen fehlgeschlagen: " + ex.Message); }
        }

        public static string ExportProfile(AslanProfile p)
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
            string dir = Path.Combine(AslanPaths.BaseDir, "modprofiles");
            Directory.CreateDirectory(dir);
            string dest = Path.Combine(dir, MinecraftService.SafeName(p.Name) + ".aslanmods.json");
            File.WriteAllText(dest, sb.ToString(), Encoding.UTF8);
            return dest;
        }

        // ---------- .mrpack-Import (Modrinth-Modpack-Format) ----------
        public sealed class MrpackResult
        {
            public string Name;
            public string McVersion;
            public string Loader;
            public int FilesOk;
            public List<string> Errors = new List<string>();
        }

        public static MrpackResult ImportMrpack(string mrpackPath, AslanProfile p, DownloadProgress prog, CancellationToken ct)
        {
            MrpackResult r = new MrpackResult();
            string tmp = Path.Combine(AslanPaths.CacheDir, "mrpack-" + DateTime.Now.ToString("yyyyMMddHHmmss"));
            Directory.CreateDirectory(tmp);
            using (FileStream fs = new FileStream(mrpackPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (ZipArchive zip = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                foreach (ZipArchiveEntry e in zip.Entries)
                {
                    if (e.FullName.EndsWith("/")) continue;
                    if (e.FullName.StartsWith("overrides/"))
                    {
                        string dest = Path.Combine(InstanceInstaller.InstanceDir(p), e.FullName.Substring("overrides/".Length).Replace('/', Path.DirectorySeparatorChar));
                        Directory.CreateDirectory(Path.GetDirectoryName(dest));
                        using (Stream s = e.Open())
                        using (FileStream o = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            s.CopyTo(o);
                        }
                        continue;
                    }
                    if (e.FullName == "modrinth.index.json")
                    {
                        string idx = Path.Combine(tmp, "modrinth.index.json");
                        using (Stream s = e.Open())
                        using (FileStream o = new FileStream(idx, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            s.CopyTo(o);
                        }
                    }
                }
            }
            string indexPath = Path.Combine(tmp, "modrinth.index.json");
            if (!File.Exists(indexPath)) throw new Exception("Kein modrinth.index.json im Pack (kein gültiges .mrpack).");
            string json = File.ReadAllText(indexPath, Encoding.UTF8);
            r.Name = MiniJson.ExtractString(json, "name", Path.GetFileNameWithoutExtension(mrpackPath));
            string deps = JsonScan.Block(json, "dependencies");
            if (deps != null)
            {
                r.McVersion = MiniJson.ExtractString(deps, "minecraft", p.MinecraftVersion);
                if (deps.Contains("fabric-loader")) r.Loader = "fabric";
                else if (deps.Contains("quilt-loader")) r.Loader = "quilt";
                else if (deps.Contains("neoforge")) r.Loader = "neoforge";
                else if (deps.Contains("forge")) r.Loader = "forge";
            }
            p.ModpackName = r.Name;
            // Dateien: [{path, hashes:{sha512}, downloads:[url], fileSize}]
            int fi = json.IndexOf("\"files\"");
            if (fi < 0) throw new Exception("Pack enthält keine Dateiliste.");
            List<string> fileObjs = JsonScan.SplitObjects(json.Substring(fi), "files", 2000);
            if (prog != null) { }
            for (int i = 0; i < fileObjs.Count; i++)
            {
                if (ct.IsCancellationRequested) throw new OperationCanceledException();
                try
                {
                    string o = fileObjs[i];
                    string rel = MiniJson.ExtractString(o, "path", null);
                    if (rel == null) continue;
                    string sha512 = null;
                    int hh = o.IndexOf("\"hashes\"");
                    if (hh >= 0)
                    {
                        int q1 = o.IndexOf("\"sha512\"", hh);
                        if (q1 >= 0)
                        {
                            int c = o.IndexOf(':', q1);
                            int s1 = o.IndexOf('"', c + 1);
                            int s2 = o.IndexOf('"', s1 + 1);
                            if (s1 >= 0 && s2 > s1) sha512 = o.Substring(s1 + 1, s2 - s1 - 1);
                        }
                    }
                    string url = null;
                    int dd = o.IndexOf("\"downloads\"");
                    if (dd >= 0)
                    {
                        int s1 = o.IndexOf('"', o.IndexOf('[', dd) + 1);
                        int s2 = s1 >= 0 ? o.IndexOf('"', s1 + 1) : -1;
                        if (s1 >= 0 && s2 > s1) url = o.Substring(s1 + 1, s2 - s1 - 1);
                    }
                    if (url == null) { r.Errors.Add(rel + ": keine URL"); continue; }
                    if (!(url.StartsWith("https://cdn.modrinth.com/") || url.StartsWith("https://edge.forgecdn.net/")))
                    { r.Errors.Add(rel + ": Quelle nicht vertrauenswürdig"); continue; }
                    string dest = Path.Combine(InstanceInstaller.InstanceDir(p), rel.Replace('/', Path.DirectorySeparatorChar));
                    Directory.CreateDirectory(Path.GetDirectoryName(dest));
                    if (prog != null) prog.Label = Path.GetFileName(dest);
                    DownloadCenter.Download(url, dest, prog, ct);
                    if (sha512 != null && sha512.Length == 128)
                    {
                        string got = HashCheck.Sha512Of(dest);
                        if (!string.Equals(got, sha512, StringComparison.OrdinalIgnoreCase))
                        {
                            try { File.Delete(dest); } catch { }
                            r.Errors.Add(rel + ": Hash mismatch");
                            continue;
                        }
                    }
                    r.FilesOk++;
                }
                catch (Exception ex) { r.Errors.Add("Datei " + i + ": " + ex.Message); }
            }
            try { Directory.Delete(tmp, true); } catch { }
            return r;
        }
    }
}

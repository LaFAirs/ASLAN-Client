using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Threading;

namespace Aslan
{
    public sealed class CfException : Exception
    {
        public bool KeyMissing;
        public CfException(string msg, bool keyMissing) : base(msg) { KeyMissing = keyMissing; }
    }

    public sealed class CfHit
    {
        public int Id;
        public string Name;
        public string Summary;
        public string IconUrl;
        public string WebsiteUrl;
        public string DownloadCount;
        public string Categories;
    }

    public sealed class CfFile
    {
        public int Id;
        public string FileName;
        public string GameVersion;
        public string ModLoader;
        public string DownloadUrl;
        public long FileLength;
    }

    // CurseForge-Integration über die offizielle API.
    // Benötigt einen API-Key (x-api-key). Ohne Key: ehrliche KeyMissing-Meldung +
    // "Auf CurseForge öffnen"-Fallback (offizielle Projektseite, keine Scraper).
    public static class CurseForgeService
    {
        public const string Api = "https://api.curseforge.com/v1";
        public const string KeyFile = "cf_key.dat";

        public static bool HasKey() { return LoadKey() != null; }

        public static string LoadKey() { return DpapiStore.Load(KeyFile); }

        public static void SaveKey(string key)
        {
            if (key == null || key.Trim().Length < 10) throw new Exception("API-Key zu kurz.");
            DpapiStore.Save(KeyFile, key.Trim());
        }

        public static void ClearKey() { DpapiStore.Delete(KeyFile); }

        private static string GetAuthed(string url, int timeoutMs)
        {
            string key = LoadKey();
            if (key == null) throw new CfException("CurseForge-API-Key fehlt. Trage ihn in den Einstellungen ein (kostenloser Key unter curseforge.com, API-Dokumentation). Bis dahin: Nutze Modrinth oder öffne die offizielle Projektseite.", true);
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            req.Timeout = timeoutMs;
            req.UserAgent = "ASLAN-Client/0.3";
            req.Headers["x-api-key"] = key;
            req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            try
            {
                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (Stream s = resp.GetResponseStream())
                using (StreamReader r = new StreamReader(s, Encoding.UTF8))
                {
                    return r.ReadToEnd();
                }
            }
            catch (WebException wex)
            {
                HttpWebResponse r = wex.Response as HttpWebResponse;
                if (r != null && (int)r.StatusCode == 403)
                    throw new CfException("CurseForge lehnt den Key ab (403). Key prüfen / Berechtigung beantragen.", false);
                throw new CfException("CurseForge-Fehler: " + wex.Message, false);
            }
        }

        // modLoaderType: 0 beliebig, 1 Forge, 4 Fabric, 5 Quilt, 6 NeoForge. classId 6 = Mods.
        public static int LoaderToCf(string loader)
        {
            if (loader == "forge") return 1;
            if (loader == "fabric") return 4;
            if (loader == "quilt") return 5;
            if (loader == "neoforge") return 6;
            return 0;
        }

        public static List<CfHit> Search(string query, string mcVersion, string loader, int limit)
        {
            string url = Api + "/mods/search?gameId=432&classId=6&searchFilter=" + Uri.EscapeDataString(query)
                + "&gameVersion=" + Uri.EscapeDataString(mcVersion)
                + "&modLoaderType=" + LoaderToCf(loader)
                + "&pageSize=" + Math.Max(1, Math.Min(50, limit));
            string json = GetAuthed(url, 20000);
            List<CfHit> hits = new List<CfHit>();
            List<string> objs = JsonScan.SplitObjects(json, "data", limit);
            for (int i = 0; i < objs.Count; i++)
            {
                string o = objs[i];
                CfHit h = new CfHit();
                string id = MiniJson.ExtractNumber(o, "id", "0");
                int n;
                if (!int.TryParse(id, out n)) continue;
                h.Id = n;
                h.Name = MiniJson.ExtractString(o, "name", "?");
                h.Summary = MiniJson.ExtractString(o, "summary", "");
                h.WebsiteUrl = MiniJson.ExtractString(o, "websiteUrl", "https://www.curseforge.com/minecraft/mc-mods");
                h.DownloadCount = MiniJson.ExtractNumber(o, "downloadCount", "?");
                hits.Add(h);
            }
            return hits;
        }

        public static List<CfFile> GetFiles(int modId, string mcVersion, string loader)
        {
            string url = Api + "/mods/" + modId + "/files?gameVersion=" + Uri.EscapeDataString(mcVersion)
                + "&modLoaderType=" + LoaderToCf(loader) + "&pageSize=20";
            string json = GetAuthed(url, 20000);
            List<CfFile> files = new List<CfFile>();
            List<string> objs = JsonScan.SplitObjects(json, "data", 20);
            for (int i = 0; i < objs.Count; i++)
            {
                string o = objs[i];
                CfFile f = new CfFile();
                string id = MiniJson.ExtractNumber(o, "id", "0");
                int n;
                if (!int.TryParse(id, out n)) continue;
                f.Id = n;
                f.FileName = MiniJson.ExtractString(o, "fileName", "file.jar");
                f.DownloadUrl = MiniJson.ExtractString(o, "downloadUrl", null);
                string fl = MiniJson.ExtractNumber(o, "fileLength", "0");
                long ln;
                if (long.TryParse(fl, out ln)) f.FileLength = ln;
                if (f.DownloadUrl != null) files.Add(f);
            }
            return files;
        }

        public static void Install(CfFile f, AslanProfile p, DownloadProgress prog, CancellationToken ct)
        {
            if (!(f.DownloadUrl.StartsWith("https://edge.forgecdn.net/") || f.DownloadUrl.StartsWith("https://mediafilez.forgecdn.net/")))
                throw new Exception("Download-Quelle nicht vertrauenswürdig: " + f.DownloadUrl);
            string dir = ModrinthService.ModsDir(p);
            string dest = Path.Combine(dir, f.FileName);
            if (prog != null) prog.Label = f.FileName;
            DownloadCenter.Download(f.DownloadUrl, dest, prog, ct);
            if (f.FileLength > 0)
            {
                long got = new FileInfo(dest).Length;
                if (got != f.FileLength) { try { File.Delete(dest); } catch { } throw new Exception("Größe mismatch — Download verworfen."); }
            }
        }

        // CurseForge-Manifest-Import (manifest.json aus Export-Archiven).
        public sealed class CfManifestResult
        {
            public string Name;
            public string McVersion;
            public string LoaderId;
            public int FilesOk;
            public List<string> Errors = new List<string>();
        }

        public static CfManifestResult ImportManifest(string zipPath, AslanProfile p, DownloadProgress prog, CancellationToken ct)
        {
            CfManifestResult r = new CfManifestResult();
            string manifest = null;
            using (FileStream fs = new FileStream(zipPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (System.IO.Compression.ZipArchive zip = new System.IO.Compression.ZipArchive(fs, ZipArchiveMode.Read))
            {
                foreach (System.IO.Compression.ZipArchiveEntry e in zip.Entries)
                {
                    if (e.FullName.EndsWith("manifest.json"))
                    {
                        using (Stream s = e.Open())
                        using (StreamReader sr = new StreamReader(s, Encoding.UTF8))
                        {
                            manifest = sr.ReadToEnd();
                        }
                        break;
                    }
                }
                if (manifest == null) throw new Exception("Kein manifest.json im Archiv.");
                // overrides/ direkt übernehmen
                foreach (System.IO.Compression.ZipArchiveEntry e in zip.Entries)
                {
                    if (e.FullName.StartsWith("overrides/") && !e.FullName.EndsWith("/"))
                    {
                        string dest = Path.Combine(InstanceInstaller.InstanceDir(p), e.FullName.Substring("overrides/".Length).Replace('/', Path.DirectorySeparatorChar));
                        Directory.CreateDirectory(Path.GetDirectoryName(dest));
                        using (Stream s = e.Open())
                        using (FileStream o = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            s.CopyTo(o);
                        }
                    }
                }
            }
            r.Name = MiniJson.ExtractString(manifest, "name", Path.GetFileNameWithoutExtension(zipPath));
            string mc = JsonScan.Block(manifest, "minecraft");
            if (mc != null) r.McVersion = MiniJson.ExtractString(mc, "version", p.MinecraftVersion);
            string mld = JsonScan.Block(manifest, "modLoaders");
            if (mld != null)
            {
                string id = MiniJson.ExtractString(mld, "id", "");
                r.LoaderId = id;
                if (id.StartsWith("forge-")) p.Loader = "forge";
                else if (id.StartsWith("neoforge-")) p.Loader = "neoforge";
                else if (id.StartsWith("fabric-")) p.Loader = "fabric";
                else if (id.StartsWith("quilt-")) p.Loader = "quilt";
            }
            // Dateien brauchen die API (projectID/fileID -> downloadUrl).
            List<string> fileObjs = JsonScan.SplitObjects(manifest, "files", 2000);
            for (int i = 0; i < fileObjs.Count; i++)
            {
                if (ct.IsCancellationRequested) throw new OperationCanceledException();
                try
                {
                    string o = fileObjs[i];
                    string pid = MiniJson.ExtractNumber(o, "projectID", "0");
                    string fid = MiniJson.ExtractNumber(o, "fileID", "0");
                    string fjson = GetAuthed(Api + "/mods/" + pid + "/files/" + fid, 20000);
                    string data = JsonScan.Block(fjson, "data") ?? fjson;
                    string url = MiniJson.ExtractString(data, "downloadUrl", null);
                    string name = MiniJson.ExtractString(data, "fileName", "mod.jar");
                    if (url == null) { r.Errors.Add(pid + "/" + fid + ": keine Download-URL (Projekt verbietet API-Downloads?)"); continue; }
                    CfFile f = new CfFile();
                    f.DownloadUrl = url;
                    f.FileName = name;
                    Install(f, p, prog, ct);
                    r.FilesOk++;
                }
                catch (CfException) { throw; }
                catch (Exception ex) { r.Errors.Add("Datei " + i + ": " + ex.Message); }
            }
            p.ModpackName = r.Name;
            return r;
        }
    }
}

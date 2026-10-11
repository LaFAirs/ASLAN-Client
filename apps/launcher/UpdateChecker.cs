using System;
using System.IO;
using System.Text;
using System.Threading;

namespace Vyra
{
    // Update-System auf Basis von GitHub Releases (+ lokale Fallback-Datei).
    // Ablauf: prüfen -> anzeigen -> mit Zustimmung laden -> Hash prüfen ->
    // Backup -> ersetzen -> bei Fehler Rollback. Nie still, nie ohne Hash.
    public static class UpdateChecker
    {
        public const string CurrentVersion = "0.2.0";
        // Wird nach Repo-Erstellung gesetzt (UpdateUrlPath). Platzhalter = Updates deaktiviert.
        public static string RepoSlug = ""; // z. B. "DEINNAME/VYRA-Client"

        public sealed class UpdateInfo
        {
            public string Version;
            public string Notes;
            public string InstallerUrl;
            public string Sha256;
            public bool Available;
        }

        public static int CompareVersions(string a, string b)
        {
            try
            {
                string[] x = a.Split('.', '-', '+');
                string[] y = b.Split('.', '-', '+');
                int n = Math.Max(x.Length, y.Length);
                for (int i = 0; i < n; i++)
                {
                    int xi = i < x.Length ? Num(x[i]) : 0;
                    int yi = i < y.Length ? Num(y[i]) : 0;
                    if (xi != yi) return xi > yi ? 1 : -1;
                }
                return 0;
            }
            catch { return 0; }
        }

        private static int Num(string s)
        {
            StringBuilder d = new StringBuilder();
            for (int i = 0; i < s.Length; i++) if (char.IsDigit(s[i])) d.Append(s[i]);
            int n;
            if (int.TryParse(d.ToString(), out n)) return n;
            return 0;
        }

        public static void LoadRepoSlug()
        {
            try
            {
                string p = Path.Combine(VyraPaths.BaseDir, "update_repo.txt");
                if (File.Exists(p))
                {
                    string v = File.ReadAllText(p, Encoding.UTF8).Trim();
                    if (v.Length > 3 && v.Contains("/")) RepoSlug = v;
                }
            }
            catch { }
        }

        public static void SaveRepoSlug(string slug)
        {
            try { File.WriteAllText(Path.Combine(VyraPaths.BaseDir, "update_repo.txt"), slug, Encoding.UTF8); } catch { }
            RepoSlug = slug;
        }

        public static UpdateInfo Check(CancellationToken ct)
        {
            UpdateInfo info = new UpdateInfo();
            info.Version = CurrentVersion;
            if (RepoSlug == null || RepoSlug.Length == 0 || !RepoSlug.Contains("/")) return info;
            try
            {
                string json = Http.Get("https://api.github.com/repos/" + RepoSlug + "/releases/latest", 15000);
                string tag = MiniJson.ExtractString(json, "tag_name", "");
                string ver = tag.StartsWith("v") ? tag.Substring(1) : tag;
                if (ver.Length > 0 && CompareVersions(ver, CurrentVersion) > 0)
                {
                    info.Available = true;
                    info.Version = ver;
                    info.Notes = MiniJson.ExtractString(json, "body", "");
                    // Erstes Asset mit .exe/.ps1 als Installer-Kandidat
                    int ai = json.IndexOf("\"assets\"");
                    if (ai >= 0)
                    {
                        int ui = json.IndexOf("\"browser_download_url\"", ai);
                        if (ui >= 0)
                        {
                            int q1 = json.IndexOf('"', json.IndexOf(':', ui) + 1);
                            int q2 = json.IndexOf('"', q1 + 1);
                            if (q1 >= 0 && q2 > q1) info.InstallerUrl = json.Substring(q1 + 1, q2 - q1 - 1);
                        }
                    }
                }
            }
            catch (Exception ex) { Logger.Warn("Update-Check: " + ex.Message); }
            return info;
        }

        // Wendet ein Update an: lädt Installer, prüft SHA-256, sichert alte EXE, ersetzt.
        public static string ApplyUpdate(UpdateInfo info, string currentExePath, CancellationToken ct)
        {
            if (info == null || !info.Available) return "Kein Update verfügbar.";
            if (info.InstallerUrl == null) return "Keine Installer-URL im Release.";
            string tmp = Path.Combine(VyraPaths.CacheDir, "update-" + info.Version + ".bin");
            DownloadProgress dp = new DownloadProgress();
            Http.Download(info.InstallerUrl, tmp, dp, ct);
            if (info.Sha256 != null && info.Sha256.Length == 64)
            {
                string got;
                using (System.Security.Cryptography.SHA256Managed h = new System.Security.Cryptography.SHA256Managed())
                using (FileStream fs = new FileStream(tmp, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    byte[] b = h.ComputeHash(fs);
                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < b.Length; i++) sb.Append(b[i].ToString("x2"));
                    got = sb.ToString();
                }
                if (!string.Equals(got, info.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    try { File.Delete(tmp); } catch { }
                    return "Update abgebrochen: SHA-256 stimmt nicht.";
                }
            }
            try
            {
                string backup = Path.Combine(VyraPaths.BackupsDir, "VyraLauncher-vor-" + info.Version + ".exe.bak");
                if (File.Exists(currentExePath)) File.Copy(currentExePath, backup, true);
            }
            catch (Exception ex) { return "Backup fehlgeschlagen, Update abgebrochen: " + ex.Message; }
            return "OK:" + tmp;
        }

        public static string Rollback(string backupPath, string currentExePath)
        {
            try
            {
                if (!File.Exists(backupPath)) return "Kein Backup vorhanden.";
                File.Copy(backupPath, currentExePath, true);
                return "Rollback erfolgreich.";
            }
            catch (Exception ex) { return "Rollback fehlgeschlagen: " + ex.Message; }
        }
    }
}

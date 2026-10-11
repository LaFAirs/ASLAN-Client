using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace Vyra
{
    // Offizieller Microsoft-Device-Code-Flow:
    // 1) POST https://login.microsoftonline.com/consumers/oauth2/v2.0/devicecode
    // 2) Benutzer bestätigt im Browser
    // 3) Poll auf .../token. Keine Passwortabfrage, kein Passwort-Speicher.
    public sealed class DeviceCodeInfo
    {
        public string UserCode;
        public string VerificationUri;
        public string Message;
        public string DeviceCode;
        public int IntervalSec = 5;
        public int ExpiresSec = 900;
    }

    public sealed class McAccount
    {
        public string Name;
        public string Uuid;
        public string AccessToken;
        public bool Online;
    }

    public static class AuthService
    {
        public static string ClientId = "00000000-0000-0000-0000-000000000000";
        public const string TokenFile = "ms_token.dat";
        public const string ProfileFile = "ms_profile.json";

        public static void LoadClientId()
        {
            try
            {
                string p = Path.Combine(VyraPaths.BaseDir, "ms_client.txt");
                if (File.Exists(p))
                {
                    string v = File.ReadAllText(p, Encoding.UTF8).Trim();
                    if (v.Length == 36) ClientId = v;
                }
            }
            catch { }
        }

        public static void SaveClientId(string id)
        {
            try { File.WriteAllText(Path.Combine(VyraPaths.BaseDir, "ms_client.txt"), id, Encoding.UTF8); }
            catch { }
            ClientId = id;
        }

        public static bool HasPlaceholderId()
        {
            return ClientId == "00000000-0000-0000-0000-000000000000";
        }

        private static string Post(string url, string body)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            req.Timeout = 20000;
            req.UserAgent = "VYRA-Client/0.2";
            byte[] b = Encoding.UTF8.GetBytes(body);
            req.ContentLength = b.Length;
            using (Stream s = req.GetRequestStream()) s.Write(b, 0, b.Length);
            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            using (Stream rs = resp.GetResponseStream())
            using (StreamReader r = new StreamReader(rs, Encoding.UTF8))
            {
                return r.ReadToEnd();
            }
        }

        public static DeviceCodeInfo StartDeviceCode()
        {
            if (HasPlaceholderId())
                throw new Exception("Keine Microsoft App-ID hinterlegt. Bitte in den Einstellungen eine eigene Azure-App-ID eintragen (siehe Docs/ANMELDUNG.md).");
            string scope = Uri.EscapeDataString("XboxLive.signin offline_access");
            string body = "client_id=" + Uri.EscapeDataString(ClientId) + "&scope=" + scope;
            string json = Post("https://login.microsoftonline.com/consumers/oauth2/v2.0/devicecode", body);
            DeviceCodeInfo d = new DeviceCodeInfo();
            d.UserCode = MiniJson.ExtractString(json, "user_code", "?");
            d.VerificationUri = MiniJson.ExtractString(json, "verification_uri", "https://www.microsoft.com/link");
            d.Message = MiniJson.ExtractString(json, "message", "Bitte im Browser bestätigen.");
            d.DeviceCode = MiniJson.ExtractString(json, "device_code", null);
            string iv = MiniJson.ExtractNumber(json, "interval", "5");
            int n;
            if (int.TryParse(iv, out n)) d.IntervalSec = Math.Max(2, n);
            if (d.DeviceCode == null) throw new Exception("Device-Code-Antwort ungültig.");
            return d;
        }

        public static string PollForToken(DeviceCodeInfo d, CancellationToken ct, Action<string> status)
        {
            DateTime end = DateTime.UtcNow.AddSeconds(Math.Min(d.ExpiresSec, 900));
            while (DateTime.UtcNow < end)
            {
                if (ct.IsCancellationRequested) throw new OperationCanceledException();
                Thread.Sleep(d.IntervalSec * 1000);
                string body = "grant_type=" + Uri.EscapeDataString("urn:ietf:params:oauth:grant-type:device_code")
                    + "&client_id=" + Uri.EscapeDataString(ClientId)
                    + "&device_code=" + Uri.EscapeDataString(d.DeviceCode);
                try
                {
                    string json = Post("https://login.microsoftonline.com/consumers/oauth2/v2.0/token", body);
                    string at = MiniJson.ExtractString(json, "access_token", null);
                    string rt = MiniJson.ExtractString(json, "refresh_token", null);
                    if (at != null)
                    {
                        DpapiStore.Save(TokenFile, at);
                        if (rt != null) DpapiStore.Save("ms_refresh.dat", rt);
                        return at;
                    }
                    if (status != null) status("Antwort erhalten, prüfe …");
                }
                catch (WebException wex)
                {
                    string err = "";
                    try
                    {
                        using (HttpWebResponse r = (HttpWebResponse)wex.Response)
                        using (Stream s = r.GetResponseStream())
                        using (StreamReader sr = new StreamReader(s))
                        {
                            err = sr.ReadToEnd();
                        }
                    }
                    catch { }
                    if (err.Contains("authorization_pending")) { if (status != null) status("Warte auf Bestätigung im Browser …"); continue; }
                    if (err.Contains("slow_down")) { Thread.Sleep(5000); continue; }
                    if (err.Contains("expired_token")) throw new Exception("Code abgelaufen — bitte erneut starten.");
                    throw new Exception("Anmeldung fehlgeschlagen (" + err.Substring(0, Math.Min(120, err.Length)) + ")");
                }
            }
            throw new Exception("Zeitüberschreitung — bitte erneut versuchen.");
        }

        // Xbox/XSTS/Minecraft-Entitlement (echt, offiziell). Gibt Account mit UUID/Token zurück.
        public static McAccount CompleteMinecraftLogin(string msAccessToken)
        {
            string xbl = PostJson("https://user.auth.xboxlive.com/user/authenticate",
                "{\"Properties\":{\"AuthMethod\":\"RPS\",\"SiteName\":\"user.auth.xboxlive.com\",\"RpsTicket\":\"d=" + msAccessToken + "\"},\"RelyingParty\":\"http://auth.xboxlive.com\",\"TokenType\":\"JWT\"}");
            string xblToken = MiniJson.ExtractString(xbl, "Token", null);
            if (xblToken == null) throw new Exception("XboxLive-Auth fehlgeschlagen.");
            string xsts = PostJson("https://xsts.auth.xboxlive.com/xsts/authorize",
                "{\"Properties\":{\"SandboxId\":\"RETAIL\",\"UserTokens\":[\"" + xblToken + "\"]},\"RelyingParty\":\"rp://api.minecraftservices.com/\",\"TokenType\":\"JWT\"}");
            string xstsToken = MiniJson.ExtractString(xsts, "Token", null);
            string uhs = ExtractUhs(xsts);
            if (xstsToken == null || uhs == null) throw new Exception("XSTS-Autorisierung fehlgeschlagen.");
            string mc = PostJson("https://api.minecraftservices.com/authentication/login_with_xbox",
                "{\"identityToken\":\"XBL3.0 x=" + uhs + ";" + xstsToken + "\"}");
            string mcToken = MiniJson.ExtractString(mc, "access_token", null);
            if (mcToken == null) throw new Exception("Minecraft-Login fehlgeschlagen.");
            DpapiStore.Save("mc_token.dat", mcToken);
            string profile = HttpBearerExt.Get("https://api.minecraftservices.com/minecraft/profile", 20000, mcToken);
            string name = MiniJson.ExtractString(profile, "name", null);
            string id = MiniJson.ExtractString(profile, "id", null);
            if (name == null) throw new Exception("Account besitzt kein Minecraft-Profil (keine Java-Berechtigung).");
            try { File.WriteAllText(Path.Combine(VyraPaths.BaseDir, ProfileFile), profile, Encoding.UTF8); } catch { }
            McAccount a = new McAccount();
            a.Name = name;
            a.Uuid = id != null ? id : InstanceInstaller.OfflineUuid(name);
            a.AccessToken = mcToken;
            a.Online = true;
            return a;
        }

        private static string PostJson(string url, string json)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST";
            req.ContentType = "application/json";
            req.Accept = "application/json";
            req.Timeout = 20000;
            req.UserAgent = "VYRA-Client/0.2";
            byte[] b = Encoding.UTF8.GetBytes(json);
            req.ContentLength = b.Length;
            using (Stream s = req.GetRequestStream()) s.Write(b, 0, b.Length);
            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            using (Stream rs = resp.GetResponseStream())
            using (StreamReader r = new StreamReader(rs, Encoding.UTF8))
            {
                return r.ReadToEnd();
            }
        }

        private static string ExtractUhs(string xstsJson)
        {
            try
            {
                int i = xstsJson.IndexOf("\"uhs\"");
                if (i < 0) return null;
                int c = xstsJson.IndexOf(':', i);
                int q1 = xstsJson.IndexOf('"', c + 1);
                int q2 = xstsJson.IndexOf('"', q1 + 1);
                if (q1 < 0 || q2 < 0) return null;
                return xstsJson.Substring(q1 + 1, q2 - q1 - 1);
            }
            catch { return null; }
        }

        public static string StoredProfileName()
        {
            try
            {
                string p = Path.Combine(VyraPaths.BaseDir, ProfileFile);
                if (!File.Exists(p)) return null;
                return MiniJson.ExtractString(File.ReadAllText(p, Encoding.UTF8), "name", null);
            }
            catch { return null; }
        }

        public static McAccount StoredAccount(string fallbackName)
        {
            McAccount a = new McAccount();
            try
            {
                string p = Path.Combine(VyraPaths.BaseDir, ProfileFile);
                if (File.Exists(p))
                {
                    string json = File.ReadAllText(p, Encoding.UTF8);
                    a.Name = MiniJson.ExtractString(json, "name", fallbackName);
                    a.Uuid = MiniJson.ExtractString(json, "id", InstanceInstaller.OfflineUuid(a.Name));
                    a.AccessToken = DpapiStore.Load("mc_token.dat");
                    a.Online = a.AccessToken != null;
                    if (!a.Online) a.AccessToken = "-";
                    return a;
                }
            }
            catch { }
            a.Name = fallbackName;
            a.Uuid = InstanceInstaller.OfflineUuid(fallbackName);
            a.AccessToken = "-";
            a.Online = false;
            return a;
        }

        public static void Logout()
        {
            DpapiStore.Delete(TokenFile);
            DpapiStore.Delete("ms_refresh.dat");
            DpapiStore.Delete("mc_token.dat");
            try { File.Delete(Path.Combine(VyraPaths.BaseDir, ProfileFile)); } catch { }
        }
    }

    internal static class HttpBearerExt
    {
        public static string Get(string url, int timeoutMs, string bearer)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "GET";
            req.Timeout = timeoutMs;
            req.UserAgent = "VYRA-Client/0.2";
            req.Headers["Authorization"] = "Bearer " + bearer;
            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            using (Stream s = resp.GetResponseStream())
            using (StreamReader r = new StreamReader(s, Encoding.UTF8))
            {
                return r.ReadToEnd();
            }
        }
    }
}

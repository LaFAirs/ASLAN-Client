using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

namespace Aslan
{
    public sealed class DownloadItem
    {
        public string Label;
        public string Url;
        public string Dest;
        public string Status = "wartend"; // wartend | aktiv | fertig | fehler | abgebrochen
        public string Error;
        public long Done;
        public long Total;
        public double SpeedBps;
        public int Attempts;
    }

    // Globaler Download-Manager: Warteschlange, echte Fortschritte/Geschwindigkeit,
    // Retry, Abbrechen. Eine Aufgabe nach der anderen (fair + verbindungsschonend).
    public static class DownloadCenter
    {
        private static readonly object LockObj = new object();
        private static List<DownloadItem> items = new List<DownloadItem>();
        private static bool workerRunning = false;
        public static event Action Changed;

        public static List<DownloadItem> Snapshot()
        {
            lock (LockObj)
            {
                List<DownloadItem> copy = new List<DownloadItem>(items.Count);
                for (int i = 0; i < items.Count; i++) copy.Add(items[i]);
                return copy;
            }
        }

        private static void Fire() { try { if (Changed != null) Changed(); } catch { } }

        public static DownloadItem Enqueue(string label, string url, string dest)
        {
            DownloadItem it = new DownloadItem();
            it.Label = label;
            it.Url = url;
            it.Dest = dest;
            lock (LockObj) { items.Add(it); if (items.Count > 200) items.RemoveAt(0); }
            EnsureWorker();
            Fire();
            return it;
        }

        private static void EnsureWorker()
        {
            lock (LockObj)
            {
                if (workerRunning) return;
                workerRunning = true;
            }
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    while (true)
                    {
                        DownloadItem next = null;
                        lock (LockObj)
                        {
                            for (int i = 0; i < items.Count; i++)
                                if (items[i].Status == "wartend") { next = items[i]; break; }
                            if (next == null) { workerRunning = false; break; }
                            next.Status = "aktiv";
                        }
                        Fire();
                        try
                        {
                            RunItem(next, new CancellationToken());
                            next.Status = "fertig";
                        }
                        catch (Exception ex)
                        {
                            next.Attempts++;
                            if (next.Attempts < 3)
                            {
                                next.Status = "wartend";
                                next.Error = "Retry (" + ex.Message + ")";
                            }
                            else { next.Status = "fehler"; next.Error = ex.Message; }
                        }
                        Fire();
                    }
                }
                catch { lock (LockObj) { workerRunning = false; } }
            });
        }

        private static void RunItem(DownloadItem it, CancellationToken ct)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(it.Url);
            req.Method = "GET";
            req.Timeout = 30000;
            req.UserAgent = "ASLAN-Client/0.3";
            req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
            using (Stream src = resp.GetResponseStream())
            {
                it.Total = resp.ContentLength;
                Directory.CreateDirectory(Path.GetDirectoryName(it.Dest));
                string tmp = it.Dest + ".part";
                using (FileStream fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    byte[] buf = new byte[65536];
                    long done = 0;
                    long t0 = DateTime.UtcNow.Ticks / 10000L;
                    while (true)
                    {
                        if (ct.IsCancellationRequested) throw new OperationCanceledException();
                        int n = src.Read(buf, 0, buf.Length);
                        if (n <= 0) break;
                        fs.Write(buf, 0, n);
                        done += n;
                        it.Done = done;
                        long now = DateTime.UtcNow.Ticks / 10000L;
                        if (now > t0 + 500) { it.SpeedBps = done * 1000.0 / Math.Max(1, now - t0); }
                    }
                }
                if (File.Exists(it.Dest)) File.Delete(it.Dest);
                File.Move(tmp, it.Dest);
            }
        }

        // Direkter Download mit Fortschritt (für Installations-Flows).
        public static void Download(string url, string dest, DownloadProgress prog, CancellationToken ct)
        {
            Http.Download(url, dest, prog, ct);
        }

        public static void CancelAll()
        {
            lock (LockObj)
            {
                for (int i = 0; i < items.Count; i++)
                    if (items[i].Status == "wartend" || items[i].Status == "aktiv")
                    { items[i].Status = "abgebrochen"; }
            }
            Fire();
        }

        public static void ClearDone()
        {
            lock (LockObj)
            {
                for (int i = items.Count - 1; i >= 0; i--)
                    if (items[i].Status == "fertig" || items[i].Status == "fehler" || items[i].Status == "abgebrochen")
                        items.RemoveAt(i);
            }
            Fire();
        }
    }
}

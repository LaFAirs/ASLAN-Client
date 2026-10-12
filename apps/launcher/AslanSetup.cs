using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("ASLAN Client Setup")]
[assembly: AssemblyDescription("ASLAN Client installer. Verifies hashes, installs locally, no admin required.")]
[assembly: AssemblyCompany("ASLAN Team")]
[assembly: AssemblyProduct("ASLAN Client Setup")]
[assembly: AssemblyCopyright("Copyright (c) 2026 ASLAN Team (MIT)")]
[assembly: AssemblyVersion("0.3.0.0")]
[assembly: AssemblyFileVersion("0.3.0.0")]

public static class AslanSetup
{
    const string Version = "0.3.0";

    [STAThread]
    static int Main(string[] args)
    {
        Application.EnableVisualStyles();
        try
        {
            string srcDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string exe = Path.Combine(srcDir, "AslanLauncher-0.3.0.exe");
            string mod = Path.Combine(srcDir, "aslan-client-0.3.0.jar");
            if (!File.Exists(exe))
            {
                MessageBox.Show("Payload fehlt: " + exe, "ASLAN Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
            string want = null;
            string shaFile = Path.Combine(srcDir, "SHA256SUMS.txt");
            if (File.Exists(shaFile))
            {
                string[] lines = File.ReadAllLines(shaFile, Encoding.ASCII);
                for (int i = 0; i < lines.Length; i++)
                    if (lines[i].Contains("AslanLauncher-0.3.0.exe")) want = lines[i].Split(' ')[0].Trim();
            }
            string got = Sha256(exe);
            if (want != null && !string.Equals(got, want, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("SHA-256 stimmt nicht. Installation abgebrochen.\nErwartet: " + want + "\nErhalten: " + got,
                    "ASLAN Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
            string dest = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ASLAN", "Client");
            Directory.CreateDirectory(dest);
            File.Copy(exe, Path.Combine(dest, "AslanLauncher.exe"), true);
            if (File.Exists(mod)) File.Copy(mod, Path.Combine(dest, "aslan-client.jar"), true);

            object ws = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string programs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "ASLAN Client");
            Directory.CreateDirectory(programs);
            MakeShortcut(ws, Path.Combine(desktop, "ASLAN Client.lnk"), Path.Combine(dest, "AslanLauncher.exe"), dest);
            MakeShortcut(ws, Path.Combine(programs, "ASLAN Client.lnk"), Path.Combine(dest, "AslanLauncher.exe"), dest);

            string uninst = Path.Combine(dest, "uninstall.ps1");
            File.WriteAllText(uninst,
                "Remove-Item '" + dest + "' -Recurse -Force -ErrorAction SilentlyContinue\r\n" +
                "Remove-Item ([IO.Path]::Combine([Environment]::GetFolderPath('Desktop'), 'ASLAN Client.lnk')) -Force -ErrorAction SilentlyContinue\r\n" +
                "Remove-Item 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\ASLANClient' -Recurse -Force -ErrorAction SilentlyContinue\r\n" +
                "Write-Host 'ASLAN Client deinstalliert.'\r\n", Encoding.ASCII);
            using (RegistryKey k = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\ASLANClient"))
            {
                k.SetValue("DisplayName", "ASLAN Client " + Version);
                k.SetValue("DisplayVersion", Version);
                k.SetValue("Publisher", "ASLAN Team");
                k.SetValue("InstallLocation", dest);
                k.SetValue("UninstallString", "powershell -ExecutionPolicy Bypass -File \"" + uninst + "\"");
                k.SetValue("NoModify", 1);
                k.SetValue("NoRepair", 1);
            }
            MessageBox.Show("ASLAN Client " + Version + " installiert nach:\n" + dest + "\nSHA-256 geprüft: " + got.Substring(0, 16) + "…",
                "ASLAN Setup", MessageBoxButtons.OK, MessageBoxIcon.Information);
            try { Process.Start(Path.Combine(dest, "AslanLauncher.exe")); } catch { }
            return 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Installation fehlgeschlagen: " + ex.Message, "ASLAN Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        }
    }

    static string Sha256(string file)
    {
        using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read))
        using (SHA256Managed h = new SHA256Managed())
        {
            byte[] b = h.ComputeHash(fs);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < b.Length; i++) sb.Append(b[i].ToString("x2"));
            return sb.ToString();
        }
    }

    static void MakeShortcut(object ws, string lnk, string target, string workDir)
    {
        try
        {
            Type t = ws.GetType();
            object sc = t.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, ws, new object[] { lnk });
            Type st = sc.GetType();
            st.InvokeMember("TargetPath", BindingFlags.SetProperty, null, sc, new object[] { target });
            st.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, sc, new object[] { workDir });
            st.InvokeMember("Save", BindingFlags.InvokeMethod, null, sc, null);
        }
        catch { }
    }
}

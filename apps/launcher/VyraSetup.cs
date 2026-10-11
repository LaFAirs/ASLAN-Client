using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

[assembly: AssemblyTitle("VYRA Client Setup")]
[assembly: AssemblyDescription("VYRA Client installer. Verifies hashes, installs locally, no admin required.")]
[assembly: AssemblyCompany("VYRA Team")]
[assembly: AssemblyProduct("VYRA Client Setup")]
[assembly: AssemblyCopyright("Copyright (c) 2026 VYRA Team (MIT)")]
[assembly: AssemblyVersion("0.2.0.0")]
[assembly: AssemblyFileVersion("0.2.0.0")]

// Echter Windows-Installer: prüft SHA-256 der Payload-Dateien, installiert nach
// %LOCALAPPDATA%\VYRA\Client, legt Desktop-/Startmenü-Verknüpfungen an und
// registriert einen Deinstallations-Eintrag (HKCU, ohne Admin).
public static class VyraSetup
{
    const string Version = "0.2.0";

    [STAThread]
    static int Main(string[] args)
    {
        Application.EnableVisualStyles();
        try
        {
            string srcDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string exe = Path.Combine(srcDir, "VyraLauncher-0.2.0.exe");
            string mod = Path.Combine(srcDir, "vyra-client-0.2.0.jar");
            if (!File.Exists(exe))
            {
                MessageBox.Show("Payload fehlt: " + exe, "VYRA Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
            string want = null;
            string shaFile = Path.Combine(srcDir, "SHA256SUMS.txt");
            if (File.Exists(shaFile))
            {
                string[] lines = File.ReadAllLines(shaFile, Encoding.ASCII);
                for (int i = 0; i < lines.Length; i++)
                    if (lines[i].Contains("VyraLauncher-0.2.0.exe")) want = lines[i].Split(' ')[0].Trim();
            }
            string got = Sha256(exe);
            if (want != null && !string.Equals(got, want, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("SHA-256 stimmt nicht. Installation abgebrochen.\nErwartet: " + want + "\nErhalten: " + got,
                    "VYRA Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
            string dest = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VYRA", "Client");
            Directory.CreateDirectory(dest);
            File.Copy(exe, Path.Combine(dest, "VyraLauncher.exe"), true);
            if (File.Exists(mod)) File.Copy(mod, Path.Combine(dest, "vyra-client.jar"), true);

            object ws = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            string programs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Start Menu", "Programs", "VYRA Client");
            Directory.CreateDirectory(programs);
            MakeShortcut(ws, Path.Combine(desktop, "VYRA Client.lnk"), Path.Combine(dest, "VyraLauncher.exe"), dest);
            MakeShortcut(ws, Path.Combine(programs, "VYRA Client.lnk"), Path.Combine(dest, "VyraLauncher.exe"), dest);

            string uninst = Path.Combine(dest, "uninstall.ps1");
            File.WriteAllText(uninst,
                "Remove-Item '" + dest + "' -Recurse -Force -ErrorAction SilentlyContinue\r\n" +
                "Remove-Item ([IO.Path]::Combine([Environment]::GetFolderPath('Desktop'), 'VYRA Client.lnk')) -Force -ErrorAction SilentlyContinue\r\n" +
                "Remove-Item 'HKCU:\\Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\VYRAClient' -Recurse -Force -ErrorAction SilentlyContinue\r\n" +
                "Write-Host 'VYRA Client deinstalliert.'\r\n", Encoding.ASCII);
            using (RegistryKey k = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\VYRAClient"))
            {
                k.SetValue("DisplayName", "VYRA Client " + Version);
                k.SetValue("DisplayVersion", Version);
                k.SetValue("Publisher", "VYRA Team");
                k.SetValue("InstallLocation", dest);
                k.SetValue("UninstallString", "powershell -ExecutionPolicy Bypass -File \"" + uninst + "\"");
                k.SetValue("NoModify", 1);
                k.SetValue("NoRepair", 1);
            }
            MessageBox.Show("VYRA Client " + Version + " installiert nach:\n" + dest + "\nSHA-256 geprüft: " + got.Substring(0, 16) + "…",
                "VYRA Setup", MessageBoxButtons.OK, MessageBoxIcon.Information);
            try { Process.Start(Path.Combine(dest, "VyraLauncher.exe")); } catch { }
            return 0;
        }
        catch (Exception ex)
        {
            MessageBox.Show("Installation fehlgeschlagen: " + ex.Message, "VYRA Setup", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            object sc = t.InvokeMember("CreateShortcut", System.Reflection.BindingFlags.InvokeMethod, null, ws, new object[] { lnk });
            Type st = sc.GetType();
            st.InvokeMember("TargetPath", System.Reflection.BindingFlags.SetProperty, null, sc, new object[] { target });
            st.InvokeMember("WorkingDirectory", System.Reflection.BindingFlags.SetProperty, null, sc, new object[] { workDir });
            st.InvokeMember("Save", System.Reflection.BindingFlags.InvokeMethod, null, sc, null);
        }
        catch { }
    }
}

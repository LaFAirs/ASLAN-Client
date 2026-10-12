using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

[assembly: AssemblyTitle("ASLAN Client")]
[assembly: AssemblyDescription("PLAY BEYOND LIMITS. Independent third-party Minecraft launcher.")]
[assembly: AssemblyCompany("ASLAN Team")]
[assembly: AssemblyProduct("ASLAN Client")]
[assembly: AssemblyCopyright("Copyright (c) 2026 ASLAN Team (MIT)")]
[assembly: AssemblyVersion("0.3.0.0")]
[assembly: AssemblyFileVersion("0.3.0.0")]

namespace Aslan
{
    static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--self-test") return SelfTest.Run();
                if (args[i] == "--version") { Console.WriteLine("ASLAN Client 0.3.0"); return 0; }
                if (args[i] == "--migrate") { Console.WriteLine(AslanPaths.MigrateFromVyra()); return 0; }
            }
            // Stille Erst-Migration (kopiert nur, löscht nichts)
            try
            {
                if (!File.Exists(Path.Combine(AslanPaths.BaseDir, "migrated.txt")))
                    AslanPaths.MigrateFromVyra();
            }
            catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            AslanConfig cfg = AslanConfig.Load();
            Application.Run(new AslanForm(cfg));
            return 0;
        }
    }

    static class SelfTest
    {
        static int failures = 0;

        static void Check(bool ok, string name)
        {
            Console.WriteLine((ok ? "PASS " : "FAIL ") + name);
            if (!ok) failures++;
        }

        public static int Run()
        {
            Console.WriteLine("ASLAN self-test 0.3.0");
            AslanConfig c = new AslanConfig();
            AslanProfile g = new AslanProfile();
            g.Name = "Test";
            g.MinecraftVersion = "1.21.11";
            g.Loader = "fabric";
            g.RamMB = 4096;
            c.Profiles.Clear();
            c.Profiles.Add(g);
            c.ActiveProfile = "Test";
            string json = c.Serialize();
            AslanConfig c2 = new AslanConfig();
            MiniJson.ParseConfig(json, c2);
            Check(c2.Profiles.Count == 1 && c2.Profiles[0].RamMB == 4096, "config-roundtrip");
            Check(c2.GetActive().Loader == "fabric", "config-loader");

            HardwareInfo hw = HardwareInfo.Detect();
            Check(hw.CpuCores >= 1, "hw-cores");
            Check(hw.RecommendedRamMB() >= 1024 && hw.RecommendedRamMB() <= 8192, "hw-ram-range");

            PerfSettings max = PerfSettings.ForProfile(PerfProfile.MaxFps, hw);
            PerfSettings q = PerfSettings.ForProfile(PerfProfile.Quality, hw);
            Check(max.RenderDistance <= q.RenderDistance, "perf-order");

            List<string> sug = LoaderMatrix.SuggestFor("1.21.11");
            Check(sug.Contains("vanilla") && sug.Contains("fabric") && sug.Contains("forge") && sug.Contains("neoforge") && sug.Contains("quilt"), "loader-matrix");
            Check(LoaderMatrix.All.Length == 5, "loader-count");

            List<string> set = new List<string>(new string[] { "sodium-0.6.jar", "optifine-1.21.jar" });
            Check(ModConflictRules.CheckSet(set) != null, "conflict-sodium-optifine");

            List<double> fps = new List<double>();
            for (int i = 0; i < 100; i++) fps.Add(120.0);
            fps[0] = 20.0;
            BenchmarkResult r = BenchmarkResult.FromFpsSamples(fps, 3000, TimeSpan.FromSeconds(12));
            Check(r.AvgFps > 100 && r.Low1Fps < 60, "benchmark-1pct-low");

            string sample = "[{\"id\":\"abc\",\"version_number\":\"0.6.0\",\"game_versions\":[\"1.21.11\"],\"files\":[{\"url\":\"https://cdn.modrinth.com/data/x/0.6.0/sodium.jar\",\"filename\":\"sodium.jar\",\"hashes\":{\"sha512\":\"" + new string('a', 128) + "\"}}],\"dependencies\":[{\"project_id\":\"fabric-api\",\"dependency_type\":\"required\"}]}]";
            List<ModrinthVersion> vs = ModrinthService.ParseVersions(sample);
            Check(vs.Count == 1 && vs[0].Sha512.Length == 128 && vs[0].RequiredDeps.Count == 1, "modrinth-parse");

            Check(CurseForgeService.LoaderToCf("fabric") == 4, "cf-loader-fabric");
            Check(CurseForgeService.LoaderToCf("neoforge") == 6, "cf-loader-neoforge");
            Check(!CurseForgeService.HasKey() || CurseForgeService.LoadKey() != null, "cf-key-roundtrip");

            Check(InstanceInstaller.MavenPath("com.mojang:logging:1.0.0", ".jar") == "com/mojang/logging/1.0.0/logging-1.0.0.jar", "maven-path");
            Check(!InstanceInstaller.AllowedByRules("[{\"action\":\"allow\",\"features\":{\"is_demo_user\":true}}]"), "rules-features");
            string uuid = InstanceInstaller.OfflineUuid("Spieler");
            Guid gg;
            Check(Guid.TryParse(uuid, out gg), "offline-uuid");
            Check(CrashAnalyzer.Analyze("java.lang.OutOfMemoryError: Java heap space").Count > 0, "crash-oom");
            Check(UpdateChecker.CompareVersions("0.3.0", "0.2.0") > 0, "vercmp");
            Check(MinecraftService.SafeName("Mein Profil 1!") == "Mein-Profil-1", "safename");
            Check(UpdateChecker.CurrentVersion == "0.3.0", "version-030");

            Console.WriteLine(failures == 0 ? "SELF-TEST OK" : ("SELF-TEST FAILURES: " + failures));
            return failures == 0 ? 0 : 1;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

[assembly: AssemblyTitle("VYRA Client")]
[assembly: AssemblyDescription("Your Minecraft. Your Performance. Your Rules. Independent third-party Minecraft launcher.")]
[assembly: AssemblyCompany("VYRA Team")]
[assembly: AssemblyProduct("VYRA Client")]
[assembly: AssemblyCopyright("Copyright (c) 2026 VYRA Team (MIT)")]
[assembly: AssemblyVersion("0.2.0.0")]
[assembly: AssemblyFileVersion("0.2.0.0")]

namespace Vyra
{
    static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--self-test") return SelfTest.Run();
                if (args[i] == "--version") { Console.WriteLine("VYRA Client 0.2.0"); return 0; }
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            VyraConfig cfg = VyraConfig.Load();
            Application.Run(new MainForm(cfg));
            return 0;
        }
    }

    // Headless-Selbsttests: prüfen Kernlogik ohne UI (für scripts/test.ps1).
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
            Console.WriteLine("VYRA self-test 0.2.0");
            VyraConfig c = new VyraConfig();
            GameProfile g = new GameProfile();
            g.Name = "Test";
            g.MinecraftVersion = "26.2";
            g.RamMB = 4096;
            c.Profiles.Clear();
            c.Profiles.Add(g);
            c.ActiveProfile = "Test";
            string json = c.Serialize();
            VyraConfig c2 = new VyraConfig();
            MiniJson.ParseConfig(json, c2);
            Check(c2.Profiles.Count == 1 && c2.Profiles[0].RamMB == 4096, "config-roundtrip");
            Check(c2.GetActive().MinecraftVersion == "26.2", "config-mc-version");

            HardwareInfo hw = HardwareInfo.Detect();
            Check(hw.CpuCores >= 1, "hw-cores");
            Check(hw.RecommendedRamMB() >= 1024 && hw.RecommendedRamMB() <= 8192, "hw-ram-range");

            PerfSettings max = PerfSettings.ForProfile(PerfProfile.MaxFps, hw);
            PerfSettings q = PerfSettings.ForProfile(PerfProfile.Quality, hw);
            Check(max.RenderDistance <= q.RenderDistance, "perf-order");
            Check(!max.ShadersAllowed, "perf-maxfps-no-shader");

            List<string> set = new List<string>(new string[] { "sodium-0.6.jar", "optifine-1.21.jar" });
            Check(ModConflictRules.CheckSet(set) != null, "conflict-sodium-optifine");
            List<string> ok = new List<string>(new string[] { "sodium-0.6.jar", "lithium-0.14.jar", "ferrite-core-7.jar" });
            Check(ModConflictRules.CheckSet(ok) == null, "conflict-clean-set");
            Check(ModConflictRules.CheckPair("iris-1.7.jar", "optifine.jar") != null, "conflict-iris-optifine");
            Check(ModConflictRules.PerfSet262.Length == 10, "perfset-count");

            List<double> fps = new List<double>();
            for (int i = 0; i < 100; i++) fps.Add(120.0);
            fps[0] = 20.0; fps[1] = 25.0;
            BenchmarkResult r = BenchmarkResult.FromFpsSamples(fps, 3000, TimeSpan.FromSeconds(12));
            Check(r.AvgFps > 100 && r.Low1Fps < 60, "benchmark-1pct-low");
            Check(r.Spikes >= 0, "benchmark-spikes");

            string sample = "[{\"id\":\"abc\",\"version_number\":\"0.6.0\",\"game_versions\":[\"26.2\"],\"files\":[{\"url\":\"https://cdn.modrinth.com/data/x/0.6.0/sodium.jar\",\"filename\":\"sodium.jar\",\"hashes\":{\"sha512\":\"" + new string('a', 128) + "\"}}],\"dependencies\":[{\"project_id\":\"fabric-api\",\"dependency_type\":\"required\"}]}]";
            List<ModrinthVersion> vs = ModrinthService.ParseVersions(sample);
            Check(vs.Count == 1 && vs[0].FileName == "sodium.jar", "modrinth-parse");
            Check(vs.Count == 1 && vs[0].Sha512 != null && vs[0].Sha512.Length == 128, "modrinth-sha512");
            Check(vs.Count == 1 && vs[0].RequiredDeps.Count == 1, "modrinth-deps");

            try
            {
                List<JavaInstall> js = JavaFinder.FindAll();
                Check(true, "java-finder-runs (" + js.Count + " gefunden)");
            }
            catch (Exception ex) { Check(false, "java-finder-runs: " + ex.Message); }

            Check(MinecraftService.SafeName("Mein Profil 1!") == "Mein-Profil-1", "safename");

            // 0.2.0: Mojang-Pipeline-Hilfen (offline, feste Samples)
            Check(InstanceInstaller.MavenPath("com.mojang:logging:1.0.0", ".jar") == "com/mojang/logging/1.0.0/logging-1.0.0.jar", "maven-path");
            Check(InstanceInstaller.AllowedByRules(null), "rules-null-allow");
            Check(InstanceInstaller.AllowedByRules("[{\"action\":\"allow\"}]"), "rules-allow");
            Check(!InstanceInstaller.AllowedByRules("[{\"action\":\"allow\",\"os\":{\"name\":\"osx\"}}]"), "rules-os-mismatch");
            Check(!InstanceInstaller.AllowedByRules("[{\"action\":\"allow\",\"features\":{\"is_demo_user\":true}}]"), "rules-features-excluded");
            Check(InstanceInstaller.AllowedByRules("[{\"action\":\"allow\",\"os\":{\"name\":\"windows\",\"versionRange\":{\"min\":\"10.0.0\"}}}]"), "rules-versionrange");
            Check(InstanceInstaller.RequiredJavaMajor("{\"javaVersion\":{\"majorVersion\":25}}") == 25, "required-java");
            Check(!InstanceInstaller.AllowedByRules("[{\"action\":\"allow\"},{\"action\":\"disallow\",\"os\":{\"name\":\"windows\"}}]"), "rules-last-wins");
            string libsSample = "{\"libraries\":[{\"name\":\"a:b:1\",\"downloads\":{\"artifact\":{\"path\":\"a/b/1/b-1.jar\",\"sha1\":\"x\",\"size\":5,\"url\":\"https://libraries.minecraft.net/a/b/1/b-1.jar\"}}}]}";
            Check(InstanceInstaller.ParseLibraries(libsSample).Count == 1, "parse-libraries");
            string uuid = InstanceInstaller.OfflineUuid("Spieler");
            Guid gg;
            Check(Guid.TryParse(uuid, out gg) && uuid.Length == 36, "offline-uuid");
            List<CrashFinding> fs = CrashAnalyzer.Analyze("java.lang.OutOfMemoryError: Java heap space");
            Check(fs.Count > 0 && fs[0].Fix.Length > 10, "crash-oom");
            Check(CrashAnalyzer.Analyze("GLFW error 65542: WGL").Count > 0, "crash-glfw");
            Check(CrashAnalyzer.Analyze("requires fabric-api which is missing").Count > 0, "crash-dep");
            Check(UpdateChecker.CompareVersions("0.2.0", "0.1.0") > 0, "vercmp-newer");
            Check(UpdateChecker.CompareVersions("0.2.0", "0.2.0") == 0, "vercmp-equal");
            Check(UpdateChecker.CompareVersions("0.10.0", "0.9.0") > 0, "vercmp-minor");

            Console.WriteLine(failures == 0 ? "SELF-TEST OK" : ("SELF-TEST FAILURES: " + failures));
            return failures == 0 ? 0 : 1;
        }
    }
}

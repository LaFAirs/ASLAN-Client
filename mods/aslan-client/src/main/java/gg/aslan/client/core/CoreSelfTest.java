package gg.aslan.client.core;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;

/** Abhängigkeitsfreie Tests, lauffähig mit plain `java` (für scripts/test.ps1). */
public final class CoreSelfTest {
    private static int failures = 0;

    private static void check(boolean ok, String name) {
        System.out.println((ok ? "PASS " : "FAIL ") + name);
        if (!ok) failures++;
    }

    public static void main(String[] args) {
        List<AslanProfile> ps = new ArrayList<AslanProfile>();
        AslanProfile p = new AslanProfile("Test");
        p.minecraftVersion = "26.2";
        p.ramMB = 4096;
        ps.add(p);
        String json = AslanConfigIo.serialize(ps, "Test");
        check(json.contains("\"ram\":4096"), "config-serialize");
        check(AslanConfigIo.str("{\"mc\":\"26.2\"}", "mc", "?").equals("26.2"), "config-parse");

        HudLayout pvp = HudLayout.pvp();
        HudLayout surv = HudLayout.survival();
        check(pvp.get("fps") != null && pvp.get("cps") != null, "hud-pvp-widgets");
        check(surv.get("coords") != null && surv.get("biome") != null, "hud-survival-widgets");
        HudLayout.Widget w = pvp.get("fps");
        w.x = 0.013;
        pvp.snapToGrid(0.01);
        check(Math.abs(w.x - 0.01) < 1e-9, "hud-snap");

        check(PerformanceEngine.recommendedRamMB(16384) == 4096, "perf-ram-16g");
        check(PerformanceEngine.recommendedRamMB(4096) == 2048, "perf-ram-4g");
        PerformanceEngine.Settings max = PerformanceEngine.forProfile(
                PerformanceEngine.Profile.MAX_FPS, PerformanceEngine.Tier.LOW);
        PerformanceEngine.Settings q = PerformanceEngine.forProfile(
                PerformanceEngine.Profile.QUALITY, PerformanceEngine.Tier.HIGH);
        check(max.renderDistance <= q.renderDistance, "perf-order");
        check(!max.shadersAllowed, "perf-maxfps-no-shader");
        check(PerformanceEngine.resolveBackend(PerformanceEngine.Backend.VULKAN, false, false)
                == PerformanceEngine.Backend.OPENGL, "perf-vulkan-fallback");
        check(PerformanceEngine.resolveBackend(PerformanceEngine.Backend.VULKAN, true, true)
                == PerformanceEngine.Backend.OPENGL, "perf-crash-fallback");

        List<Double> fps = new ArrayList<Double>();
        for (int i = 0; i < 100; i++) fps.add(120.0);
        fps.set(0, 20.0);
        fps.set(1, 25.0);
        BenchmarkMath.Result r = BenchmarkMath.fromFpsSamples(fps, 3000);
        check(r.avgFps > 100 && r.low1Fps < 60, "bench-1pct");
        check(r.samples == 100, "bench-samples");

        check(ModCompat.checkSet(Arrays.asList("sodium-0.6.jar", "optifine.jar")) != null, "compat-so");
        check(ModCompat.checkSet(Arrays.asList("sodium-0.6.jar", "lithium-0.14.jar")) == null, "compat-clean");

        System.out.println(failures == 0 ? "CORE-TEST OK" : ("CORE-TEST FAILURES: " + failures));
        if (failures != 0) System.exit(1);
    }
}

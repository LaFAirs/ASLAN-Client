package gg.aslan.client.core;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

/** Benchmark-Mathematik: Avg-FPS, 1%-Low, Frametimes, Spikes. */
public final class BenchmarkMath {
    private BenchmarkMath() {}

    public static final class Result {
        public double avgFps;
        public double low1Fps;
        public double avgFrametimeMs;
        public long ramUsedMB;
        public int samples;
        public int spikes;
    }

    public static Result fromFpsSamples(List<Double> fps, long ramMB) {
        Result r = new Result();
        r.samples = fps.size();
        r.ramUsedMB = ramMB;
        if (fps.isEmpty()) return r;
        List<Double> sorted = new ArrayList<Double>(fps);
        Collections.sort(sorted);
        double sum = 0;
        for (double v : sorted) sum += v;
        r.avgFps = sum / sorted.size();
        int n = Math.max(1, (int) Math.ceil(sorted.size() * 0.01));
        double low = 0;
        for (int i = 0; i < n; i++) low += sorted.get(i);
        r.low1Fps = low / n;
        double ft = 0;
        int spikes = 0;
        for (double v : sorted) {
            double ms = v > 0 ? 1000.0 / v : 1000.0;
            ft += ms;
            if (ms > 50.0) spikes++;
        }
        r.avgFrametimeMs = ft / sorted.size();
        r.spikes = spikes;
        return r;
    }
}

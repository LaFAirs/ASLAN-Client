package gg.aslan.client.util;

import gg.aslan.client.core.BenchmarkMath;
import java.util.ArrayList;
import java.util.List;

/** Echte Benchmark-Aufzeichnung: FPS-Samples → Avg, 1%-Low, Frametimes, Spikes. */
public final class BenchmarkRecorder {
    private final List<Double> samples = new ArrayList<Double>();
    private long lastNs;
    private long windowStartMs;
    private int windowSeconds;
    private boolean running;
    private String lastSummary;

    public synchronized void frame() {
        long now = System.nanoTime();
        if (lastNs != 0) {
            double dtMs = (now - lastNs) / 1_000_000.0;
            if (dtMs > 0.5 && dtMs < 1000) {
                double fps = 1000.0 / dtMs;
                samples.add(fps);
                if (samples.size() > 20000) samples.remove(0);
            }
        }
        lastNs = now;
        if (running && System.currentTimeMillis() - windowStartMs > windowSeconds * 1000L) {
            running = false;
            finish();
        }
    }

    public synchronized void start(int seconds) {
        samples.clear();
        windowSeconds = seconds;
        windowStartMs = System.currentTimeMillis();
        running = true;
        lastSummary = null;
    }

    private void finish() {
        Runtime rt = Runtime.getRuntime();
        long used = (rt.totalMemory() - rt.freeMemory()) / 1048576L;
        BenchmarkMath.Result r = BenchmarkMath.fromFpsSamples(new ArrayList<Double>(samples), used);
        lastSummary = String.format("Avg %.0f FPS | 1%%-Low %.0f | %.1f ms | Spikes %d | RAM %d MB (%d Samples)",
                r.avgFps, r.low1Fps, r.avgFrametimeMs, r.spikes, r.ramUsedMB, r.samples);
    }

    public synchronized String summary() { return lastSummary; }
}

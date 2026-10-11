package gg.vyra.client.performance;

import gg.vyra.client.core.PerformanceEngine;
import net.minecraft.client.MinecraftClient;

import java.io.File;

/**
 * Clientseitige Performance-Engine: wendet Profile auf echte
 * Minecraft-Optionen an (Sichtweiten, Grafik, Partikel, FPS-Limit, VSync).
 * Alles reversibel; Einstellungen werden in config/vyra-perf.json gespeichert.
 */
public final class ClientPerformance {
    private PerformanceEngine.Profile current = PerformanceEngine.Profile.BALANCED;

    public void applySavedOr(PerformanceEngine.Profile def) {
        PerformanceEngine.Profile p = load();
        apply(p == null ? def : p);
    }

    public void apply(PerformanceEngine.Profile p) {
        current = p;
        MinecraftClient mc = MinecraftClient.getInstance();
        PerformanceEngine.Tier tier = estimateTier();
        PerformanceEngine.Settings s = PerformanceEngine.forProfile(p, tier);
        if (mc != null && mc.options != null) {
            try {
                mc.options.getViewDistance().setValue(s.renderDistance);
            } catch (Throwable ignored) {
            }
            try {
                mc.options.getSimulationDistance().setValue(s.simulationDistance);
            } catch (Throwable ignored) {
            }
            try {
                mc.options.applyGraphicsMode(s.fancyGraphics
                        ? net.minecraft.client.option.GraphicsMode.FANCY
                        : net.minecraft.client.option.GraphicsMode.FAST);
            } catch (Throwable ignored) {
            }
            try {
                mc.options.getParticles().setValue(s.particles
                        ? net.minecraft.particle.ParticlesMode.ALL
                        : net.minecraft.particle.ParticlesMode.MINIMAL);
            } catch (Throwable ignored) {
            }
            try {
                mc.options.getMaxFps().setValue(s.maxFps == 0 ? 260 : s.maxFps);
            } catch (Throwable ignored) {
            }
            try {
                mc.options.getEnableVsync().setValue(s.vsync);
            } catch (Throwable ignored) {
            }
        }
        save(p);
    }

    public PerformanceEngine.Profile current() { return current; }

    private PerformanceEngine.Tier estimateTier() {
        int cores = Runtime.getRuntime().availableProcessors();
        long totalMB = Runtime.getRuntime().maxMemory() / 1048576L;
        String gpu = "";
        try {
            gpu = org.lwjgl.opengl.GL11.glGetString(org.lwjgl.opengl.GL11.GL_RENDERER);
            if (gpu == null) gpu = "";
        } catch (Throwable ignored) {
        }
        String g = gpu.toLowerCase();
        boolean weak = g.contains("intel") || g.contains("uhd") || g.contains("vega 3");
        return PerformanceEngine.tier(cores, totalMB, weak);
    }

    private File file() {
        return new File(MinecraftClient.getInstance().runDirectory, "config/vyra-perf.json");
    }

    private void save(PerformanceEngine.Profile p) {
        try {
            File f = file();
            f.getParentFile().mkdirs();
            java.nio.file.Files.write(f.toPath(), ("{\"perf\":\"" + p.name() + "\"}").getBytes("UTF-8"));
        } catch (Exception ignored) {
        }
    }

    private PerformanceEngine.Profile load() {
        try {
            File f = file();
            if (!f.exists()) return null;
            String s = new String(java.nio.file.Files.readAllBytes(f.toPath()), "UTF-8");
            for (PerformanceEngine.Profile p : PerformanceEngine.Profile.values()) {
                if (s.contains(p.name())) return p;
            }
        } catch (Exception ignored) {
        }
        return null;
    }
}

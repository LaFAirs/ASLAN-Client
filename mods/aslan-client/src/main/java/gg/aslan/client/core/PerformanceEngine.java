package gg.aslan.client.core;

/** ASLAN Performance Engine: Profile, Hardware-Stufen, Backend-Regeln. */
public final class PerformanceEngine {
    private PerformanceEngine() {}

    public enum Profile { MAX_FPS, BALANCED, QUALITY, CUSTOM }
    public enum Tier { LOW, MEDIUM, HIGH }
    public enum Backend { OPENGL, VULKAN }

    public static final class Settings {
        public Profile profile = Profile.BALANCED;
        public int renderDistance = 8;
        public int simulationDistance = 6;
        public boolean fancyGraphics = false;
        public boolean smoothLighting = true;
        public boolean particles = true;
        public boolean vsync = false;
        public int maxFps = 120;
        public boolean shadersAllowed = false;
        public Backend backend = Backend.OPENGL;
    }

    public static Tier tier(int cpuCores, long ramMB, boolean weakGpu) {
        if (cpuCores <= 4 && (ramMB <= 6144 || weakGpu)) return Tier.LOW;
        if (cpuCores <= 4 || ramMB <= 6144 || weakGpu) return Tier.MEDIUM;
        return Tier.HIGH;
    }

    public static int recommendedRamMB(long totalRamMB) {
        if (totalRamMB <= 0) return 2048;
        if (totalRamMB <= 4096) return 2048;
        if (totalRamMB <= 8192) return 3072;
        if (totalRamMB <= 16384) return 4096;
        return 6144;
    }

    public static Settings forProfile(Profile p, Tier t) {
        Settings s = new Settings();
        s.profile = p;
        switch (p) {
            case MAX_FPS:
                s.renderDistance = 5; s.simulationDistance = 4;
                s.fancyGraphics = false; s.smoothLighting = false;
                s.particles = false; s.vsync = false; s.maxFps = 0;
                s.shadersAllowed = false;
                break;
            case BALANCED:
                s.renderDistance = 8; s.simulationDistance = 6;
                s.fancyGraphics = false; s.smoothLighting = true;
                s.particles = true; s.vsync = false; s.maxFps = 120;
                s.shadersAllowed = false;
                break;
            case QUALITY:
                s.renderDistance = 12; s.simulationDistance = 8;
                s.fancyGraphics = true; s.smoothLighting = true;
                s.particles = true; s.vsync = true; s.maxFps = 60;
                s.shadersAllowed = true;
                break;
            default:
                break;
        }
        if (t == Tier.LOW && p != Profile.CUSTOM) {
            s.renderDistance = Math.min(s.renderDistance, 6);
            s.simulationDistance = Math.min(s.simulationDistance, 4);
            s.shadersAllowed = false;
        }
        return s;
    }

    /** Vulkan ist experimentell: nur wenn Treiber es meldet, sonst Fallback OpenGL. */
    public static Backend resolveBackend(Backend wanted, boolean vulkanSupported, boolean lastCrashWasGfx) {
        if (lastCrashWasGfx) return Backend.OPENGL;
        if (wanted == Backend.VULKAN && vulkanSupported) return Backend.VULKAN;
        return Backend.OPENGL;
    }
}

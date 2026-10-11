package gg.vyra.client.util;

import net.minecraft.client.MinecraftClient;
import java.io.File;

/** Safe Mode: deaktiviert optionale Mods (außer Fabric-API + VYRA), ohne Welten/Settings zu löschen. */
public final class SafeMode {
    private SafeMode() {}

    public static boolean requested() {
        return Boolean.getBoolean("vyra.safeMode");
    }

    public static void applyIfRequested() {
        if (!requested()) return;
        try {
            File mods = new File(MinecraftClient.getInstance().runDirectory, "mods");
            File[] files = mods.listFiles();
            if (files == null) return;
            for (File f : files) {
                String n = f.getName().toLowerCase();
                if (!f.isFile() || !n.endsWith(".jar")) continue;
                if (n.contains("fabric-api") || n.contains("vyra-client") || n.contains("fabric-loader")) continue;
                File off = new File(mods, f.getName() + ".disabled-by-safemode");
                f.renameTo(off);
            }
        } catch (Exception ignored) {
        }
    }
}

package gg.vyra.client.util;

import net.minecraft.client.MinecraftClient;
import java.io.File;

/** Profil-Sync: Export/Import von Einstellungen als JSON (Cloud nur mit Opt-in). */
public final class ProfileSync {
    private ProfileSync() {}

    public static File exportFile() {
        File d = new File(MinecraftClient.getInstance().runDirectory, "vyra-sync");
        d.mkdirs();
        return new File(d, "vyra-profile.json");
    }

    public static String exportNow() {
        try {
            String json = "{\"hud\":\"" + gg.vyra.client.VyraClient.hud().active().name
                    + "\",\"perf\":\"" + gg.vyra.client.VyraClient.perf().current().name() + "\"}";
            java.nio.file.Files.write(exportFile().toPath(), json.getBytes("UTF-8"));
            return exportFile().getAbsolutePath();
        } catch (Exception e) {
            return null;
        }
    }

    public static boolean importNow(File f) {
        try {
            String s = new String(java.nio.file.Files.readAllBytes(f.toPath()), "UTF-8");
            if (s.contains("survival")) gg.vyra.client.VyraClient.hud().setProfile("survival");
            else gg.vyra.client.VyraClient.hud().setProfile("pvp");
            return true;
        } catch (Exception e) {
            return false;
        }
    }
}

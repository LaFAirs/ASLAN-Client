package gg.aslan.client.util;

import net.minecraft.client.MinecraftClient;
import java.io.File;

/** Profil-Sync: Export/Import von Einstellungen als JSON (Cloud nur mit Opt-in). */
public final class ProfileSync {
    private ProfileSync() {}

    public static File exportFile() {
        File d = new File(MinecraftClient.getInstance().runDirectory, "aslan-sync");
        d.mkdirs();
        return new File(d, "aslan-profile.json");
    }

    public static String exportNow() {
        try {
            String json = "{\"hud\":\"" + gg.aslan.client.AslanClient.hud().active().name
                    + "\",\"perf\":\"" + gg.aslan.client.AslanClient.perf().current().name() + "\"}";
            java.nio.file.Files.write(exportFile().toPath(), json.getBytes("UTF-8"));
            return exportFile().getAbsolutePath();
        } catch (Exception e) {
            return null;
        }
    }

    public static boolean importNow(File f) {
        try {
            String s = new String(java.nio.file.Files.readAllBytes(f.toPath()), "UTF-8");
            if (s.contains("survival")) gg.aslan.client.AslanClient.hud().setProfile("survival");
            else gg.aslan.client.AslanClient.hud().setProfile("pvp");
            return true;
        } catch (Exception e) {
            return false;
        }
    }
}

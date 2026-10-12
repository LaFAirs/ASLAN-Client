package gg.aslan.client.util;

import java.util.ArrayList;
import java.util.List;

/** Settings-Suche: durchsucht Einstellungen, Module und Keybindings (Offline-Index). */
public final class SettingsSearch {
    private SettingsSearch() {}

    private static final String[] INDEX = {
        "hud fps counter", "hud ping", "hud koordinaten coords", "hud kompass direction",
        "hud cps klicks", "hud keystrokes tasten", "hud armor rüstung", "hud potion effekte",
        "hud crosshair fadenkreuz", "hud editor layout profil pvp survival",
        "performance max fps balanced quality backend opengl vulkan",
        "mods installieren modrinth suche update entfernen safe mode",
        "shader iris installieren aktivieren", "screenshot hud ausblenden",
        "tastenbelegung quick settings v menü g", "discord presence opt-in"
    };

    public static List<String> search(String q) {
        List<String> out = new ArrayList<String>();
        String nq = q.toLowerCase().trim();
        if (nq.isEmpty()) return out;
        String[] words = nq.split("\\s+");
        for (String entry : INDEX) {
            boolean all = true;
            for (String w : words) {
                if (!entry.contains(w)) { all = false; break; }
            }
            if (all) out.add(entry);
        }
        return out;
    }
}

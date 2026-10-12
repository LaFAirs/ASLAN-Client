package gg.aslan.client.core;

import java.util.List;

/** Minimale, abhängigkeitsfreie JSON-Serialisierung für Profile (kein Gson nötig). */
public final class AslanConfigIo {
    private AslanConfigIo() {}

    public static String serialize(List<AslanProfile> profiles, String active) {
        StringBuilder sb = new StringBuilder("{\"activeProfile\":\"");
        sb.append(esc(active)).append("\",\"profiles\":[");
        for (int i = 0; i < profiles.size(); i++) {
            AslanProfile p = profiles.get(i);
            sb.append("{\"name\":\"").append(esc(p.name)).append("\",");
            sb.append("\"mc\":\"").append(esc(p.minecraftVersion)).append("\",");
            sb.append("\"loaderVersion\":\"").append(esc(p.loaderVersion)).append("\",");
            sb.append("\"ram\":").append(p.ramMB).append(",");
            sb.append("\"perf\":\"").append(esc(p.perfProfile)).append("\",");
            sb.append("\"safeMode\":").append(p.safeMode ? "true" : "false").append("}");
            if (i + 1 < profiles.size()) sb.append(",");
        }
        sb.append("]}");
        return sb.toString();
    }

    public static String esc(String s) {
        if (s == null) return "";
        return s.replace("\\", "\\\\").replace("\"", "\\\"");
    }

    /** Extrahiert einen String-Wert zu key aus flachem JSON (eigene Schema-Kontrolle). */
    public static String str(String json, String key, String def) {
        String k = "\"" + key + "\"";
        int i = json.indexOf(k);
        if (i < 0) return def;
        int c = json.indexOf(':', i);
        if (c < 0) return def;
        int q1 = json.indexOf('"', c + 1);
        if (q1 < 0) return def;
        int q2 = json.indexOf('"', q1 + 1);
        if (q2 < 0) return def;
        return json.substring(q1 + 1, q2);
    }
}

package gg.vyra.client.core;

import java.util.List;

/** Kompatibilitätsregeln für Mod-Kombinationen (konservativ, erklärend). */
public final class ModCompat {
    private ModCompat() {}

    public static String checkPair(String a, String b) {
        String x = (a + "|" + b).toLowerCase();
        if (x.contains("sodium") && x.contains("optifine")) {
            return "Sodium und OptiFine verändern beide das Rendering und sind inkompatibel.";
        }
        if (x.contains("phosphor") && x.contains("lithium")) {
            return "Phosphor ist veraltet; Lithium übernimmt diesen Bereich stabiler.";
        }
        if (x.contains("iris") && x.contains("optifine")) {
            return "Iris und OptiFine schließen sich aus (beide Shader-Pipelines).";
        }
        return null;
    }

    public static String checkSet(List<String> ids) {
        for (int i = 0; i < ids.size(); i++) {
            for (int j = i + 1; j < ids.size(); j++) {
                String r = checkPair(ids.get(i), ids.get(j));
                if (r != null) return ids.get(i) + " + " + ids.get(j) + ": " + r;
            }
        }
        return null;
    }
}

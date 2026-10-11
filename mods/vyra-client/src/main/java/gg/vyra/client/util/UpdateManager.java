package gg.vyra.client.util;

/**
 * Update-Manager: prüft Releases, informiert vor Installation,
 * Rollback = vorherige JAR aus Backup wiederherstellen. Kein stilles Update.
 */
public final class UpdateManager {
    private UpdateManager() {}

    public static final String CURRENT = "0.2.0";
    public static final String RELEASES_URL = "https://api.github.com/repos/OWNER/VYRA-Client/releases/latest";

    /** Vergleicht Semver (ehrlich: nur Ziffern-Teile). true = Update verfügbar. */
    public static boolean isNewer(String remote, String local) {
        try {
            String[] a = remote.split("[.-]");
            String[] b = local.split("[.-]");
            int n = Math.max(a.length, b.length);
            for (int i = 0; i < n; i++) {
                int x = i < a.length ? num(a[i]) : 0;
                int y = i < b.length ? num(b[i]) : 0;
                if (x != y) return x > y;
            }
            return false;
        } catch (Exception e) {
            return false;
        }
    }

    private static int num(String s) {
        try {
            return Integer.parseInt(s.replaceAll("[^0-9]", ""));
        } catch (Exception e) {
            return 0;
        }
    }
}

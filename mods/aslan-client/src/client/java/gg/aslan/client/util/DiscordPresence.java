package gg.aslan.client.util;

/**
 * Discord Rich Presence: strikt Opt-in, standardmäßig AUS.
 * Echte IPC-Anbindung erfolgt über eine optionale, separat lizenzierte
 * Bibliothek; ohne sie bleibt die Presence inaktiv (kein Fake-Status).
 */
public final class DiscordPresence {
    private static boolean enabled = false;

    private DiscordPresence() {}

    public static void setEnabled(boolean on) { enabled = on; }
    public static boolean isEnabled() { return enabled; }

    public static void update(String state) {
        if (!enabled) return;
    }
}

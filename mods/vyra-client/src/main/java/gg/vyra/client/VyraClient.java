package gg.vyra.client;

import gg.vyra.client.core.HudLayout;
import gg.vyra.client.core.PerformanceEngine;
import gg.vyra.client.hud.HudManager;
import gg.vyra.client.menu.VyraMainMenu;
import gg.vyra.client.performance.ClientPerformance;
import gg.vyra.client.util.BenchmarkRecorder;
import gg.vyra.client.util.QuickSettings;
import gg.vyra.client.util.SafeMode;
import net.fabricmc.api.ClientModInitializer;
import net.fabricmc.fabric.api.client.event.lifecycle.v1.ClientTickEvents;
import net.fabricmc.fabric.api.client.keybinding.v1.KeyBindingHelper;
import net.fabricmc.fabric.api.client.rendering.v1.HudRenderCallback;
import net.minecraft.client.MinecraftClient;
import net.minecraft.client.option.KeyBinding;
import net.minecraft.client.util.InputUtil;
import org.lwjgl.glfw.GLFW;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

/**
 * VYRA Client — Fabric-Einstiegspunkt (clientseitig).
 * Registriert HUD, Menü-Hook, Quick Settings und Performance-Engine.
 * Keine Cheats: keine Aim-/X-Ray-/KillAura-Funktionen.
 */
public final class VyraClient implements ClientModInitializer {
    public static final String MOD_ID = "vyra-client";
    public static final Logger LOG = LoggerFactory.getLogger(MOD_ID);

    private static HudManager hud;
    private static ClientPerformance perf;
    private static BenchmarkRecorder bench;
    private static KeyBinding quickKey;
    private static KeyBinding menuKey;
    /** Sitzungsbeginn (eigener Timer — keine Vanilla-API-Abhängigkeit). */
    public static final long SESSION_START = System.currentTimeMillis();

    @Override
    public void onInitializeClient() {
        LOG.info("[VYRA] Initialisiere Client 0.2.0");
        SafeMode.applyIfRequested();
        hud = new HudManager(HudLayout.pvp());
        hud.loadFromDisk();
        perf = new ClientPerformance();
        perf.applySavedOr(PerformanceEngine.Profile.BALANCED);
        bench = new BenchmarkRecorder();

        HudRenderCallback.EVENT.register((drawContext, tickDelta) -> {
            MinecraftClient mc = MinecraftClient.getInstance();
            if (mc == null || mc.player == null) return;
            bench.frame();
            hud.render(drawContext, mc);
        });

        ClientTickEvents.END_CLIENT_TICK.register(mc -> {
            if (mc == null) return;
            if (quickKey != null && quickKey.wasPressed()) QuickSettings.open();
            if (menuKey != null && menuKey.wasPressed()) VyraMainMenu.open();
            hud.tick(mc);
        });

        quickKey = KeyBindingHelper.registerKeyBinding(new KeyBinding(
                "key.vyra.quick", InputUtil.Type.KEYSYM, GLFW.GLFW_KEY_V,
                "category.vyra"));
        menuKey = KeyBindingHelper.registerKeyBinding(new KeyBinding(
                "key.vyra.menu", InputUtil.Type.KEYSYM, GLFW.GLFW_KEY_G,
                "category.vyra"));
        LOG.info("[VYRA] Bereit. HUD-Profile: pvp/survival/creative.");
    }

    public static HudManager hud() { return hud; }
    public static ClientPerformance perf() { return perf; }
    public static BenchmarkRecorder bench() { return bench; }
}

package gg.aslan.client;

import gg.aslan.client.core.HudLayout;
import gg.aslan.client.core.PerformanceEngine;
import gg.aslan.client.hud.HudManager;
import gg.aslan.client.menu.AslanMainMenu;
import gg.aslan.client.performance.ClientPerformance;
import gg.aslan.client.util.BenchmarkRecorder;
import gg.aslan.client.util.QuickSettings;
import gg.aslan.client.util.SafeMode;
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
 * ASLAN Client — Fabric-Einstiegspunkt (clientseitig).
 * Registriert HUD, Menü-Hook, Quick Settings und Performance-Engine.
 * Keine Cheats: keine Aim-/X-Ray-/KillAura-Funktionen.
 */
public final class AslanClient implements ClientModInitializer {
    public static final String MOD_ID = "aslan-client";
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
        LOG.info("[ASLAN] Initialisiere Client 0.2.0");
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
            if (menuKey != null && menuKey.wasPressed()) AslanMainMenu.open();
            hud.tick(mc);
        });

        quickKey = KeyBindingHelper.registerKeyBinding(new KeyBinding(
                "key.aslan.quick", InputUtil.Type.KEYSYM, GLFW.GLFW_KEY_V,
                KeyBinding.Category.MISC));
        menuKey = KeyBindingHelper.registerKeyBinding(new KeyBinding(
                "key.aslan.menu", InputUtil.Type.KEYSYM, GLFW.GLFW_KEY_G,
                KeyBinding.Category.MISC));
        LOG.info("[ASLAN] Bereit. HUD-Profile: pvp/survival/creative.");
    }

    public static HudManager hud() { return hud; }
    public static ClientPerformance perf() { return perf; }
    public static BenchmarkRecorder bench() { return bench; }
}

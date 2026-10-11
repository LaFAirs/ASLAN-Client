package gg.vyra.client.util;

import net.minecraft.client.MinecraftClient;
import net.minecraft.client.gui.screen.Screen;
import net.minecraft.client.gui.widget.ButtonWidget;
import net.minecraft.client.gui.DrawContext;
import net.minecraft.text.Text;
import gg.vyra.client.VyraClient;
import gg.vyra.client.core.PerformanceEngine;

/** VYRA Quick Settings (Taste V): wichtigste Schalter ohne langes Navigieren. */
public final class QuickSettings extends Screen {
    public QuickSettings() { super(Text.literal("VYRA Quick Settings")); }

    public static void open() {
        MinecraftClient mc = MinecraftClient.getInstance();
        if (mc != null) mc.setScreen(new QuickSettings());
    }

    @Override
    protected void init() {
        int y = 50;
        addRow(y, "HUD-Profil: PvP", b -> VyraClient.hud().setProfile("pvp")); y += 26;
        addRow(y, "HUD-Profil: Survival", b -> VyraClient.hud().setProfile("survival")); y += 26;
        addRow(y, "Perf: MAX FPS", b -> VyraClient.perf().apply(PerformanceEngine.Profile.MAX_FPS)); y += 26;
        addRow(y, "Perf: BALANCED", b -> VyraClient.perf().apply(PerformanceEngine.Profile.BALANCED)); y += 26;
        addRow(y, "Perf: QUALITY", b -> VyraClient.perf().apply(PerformanceEngine.Profile.QUALITY)); y += 26;
        addRow(y, "Screenshot", b -> ScreenshotStudio.take()); y += 26;
        addDrawableChild(ButtonWidget.builder(Text.literal("Schließen"), b -> close())
                .dimensions(width / 2 - 100, y + 8, 200, 22).build());
    }

    private void addRow(int y, String label, ButtonWidget.PressAction a) {
        addDrawableChild(ButtonWidget.builder(Text.literal(label), a)
                .dimensions(width / 2 - 100, y, 200, 22).build());
    }

    @Override
    public void render(DrawContext ctx, int mouseX, int mouseY, float delta) {
        renderBackground(ctx, mouseX, mouseY, delta);
        ctx.drawCenteredTextWithShadow(textRenderer, title, width / 2, 20, 0xFFFFFF);
        super.render(ctx, mouseX, mouseY, delta);
    }
}

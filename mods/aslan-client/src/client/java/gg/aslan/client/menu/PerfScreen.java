package gg.aslan.client.menu;

import gg.aslan.client.AslanClient;
import gg.aslan.client.core.PerformanceEngine;
import net.minecraft.client.gui.DrawContext;
import net.minecraft.client.gui.screen.Screen;
import net.minecraft.client.gui.widget.ButtonWidget;
import net.minecraft.text.Text;

/** Performance-Auswahl ingame (spiegelt Launcher-Profile). */
public final class PerfScreen extends Screen {
    private final Screen parent;

    public PerfScreen(Screen parent) {
        super(Text.literal("ASLAN Performance"));
        this.parent = parent;
    }

    @Override
    protected void init() {
        int y = 60;
        for (PerformanceEngine.Profile p : PerformanceEngine.Profile.values()) {
            final PerformanceEngine.Profile fp = p;
            addDrawableChild(ButtonWidget.builder(Text.literal("Profil: " + p.name()), b -> {
                AslanClient.perf().apply(fp);
            }).dimensions(width / 2 - 100, y, 200, 22).build());
            y += 26;
        }
        addDrawableChild(ButtonWidget.builder(Text.literal("Benchmark: 10 s"), b -> {
            AslanClient.bench().start(10);
            b.setMessage(Text.literal("Benchmark läuft …"));
        }).dimensions(width / 2 - 100, y + 10, 200, 22).build());
        addDrawableChild(ButtonWidget.builder(Text.literal("Zurück"), b -> close())
                .dimensions(width / 2 - 100, y + 38, 200, 22).build());
    }

    @Override
    public void render(DrawContext ctx, int mouseX, int mouseY, float delta) {
        renderBackground(ctx, mouseX, mouseY, delta);
        ctx.drawCenteredTextWithShadow(textRenderer, title, width / 2, 24, 0xFFFFFF);
        String s = AslanClient.bench().summary();
        if (s != null) ctx.drawCenteredTextWithShadow(textRenderer, Text.literal(s), width / 2, height - 60, 0x94A3B8);
        super.render(ctx, mouseX, mouseY, delta);
    }

    @Override
    public void close() {
        if (client != null) client.setScreen(parent);
    }
}

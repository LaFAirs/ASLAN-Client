package gg.vyra.client.menu;

import net.minecraft.client.gui.DrawContext;
import net.minecraft.client.gui.screen.Screen;
import net.minecraft.client.gui.widget.ButtonWidget;
import net.minecraft.text.Text;

/** Einfacher Info-Dialog (z. B. fehlendes ModMenu wird erklärt statt Fake-Button). */
public final class InfoScreen extends Screen {
    private final Screen parent;
    private final String body;

    public InfoScreen(Screen parent, String title, String body) {
        super(Text.literal(title));
        this.parent = parent;
        this.body = body;
    }

    @Override
    protected void init() {
        addDrawableChild(ButtonWidget.builder(Text.literal("Zurück"), b -> close())
                .dimensions(width / 2 - 100, height - 50, 200, 22).build());
    }

    @Override
    public void render(DrawContext ctx, int mouseX, int mouseY, float delta) {
        renderBackground(ctx, mouseX, mouseY, delta);
        ctx.drawCenteredTextWithShadow(textRenderer, title, width / 2, height / 2 - 30, 0xFFFFFF);
        ctx.drawCenteredTextWithShadow(textRenderer, Text.literal(body), width / 2, height / 2 - 10, 0x94A3B8);
        super.render(ctx, mouseX, mouseY, delta);
    }

    @Override
    public void close() {
        if (client != null) client.setScreen(parent);
    }
}

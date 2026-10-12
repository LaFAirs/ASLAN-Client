package gg.aslan.client.menu;

import gg.aslan.client.AslanClient;
import gg.aslan.client.core.HudLayout;
import net.minecraft.client.gui.DrawContext;
import net.minecraft.client.gui.screen.Screen;
import net.minecraft.client.gui.widget.ButtonWidget;
import net.minecraft.text.Text;

/** Ingame-HUD-Editor: Profilwahl, Widget-Toggles, Raster-Snap. */
public final class HudEditorScreen extends Screen {
    private final Screen parent;

    public HudEditorScreen(Screen parent) {
        super(Text.literal("ASLAN HUD-Editor"));
        this.parent = parent;
    }

    @Override
    protected void init() {
        int y = 40;
        addDrawableChild(btn(20, y, "Profil: PvP", b -> AslanClient.hud().setProfile("pvp")));
        addDrawableChild(btn(180, y, "Profil: Survival", b -> AslanClient.hud().setProfile("survival")));
        addDrawableChild(btn(340, y, "Raster-Snap", b -> AslanClient.hud().active().snapToGrid(0.02)));
        y += 30;
        HudLayout l = AslanClient.hud().active();
        int x = 20;
        for (HudLayout.Widget w : l.widgets) {
            final HudLayout.Widget fw = w;
            addDrawableChild(btn(x, y, (fw.enabled ? "[x] " : "[ ] ") + fw.id, b -> {
                fw.enabled = !fw.enabled;
                b.setMessage(Text.literal((fw.enabled ? "[x] " : "[ ] ") + fw.id));
                AslanClient.hud().saveToDisk();
            }));
            x += 165;
            if (x > width - 170) { x = 20; y += 26; }
        }
        addDrawableChild(btn(20, height - 40, "Fertig", b -> close()));
    }

    private ButtonWidget btn(int x, int y, String label, ButtonWidget.PressAction a) {
        return ButtonWidget.builder(Text.literal(label), a).dimensions(x, y, 150, 22).build();
    }

    @Override
    public void render(DrawContext ctx, int mouseX, int mouseY, float delta) {
        renderBackground(ctx, mouseX, mouseY, delta);
        ctx.drawCenteredTextWithShadow(textRenderer, title, width / 2, 12, 0xFFFFFF);
        super.render(ctx, mouseX, mouseY, delta);
    }

    @Override
    public void close() {
        if (client != null) client.setScreen(parent);
    }
}

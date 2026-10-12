package gg.aslan.client.menu;

import net.minecraft.client.MinecraftClient;
import net.minecraft.client.gui.DrawContext;
import net.minecraft.client.gui.screen.Screen;
import net.minecraft.client.gui.widget.ButtonWidget;
import net.minecraft.text.Text;

/**
 * ASLAN-Hauptmenü: ersetzt den Vanilla-Titelbildschirm (Hook via Mixin auf
 * TitleScreen — siehe aslan-client.json). Alle Einträge sind verdrahtet;
 * Hintergrund-Animation ist abschaltbar und FPS-schonend (2D-Gradient +
 * max. 40 Partikel).
 */
public final class AslanMainMenu extends Screen {
    private static boolean animation = true;
    private int ticks;
    private final float[] px = new float[40];
    private final float[] py = new float[40];
    private final float[] ps = new float[40];

    public AslanMainMenu() {
        super(Text.literal("ASLAN Client"));
        java.util.Random r = new java.util.Random(1234);
        for (int i = 0; i < px.length; i++) {
            px[i] = r.nextFloat();
            py[i] = r.nextFloat();
            ps[i] = 0.2f + r.nextFloat() * 0.8f;
        }
    }

    public static void open() {
        MinecraftClient mc = MinecraftClient.getInstance();
        if (mc != null) mc.setScreen(new AslanMainMenu());
    }

    public static void setAnimation(boolean on) { animation = on; }

    @Override
    protected void init() {
        int cx = width / 2 - 100;
        int y = height / 2 - 80;
        int step = 26;
        addDrawableChild(btn(cx, y, "Singleplayer", b -> single()));
        addDrawableChild(btn(cx, y + step, "Multiplayer", b -> multi()));
        addDrawableChild(btn(cx, y + step * 2, "HUD-Editor", b -> hudEditor()));
        addDrawableChild(btn(cx, y + step * 3, "Performance", b -> perf()));
        addDrawableChild(btn(cx, y + step * 4, "Mods (ModMenu, falls installiert)", b -> mods()));
        addDrawableChild(btn(cx, y + step * 5, "Einstellungen", b -> settings()));
        addDrawableChild(btn(cx, y + step * 6, "Animation: " + (animation ? "AN" : "AUS"), b -> {
            animation = !animation;
            b.setMessage(Text.literal("Animation: " + (animation ? "AN" : "AUS")));
        }));
    }

    private ButtonWidget btn(int x, int y, String label, ButtonWidget.PressAction a) {
        return ButtonWidget.builder(Text.literal(label), a).dimensions(x, y, 200, 22).build();
    }

    private void single() {
        MinecraftClient mc = MinecraftClient.getInstance();
        mc.setScreen(new net.minecraft.client.gui.screen.world.SelectWorldScreen(this));
    }

    private void multi() {
        MinecraftClient mc = MinecraftClient.getInstance();
        mc.setScreen(new net.minecraft.client.gui.screen.multiplayer.MultiplayerScreen(this));
    }

    private void hudEditor() {
        MinecraftClient mc = MinecraftClient.getInstance();
        mc.setScreen(new HudEditorScreen(this));
    }

    private void perf() {
        MinecraftClient mc = MinecraftClient.getInstance();
        mc.setScreen(new PerfScreen(this));
    }

    private void mods() {
        MinecraftClient mc = MinecraftClient.getInstance();
        try {
            Class<?> cls = Class.forName("com.terraformersmc.modmenu.gui.ModsScreen");
            java.lang.reflect.Constructor<?> c = cls.getConstructor(Screen.class);
            mc.setScreen((Screen) c.newInstance(this));
        } catch (Exception e) {
            mc.setScreen(new InfoScreen(this, "ModMenu ist nicht installiert.",
                    "Installiere „Mod Menu“ über den ASLAN-Launcher (Mods-Tab), um Mods ingame zu verwalten."));
        }
    }

    private void settings() {
        MinecraftClient mc = MinecraftClient.getInstance();
        mc.setScreen(new net.minecraft.client.gui.screen.option.OptionsScreen(this, mc.options));
    }

    @Override
    public void render(DrawContext ctx, int mouseX, int mouseY, float delta) {
        ctx.fillGradient(0, 0, width, height, 0xFF0A0A10, 0xFF1A1440);
        if (animation) {
            ticks++;
            for (int i = 0; i < px.length; i++) {
                py[i] -= 0.0006f * ps[i];
                if (py[i] < 0) py[i] = 1.0f;
                int x = (int) (px[i] * width);
                int y = (int) (py[i] * height);
                int a = (int) (40 + 30 * Math.sin(ticks * 0.02 + i));
                ctx.fill(x, y, x + 2, y + 2, (a << 24) | 0x8F7BFF);
            }
        }
        ctx.drawCenteredTextWithShadow(textRenderer, Text.literal("ASLAN CLIENT"),
                width / 2, height / 2 - 120, 0xA78BFA);
        ctx.drawCenteredTextWithShadow(textRenderer,
                Text.literal("Your Minecraft. Your Performance. Your Rules."),
                width / 2, height / 2 - 104, 0x94A3B8);
        super.render(ctx, mouseX, mouseY, delta);
    }
}

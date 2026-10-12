package gg.aslan.client.hud;

import gg.aslan.client.AslanClient;
import gg.aslan.client.core.HudLayout;
import net.minecraft.client.MinecraftClient;
import net.minecraft.client.gui.DrawContext;
import net.minecraft.client.network.PlayerListEntry;
import net.minecraft.entity.effect.StatusEffectInstance;
import net.minecraft.item.ItemStack;
import net.minecraft.text.Text;
import net.minecraft.util.math.BlockPos;

import java.io.File;
import java.util.ArrayList;
import java.util.Collection;
import java.util.List;

/**
 * HUD-Manager: rendert alle Widgets des aktiven Layouts, verwaltet Profile,
 * Klick-/Combo-Zähler und speichert Positionen pro Profil.
 */
public final class HudManager {
    private HudLayout active;
    private final HudLayout pvp = HudLayout.pvp();
    private final HudLayout survival = HudLayout.survival();
    private final List<Long> clicks = new ArrayList<Long>();
    private int combo;
    private long comboExpires;

    public HudManager(HudLayout initial) {
        this.active = initial;
    }

    public HudLayout active() { return active; }

    public void setProfile(String name) {
        if ("survival".equalsIgnoreCase(name) || "creative".equalsIgnoreCase(name)) active = survival;
        else active = pvp;
        saveToDisk();
    }

    public void registerClick() {
        long now = System.currentTimeMillis();
        clicks.add(now);
        while (!clicks.isEmpty() && now - clicks.get(0) > 1000) clicks.remove(0);
        combo++;
        comboExpires = now + 3000;
    }

    public void tick(MinecraftClient mc) {
        long now = System.currentTimeMillis();
        while (!clicks.isEmpty() && now - clicks.get(0) > 1000) clicks.remove(0);
        if (now > comboExpires) combo = 0;
    }

    public void render(DrawContext ctx, MinecraftClient mc) {
        if (mc.options.hudHidden) return;
        int sw = ctx.getScaledWindowWidth();
        int sh = ctx.getScaledWindowHeight();
        for (HudLayout.Widget w : active.widgets) {
            if (!w.enabled) continue;
            int x = (int) (w.x * sw);
            int y = (int) (w.y * sh);
            int alpha = (int) (255 * clamp(w.opacity, 0.15, 1.0));
            int color = (0xFFFFFF & parseColor(w.color)) | (alpha << 24);
            String id = w.id;
            if ("fps".equals(id)) text(ctx, "FPS: " + mc.getCurrentFps(), x, y, color);
            else if ("ping".equals(id)) text(ctx, "Ping: " + ping(mc) + " ms", x, y, color);
            else if ("coords".equals(id)) text(ctx, coords(mc), x, y, color);
            else if ("biome".equals(id)) text(ctx, biome(mc), x, y, color);
            else if ("clock".equals(id)) text(ctx, clock(), x, y, color);
            else if ("cps".equals(id)) text(ctx, "CPS: " + clicks.size(), x, y, color);
            else if ("combo".equals(id)) { if (combo > 1) text(ctx, "Combo x" + combo, x, y, color); }
            else if ("ram".equals(id)) text(ctx, ram(), x, y, color);
            else if ("keystrokes".equals(id)) renderKeystrokes(ctx, mc, x, y, color);
            else if ("armor".equals(id)) renderArmor(ctx, mc, x, y);
            else if ("potion".equals(id)) renderPotions(ctx, mc, x, y, color);
            else if ("crosshair".equals(id)) renderCrosshair(ctx, sw / 2, sh / 2, color);
            else if ("session".equals(id)) text(ctx, session(), x, y, color);
        }
    }

    private void text(DrawContext ctx, String s, int x, int y, int color) {
        ctx.drawText(MinecraftClient.getInstance().textRenderer, Text.literal(s), x, y, color, true);
    }

    private int ping(MinecraftClient mc) {
        try {
            if (mc.getNetworkHandler() == null) return 0;
            PlayerListEntry e = mc.getNetworkHandler().getPlayerListEntry(mc.player.getUuid());
            return e == null ? 0 : e.getLatency();
        } catch (Exception ex) {
            return 0;
        }
    }

    private String coords(MinecraftClient mc) {
        BlockPos p = mc.player.getBlockPos();
        String dir = mc.player.getHorizontalFacing().asString();
        return "XYZ: " + p.getX() + " / " + p.getY() + " / " + p.getZ() + " (" + dir + ")";
    }

    private String biome(MinecraftClient mc) {
        try {
            return "Biom: " + mc.world.getBiome(mc.player.getBlockPos()).getKey().get().getValue().getPath();
        } catch (Exception ex) {
            return "Biom: —";
        }
    }

    private String clock() {
        java.util.Calendar c = java.util.Calendar.getInstance();
        return String.format("%02d:%02d", c.get(java.util.Calendar.HOUR_OF_DAY), c.get(java.util.Calendar.MINUTE));
    }

    private String ram() {
        Runtime rt = Runtime.getRuntime();
        long used = (rt.totalMemory() - rt.freeMemory()) / 1048576L;
        return "RAM: " + used + " MB";
    }

    private String session() {
        long mins = (System.currentTimeMillis() - AslanClient.SESSION_START) / 60000L;
        return "Session: " + mins + " min";
    }

    private void renderKeystrokes(DrawContext ctx, MinecraftClient mc, int x, int y, int color) {
        boolean w = mc.options.forwardKey.isPressed();
        boolean a = mc.options.leftKey.isPressed();
        boolean s = mc.options.backKey.isPressed();
        boolean d = mc.options.rightKey.isPressed();
        boolean sp = mc.options.jumpKey.isPressed();
        ctx.drawText(mc.textRenderer, Text.literal(key("W", w)), x + 16, y, color, true);
        ctx.drawText(mc.textRenderer, Text.literal(key("A", a)), x, y + 12, color, true);
        ctx.drawText(mc.textRenderer, Text.literal(key("S", s)), x + 16, y + 12, color, true);
        ctx.drawText(mc.textRenderer, Text.literal(key("D", d)), x + 32, y + 12, color, true);
        if (sp) ctx.drawText(mc.textRenderer, Text.literal("SPACE"), x, y + 24, color, true);
    }

    private String key(String k, boolean pressed) {
        return pressed ? "[" + k + "]" : " " + k + " ";
    }

    private void renderArmor(DrawContext ctx, MinecraftClient mc, int x, int y) {
        int dx = x;
        net.minecraft.entity.EquipmentSlot[] slots = {
            net.minecraft.entity.EquipmentSlot.HEAD,
            net.minecraft.entity.EquipmentSlot.CHEST,
            net.minecraft.entity.EquipmentSlot.LEGS,
            net.minecraft.entity.EquipmentSlot.FEET
        };
        for (int i = 0; i < slots.length; i++) {
            ItemStack stack = mc.player.getEquippedStack(slots[i]);
            if (stack == null || stack.isEmpty()) continue;
            ctx.drawItem(stack, dx, y);
            int dmg = stack.getMaxDamage() - stack.getDamage();
            ctx.drawText(mc.textRenderer, Text.literal(String.valueOf(dmg)), dx, y + 18, 0xFFFFFFFF, true);
            dx += 22;
        }
    }

    private void renderPotions(DrawContext ctx, MinecraftClient mc, int x, int y, int color) {
        Collection<StatusEffectInstance> fx = mc.player.getStatusEffects();
        int dy = y;
        for (StatusEffectInstance e : fx) {
            String name;
            try {
                name = Text.translatable(e.getTranslationKey()).getString();
            } catch (Exception ex) {
                name = e.getTranslationKey();
            }
            text(ctx, name + " " + (e.getAmplifier() + 1) + " (" + (e.getDuration() / 20) + "s)", x, dy, color);
            dy += 11;
        }
    }

    private void renderCrosshair(DrawContext ctx, int x, int y, int color) {
        ctx.fill(x - 5, y, x + 6, y + 1, color);
        ctx.fill(x, y - 5, x + 1, y + 6, color);
    }

    private static double clamp(double v, double lo, double hi) {
        return Math.max(lo, Math.min(hi, v));
    }

    private static int parseColor(String hex) {
        try {
            return Integer.parseInt(hex.replace("#", ""), 16);
        } catch (Exception e) {
            return 0xFFFFFF;
        }
    }

    private File file() {
        return new File(MinecraftClient.getInstance().runDirectory, "config/aslan-hud.json");
    }

    public void saveToDisk() {
        try {
            File f = file();
            f.getParentFile().mkdirs();
            StringBuilder sb = new StringBuilder("{\"layout\":\"" + active.name + "\"}");
            java.nio.file.Files.write(f.toPath(), sb.toString().getBytes("UTF-8"));
        } catch (Exception ignored) {
        }
    }

    public void loadFromDisk() {
        try {
            File f = file();
            if (!f.exists()) return;
            String s = new String(java.nio.file.Files.readAllBytes(f.toPath()), "UTF-8");
            if (s.contains("survival")) active = survival;
            else active = pvp;
        } catch (Exception ignored) {
        }
    }
}

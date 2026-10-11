package gg.vyra.client.core;

import java.util.ArrayList;
import java.util.List;

/** HUD-Layout: Widgets mit Position/Skala/Transparenz, Profile je Spielmodus. */
public final class HudLayout {
    public static final class Widget {
        public String id;
        public boolean enabled = true;
        public double x; // 0..1 relativ
        public double y;
        public double scale = 1.0;
        public double opacity = 1.0;
        public String color = "#FFFFFF";

        public Widget(String id, double x, double y) {
            this.id = id;
            this.x = x;
            this.y = y;
        }
    }

    public String name;
    public List<Widget> widgets = new ArrayList<Widget>();

    public HudLayout(String name) {
        this.name = name;
    }

    public void add(String id, double x, double y) {
        widgets.add(new Widget(id, x, y));
    }

    public Widget get(String id) {
        for (Widget w : widgets) {
            if (w.id.equals(id)) return w;
        }
        return null;
    }

    public void snapToGrid(double step) {
        for (Widget w : widgets) {
            w.x = Math.round(w.x / step) * step;
            w.y = Math.round(w.y / step) * step;
        }
    }

    /** Standardlayouts für PvP / Survival / Creative. */
    public static HudLayout pvp() {
        HudLayout l = new HudLayout("pvp");
        l.add("fps", 0.01, 0.01);
        l.add("ping", 0.01, 0.05);
        l.add("cps", 0.01, 0.09);
        l.add("keystrokes", 0.42, 0.72);
        l.add("armor", 0.42, 0.62);
        l.add("potion", 0.93, 0.01);
        l.add("crosshair", 0.5, 0.5);
        l.add("combo", 0.01, 0.13);
        return l;
    }

    public static HudLayout survival() {
        HudLayout l = new HudLayout("survival");
        l.add("fps", 0.01, 0.01);
        l.add("coords", 0.01, 0.05);
        l.add("biome", 0.01, 0.09);
        l.add("clock", 0.93, 0.01);
        l.add("armor", 0.42, 0.62);
        l.add("bossbar", 0.5, 0.03);
        return l;
    }

    public String describe() {
        StringBuilder sb = new StringBuilder(name).append(":");
        for (Widget w : widgets) {
            sb.append(" ").append(w.id).append(w.enabled ? "" : "(off)");
        }
        return sb.toString();
    }
}

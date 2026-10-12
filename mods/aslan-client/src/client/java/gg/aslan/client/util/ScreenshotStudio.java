package gg.aslan.client.util;

import net.minecraft.client.MinecraftClient;
import java.io.File;
import java.text.SimpleDateFormat;
import java.util.Date;

/** Screenshot-Studio: speichert Screenshots in aslan-screenshots/. */
public final class ScreenshotStudio {
    private ScreenshotStudio() {}

    public static File dir() {
        File d = new File(MinecraftClient.getInstance().runDirectory, "aslan-screenshots");
        d.mkdirs();
        return d;
    }

    public static void take() {
        MinecraftClient mc = MinecraftClient.getInstance();
        if (mc == null) return;
        try {
            String name = new SimpleDateFormat("yyyy-MM-dd_HH.mm.ss").format(new Date());
            File out = new File(dir(), "aslan-" + name + ".png");
            net.minecraft.client.util.ScreenshotRecorder.takeScreenshot(mc.getFramebuffer(), img -> {
                try {
                    img.writeTo(out);
                } catch (Exception ignored) {
                }
            });
        } catch (Exception ignored) {
        }
    }
}

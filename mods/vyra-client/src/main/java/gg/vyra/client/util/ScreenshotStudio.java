package gg.vyra.client.util;

import net.minecraft.client.MinecraftClient;
import java.io.File;
import java.text.SimpleDateFormat;
import java.util.Date;

/** Screenshot-Studio: speichert Screenshots in vyra-screenshots/. */
public final class ScreenshotStudio {
    private ScreenshotStudio() {}

    public static File dir() {
        File d = new File(MinecraftClient.getInstance().runDirectory, "vyra-screenshots");
        d.mkdirs();
        return d;
    }

    public static void take() {
        MinecraftClient mc = MinecraftClient.getInstance();
        if (mc == null) return;
        try {
            String name = new SimpleDateFormat("yyyy-MM-dd_HH.mm.ss").format(new Date());
            net.minecraft.client.util.ScreenshotRecorder.saveScreenshot(
                    dir(), name, mc.getFramebuffer(), text -> {});
        } catch (Exception ignored) {
        }
    }
}

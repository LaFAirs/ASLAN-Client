package gg.aslan.client.mixin;

import gg.aslan.client.menu.AslanMainMenu;
import net.minecraft.client.MinecraftClient;
import net.minecraft.client.gui.screen.Screen;
import net.minecraft.client.gui.screen.TitleScreen;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

/**
 * Ersetzt den Vanilla-Titelbildschirm durch das ASLAN-Hauptmenü.
 * Einmalig pro Sitzung (Flag), damit Untermenüs normal funktionieren.
 */
@Mixin(TitleScreen.class)
public abstract class TitleScreenMixin extends Screen {
    protected TitleScreenMixin(net.minecraft.text.Text title) {
        super(title);
    }

    private static boolean aslanShown = false;

    @Inject(method = "init", at = @At("HEAD"))
    private void aslan$replaceTitle(CallbackInfo ci) {
        if (aslanShown) return;
        aslanShown = true;
        MinecraftClient mc = MinecraftClient.getInstance();
        if (mc != null) mc.setScreen(new AslanMainMenu());
    }
}

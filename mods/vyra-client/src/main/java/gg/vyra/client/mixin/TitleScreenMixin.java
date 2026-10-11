package gg.vyra.client.mixin;

import gg.vyra.client.menu.VyraMainMenu;
import net.minecraft.client.MinecraftClient;
import net.minecraft.client.gui.screen.Screen;
import net.minecraft.client.gui.screen.TitleScreen;
import org.spongepowered.asm.mixin.Mixin;
import org.spongepowered.asm.mixin.injection.At;
import org.spongepowered.asm.mixin.injection.Inject;
import org.spongepowered.asm.mixin.injection.callback.CallbackInfo;

/**
 * Ersetzt den Vanilla-Titelbildschirm durch das VYRA-Hauptmenü.
 * Einmalig pro Sitzung (Flag), damit Untermenüs normal funktionieren.
 */
@Mixin(TitleScreen.class)
public abstract class TitleScreenMixin extends Screen {
    protected TitleScreenMixin(net.minecraft.text.Text title) {
        super(title);
    }

    private static boolean vyraShown = false;

    @Inject(method = "init", at = @At("HEAD"))
    private void vyra$replaceTitle(CallbackInfo ci) {
        if (vyraShown) return;
        vyraShown = true;
        MinecraftClient mc = MinecraftClient.getInstance();
        if (mc != null) mc.setScreen(new VyraMainMenu());
    }
}

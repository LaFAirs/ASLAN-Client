package gg.aslan.client.core;

/** Spielerprofil: Version, Loader, RAM, Performance, Startoptionen. */
public final class AslanProfile {
    public String name = "Standard";
    public String minecraftVersion = "26.2";
    public String loader = "fabric";
    public String loaderVersion = "0.19.5";
    public int ramMB = 3072;
    public String perfProfile = "BALANCED";
    public String javaArgs = "";
    public boolean safeMode = false;

    public AslanProfile() {}

    public AslanProfile(String name) {
        this.name = name;
    }
}

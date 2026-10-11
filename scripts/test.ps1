# VYRA Tests 0.2.0 - ASCII only, keine Zusatztools.
$ErrorActionPreference = "Continue"
$root = Split-Path $PSScriptRoot -Parent
$fail = 0
function Ok($name) { Write-Host ("PASS " + $name) }
function Bad($name) { Write-Host ("FAIL " + $name); $script:fail++ }

Write-Host "== VYRA TESTS 0.2.0 =="

# --- 1) Launcher-Kernlogik: echte C#-Quellen, in-memory kompiliert (ohne Setup) ---
try {
    $csDir = Join-Path $root "apps\launcher"
    $files = Get-ChildItem $csDir -Filter "*.cs" | Where-Object { $_.Name -ne "VyraSetup.cs" } | ForEach-Object { $_.FullName }
    $usings = @()
    $asmAttrs = @()
    $bodies = @()
    foreach ($f in $files) {
        $lines = [IO.File]::ReadAllLines($f)
        foreach ($ln in $lines) {
            $t = $ln.Trim()
            if ($t.StartsWith("[assembly:")) {
                if ($asmAttrs -notcontains $t) { $asmAttrs += $t }
            } elseif ($t.StartsWith("using ") -and $t.EndsWith(";") -and ($t.Contains("=") -eq $false)) {
                if ($usings -notcontains $t) { $usings += $t }
            } else {
                $bodies += $ln
            }
        }
    }
    $src = ($usings -join "`n") + "`n" + ($asmAttrs -join "`n") + "`n" + ($bodies -join "`n")
    Add-Type -TypeDefinition $src -ReferencedAssemblies @("System.dll","System.Core.dll","System.Drawing.dll","System.Windows.Forms.dll","System.Management.dll","System.Security.dll","System.IO.Compression.dll","System.IO.Compression.FileSystem.dll") -ErrorAction Stop
    $t = $null
    foreach ($a in [AppDomain]::CurrentDomain.GetAssemblies()) {
        try { $c = $a.GetType("Vyra.SelfTest"); if ($c -ne $null) { $t = $c; break } } catch { }
    }
    if ($t -eq $null) { Bad "launcher-selftest-type" } else {
        $rc = $t.GetMethod("Run").Invoke($null, @())
        if ($rc -eq 0) { Ok "launcher-selftest" } else { Bad "launcher-selftest rc=$rc" }
    }
} catch {
    Bad ("launcher-compile: " + $_.Exception.Message)
}

# --- 1b) Setup-Quelle kompiliert separat ---
try {
    $f = Join-Path $root "apps\launcher\VyraSetup.cs"
    $lines = [IO.File]::ReadAllLines($f)
    $us = @(); $bo = @()
    foreach ($ln in $lines) {
        $t2 = $ln.Trim()
        if ($t2.StartsWith("using ") -and $t2.EndsWith(";") -and ($t2.Contains("=") -eq $false)) {
            if ($us -notcontains $t2) { $us += $t2 }
        } else { $bo += $ln }
    }
    Add-Type -TypeDefinition (($us -join "`n") + "`n" + ($bo -join "`n")) -ReferencedAssemblies @("System.dll","System.Drawing.dll","System.Windows.Forms.dll") -ErrorAction Stop
    Ok "setup-compile"
} catch { Bad ("setup-compile: " + $_.Exception.Message) }

# --- 2) Mod-Kernlogik per java ---
try {
    $javaBin = Join-Path $env:APPDATA "PrismLauncher\java\java-runtime-epsilon\bin"
    $java = Join-Path $javaBin "java.exe"
    $classes = Join-Path $root "mods\vyra-client\build\classes"
    if ((Test-Path $java) -and (Test-Path $classes)) {
        $o = & $java -cp $classes gg.vyra.client.core.CoreSelfTest 2>&1 | Out-String
        Write-Host $o
        if ($o -match "CORE-TEST OK") { Ok "mod-core-java" } else { Bad "mod-core-java" }
    } else { Bad "mod-core-java (java oder classes fehlt)" }
} catch { Bad ("mod-core-java: " + $_.Exception.Message) }

# --- 3) Mod-JAR enthaelt Klassen + fabric.mod.json ---
try {
    Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction Stop
    $jar = Join-Path $root "mods\vyra-client\build\vyra-client-0.2.0.jar"
    if (Test-Path $jar) {
        $zip = [IO.Compression.ZipFile]::OpenRead($jar)
        $names = @(); foreach ($e in $zip.Entries) { $names += $e.FullName }
        $zip.Dispose()
        if (($names -like "gg/vyra/client/core/*.class").Count -ge 5) { Ok "jar-klassen" } else { Bad "jar-klassen" }
        if ($names -contains "fabric.mod.json") { Ok "jar-fabric.mod.json" } else { Bad "jar-fabric.mod.json" }
    } else { Bad "jar fehlt" }
} catch { Bad ("jar-check: " + $_.Exception.Message) }

# --- 4) Website statisch ---
try {
    $site = Join-Path $root "apps\website\site"
    $need = @("index.html","styles.css","app.js","downloads.html","docs.html","changelog.html","support.html")
    $allOk = $true
    foreach ($n in $need) { if (!(Test-Path (Join-Path $site $n))) { $allOk = $false; Write-Host ("fehlt: " + $n) } }
    if ($allOk) { Ok "website-dateien" } else { Bad "website-dateien" }
    $idx = [IO.File]::ReadAllText((Join-Path $site "index.html"))
    if ($idx.Contains("VYRA CLIENT") -and $idx.Contains("Your Minecraft. Your Performance. Your Rules.")) { Ok "website-hero" } else { Bad "website-hero" }
    $rel = Join-Path $root "releases\SHA256SUMS.txt"
    if (Test-Path $rel) {
        $sha = [IO.File]::ReadAllText($rel)
        Get-ChildItem (Join-Path $root "releases") -File | Where-Object { $_.Name -ne "SHA256SUMS.txt" } | ForEach-Object {
            if ($sha.Contains($_.Name)) { } else { $script:fail++; Write-Host ("FAIL sha-eintrag " + $_.Name) }
        }
        Ok "website-sha-konsistenz"
    } else { Bad "sha-datei" }
} catch { Bad ("website: " + $_.Exception.Message) }

# --- 5) Next.js-Quellen vorhanden ---
try {
    $nx = Join-Path $root "apps\website\next-src"
    if ((Test-Path (Join-Path $nx "package.json")) -and (Test-Path (Join-Path $nx "app\page.tsx")) -and (Test-Path (Join-Path $nx "app\downloads\page.tsx"))) { Ok "next-quellen" } else { Bad "next-quellen" }
} catch { Bad "next-quellen" }

# --- 6) Fabric-Quellen ---
try {
    $mj = Join-Path $root "mods\vyra-client\src\main\java\gg\vyra\client"
    $needJ = @("VyraClient.java","hud\HudManager.java","menu\VyraMainMenu.java","menu\HudEditorScreen.java","performance\ClientPerformance.java","util\BenchmarkRecorder.java","util\QuickSettings.java","util\SafeMode.java","util\ScreenshotStudio.java","util\ProfileSync.java","util\UpdateManager.java","util\SettingsSearch.java","util\DiscordPresence.java","mixin\TitleScreenMixin.java")
    $allOk = $true
    foreach ($n in $needJ) { if (!(Test-Path (Join-Path $mj $n))) { $allOk = $false; Write-Host ("fehlt: " + $n) } }
    if ($allOk) { Ok "fabric-quellen (14)" } else { Bad "fabric-quellen" }
    $fm = [IO.File]::ReadAllText((Join-Path $root "mods\vyra-client\src\main\resources\fabric.mod.json"))
    if ($fm.Contains("1.21.11") -and $fm.Contains("0.19.5")) { Ok "fabric-meta-versionen" } else { Bad "fabric-meta-versionen" }
    $hm = [IO.File]::ReadAllText((Join-Path $mj "hud\HudManager.java"))
    if ($hm.Contains("getSessionStartTime")) { Bad "fabric-kein-vanilla-api-leak" } else { Ok "fabric-kein-vanilla-api-leak" }
} catch { Bad ("fabric: " + $_.Exception.Message) }

# --- 7) Installer-Artefakt + CI-Workflows + Repo-Dateien ---
try {
    if (Test-Path (Join-Path $root "releases\VYRA-Client-Setup-0.2.0.exe")) { Ok "setup-artefakt" } else { Bad "setup-artefakt" }
    $wf = Join-Path $root ".github\workflows"
    if ((Test-Path (Join-Path $wf "build.yml")) -and (Test-Path (Join-Path $wf "pages.yml")) -and (Test-Path (Join-Path $wf "release.yml"))) { Ok "ci-workflows" } else { Bad "ci-workflows" }
    foreach ($rf in @(".gitignore","LICENSE","CONTRIBUTING.md","SECURITY.md","CHANGELOG.md")) {
        if (Test-Path (Join-Path $root $rf)) { } else { Bad ("repo-datei " + $rf) }
    }
    Ok "repo-dateien"
} catch { Bad ("release-check: " + $_.Exception.Message) }

Write-Host ("== TESTS FERTIG, Fehler: " + $fail + " ==")
"VYRA test run $(Get-Date -Format s) failures=$fail" | Set-Content (Join-Path $root "tests\last-run.txt") -Encoding ASCII
exit $fail

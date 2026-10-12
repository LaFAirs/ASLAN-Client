# ASLAN Tests 0.3.0 - ASCII only, keine Zusatztools.
$ErrorActionPreference = "Continue"
$root = Split-Path $PSScriptRoot -Parent
$fail = 0
function Ok($name) { Write-Host ("PASS " + $name) }
function Bad($name) { Write-Host ("FAIL " + $name); $script:fail++ }

Write-Host "== ASLAN TESTS 0.3.0 =="

# --- 1) Launcher: echte C#-Quellen, in-memory kompiliert (ohne Setup) ---
try {
    $csDir = Join-Path $root "apps\launcher"
    $files = Get-ChildItem $csDir -Filter "*.cs" | Where-Object { $_.Name -ne "AslanSetup.cs" } | ForEach-Object { $_.FullName }
    $us = @(); $asm = @(); $bo = @()
    foreach ($f in $files) {
        foreach ($ln in [IO.File]::ReadAllLines($f)) {
            $t = $ln.Trim()
            if ($t.StartsWith("[assembly:")) { if ($asm -notcontains $t) { $asm += $t } }
            elseif ($t.StartsWith("using ") -and $t.EndsWith(";") -and ($t.Contains("=") -eq $false)) { if ($us -notcontains $t) { $us += $t } }
            else { $bo += $ln }
        }
    }
    Add-Type -TypeDefinition (($us -join "`n") + "`n" + ($asm -join "`n") + "`n" + ($bo -join "`n")) -ReferencedAssemblies @("System.dll","System.Core.dll","System.Drawing.dll","System.Windows.Forms.dll","System.Management.dll","System.Security.dll","System.IO.Compression.dll","System.IO.Compression.FileSystem.dll","System.Xml.dll") -ErrorAction Stop
    $t = $null
    foreach ($a in [AppDomain]::CurrentDomain.GetAssemblies()) {
        try { $c = $a.GetType("Aslan.SelfTest"); if ($c -ne $null) { $t = $c; break } } catch { }
    }
    if ($t -eq $null) { Bad "launcher-selftest-type" } else {
        $rc = $t.GetMethod("Run").Invoke($null, @())
        if ($rc -eq 0) { Ok "launcher-selftest" } else { Bad "launcher-selftest rc=$rc" }
    }
} catch {
    Bad ("launcher-compile: " + $_.Exception.Message)
}

# --- 1b) Setup-Quelle separat ---
try {
    $f = Join-Path $root "apps\launcher\AslanSetup.cs"
    $lines = [IO.File]::ReadAllLines($f)
    $us2 = @(); $bo2 = @()
    foreach ($ln in $lines) {
        $t2 = $ln.Trim()
        if ($t2.StartsWith("[assembly:")) { }
        elseif ($t2.StartsWith("using ") -and $t2.EndsWith(";") -and ($t2.Contains("=") -eq $false)) {
            if ($us2 -notcontains $t2) { $us2 += $t2 }
        } else { $bo2 += $ln }
    }
    Add-Type -TypeDefinition (($us2 -join "`n") + "`n" + ($bo2 -join "`n")) -ReferencedAssemblies @("System.dll","System.Drawing.dll","System.Windows.Forms.dll") -ErrorAction Stop
    Ok "setup-compile"
} catch { Bad ("setup-compile: " + $_.Exception.Message) }

# --- 2) Mod-Kernlogik per java ---
try {
    $javaBin = Join-Path $env:APPDATA "PrismLauncher\java\java-runtime-epsilon\bin"
    $java = Join-Path $javaBin "java.exe"
    $classes = $null
    foreach ($cand in @((Join-Path $root "mods\aslan-client\build\classes"), (Join-Path $root "mods\vyra-client\build\classes"))) {
        if (Test-Path $cand) { $classes = $cand }
    }
    if ((Test-Path $java) -and ($classes -ne $null)) {
        $core = "gg.aslan.client.core.CoreSelfTest"
        if (!(Test-Path (Join-Path $classes "gg\aslan"))) { $core = "gg.vyra.client.core.CoreSelfTest" }
        $o = & $java -cp $classes $core 2>&1 | Out-String
        Write-Host $o
        if ($o -match "CORE-TEST OK") { Ok "mod-core-java" } else { Bad "mod-core-java" }
    } else { Bad "mod-core-java (java oder classes fehlt)" }
} catch { Bad ("mod-core-java: " + $_.Exception.Message) }

# --- 3) Mod-JAR ---
try {
    Add-Type -AssemblyName System.IO.Compression.FileSystem -ErrorAction Stop
    $jar = Get-ChildItem (Join-Path $root "mods") -Recurse -Filter "aslan-client-0.3.0.jar" -ErrorAction SilentlyContinue | Where-Object { $_.FullName -match "libs" } | Select-Object -First 1
    if ($jar -eq $null) { $jar = Get-ChildItem (Join-Path $root "mods") -Recurse -Filter "vyra-client-0.2.0.jar" -ErrorAction SilentlyContinue | Where-Object { $_.FullName -match "libs" } | Select-Object -First 1 }
    if ($jar -ne $null) {
        $zip = [IO.Compression.ZipFile]::OpenRead($jar.FullName)
        $names = @(); foreach ($e in $zip.Entries) { $names += $e.FullName }
        $zip.Dispose()
        if (($names -like "*VyraClient.class*").Count + ($names -like "*AslanClient.class*").Count -ge 1) { Ok "jar-clientklasse" } else { Bad "jar-clientklasse" }
        if ($names -contains "fabric.mod.json") { Ok "jar-fabric.mod.json" } else { Bad "jar-fabric.mod.json" }
    } else { Bad "loom-jar fehlt (noch nicht gebaut?)" }
} catch { Bad ("jar-check: " + $_.Exception.Message) }

# --- 4) Website ---
try {
    $site = Join-Path $root "apps\website\site"
    $need = @("index.html","styles.css","app.js","downloads.html","docs.html","changelog.html","support.html")
    $allOk = $true
    foreach ($n in $need) { if (!(Test-Path (Join-Path $site $n))) { $allOk = $false; Write-Host ("fehlt: " + $n) } }
    if ($allOk) { Ok "website-dateien" } else { Bad "website-dateien" }
    $idx = [IO.File]::ReadAllText((Join-Path $site "index.html"))
    if ($idx.Contains("ASLAN CLIENT") -and $idx.Contains("PLAY BEYOND LIMITS")) { Ok "website-branding" } else { Bad "website-branding" }
    $css = [IO.File]::ReadAllText((Join-Path $site "styles.css"))
    if ($css.Contains("#FFBF47") -or $css.Contains("255,191,71") -or $css.Contains("FFBF47")) { Ok "website-gold" } else { Bad "website-gold" }
    $rel = Join-Path $root "releases\SHA256SUMS.txt"
    if (Test-Path $rel) {
        $sha = [IO.File]::ReadAllText($rel)
        Get-ChildItem (Join-Path $root "releases") -File | Where-Object { $_.Name -ne "SHA256SUMS.txt" } | ForEach-Object {
            if ($sha.Contains($_.Name)) { } else { $script:fail++; Write-Host ("FAIL sha-eintrag " + $_.Name) }
        }
        Ok "sha-konsistenz"
    } else { Bad "sha-datei" }
} catch { Bad ("website: " + $_.Exception.Message) }

# --- 5) Next.js-Quellen ---
try {
    $nx = Join-Path $root "apps\website\next-src"
    if ((Test-Path (Join-Path $nx "package.json")) -and (Test-Path (Join-Path $nx "app\page.tsx"))) { Ok "next-quellen" } else { Bad "next-quellen" }
} catch { Bad "next-quellen" }

# --- 6) Mod-Quellen (aslan-Paket) ---
try {
    $mj = Join-Path $root "mods\aslan-client\src\client\java\gg\aslan\client"
    if (!(Test-Path $mj)) { $mj = Join-Path $root "mods\vyra-client\src\client\java\gg\vyra\client" }
    $needJ = @("AslanClient.java","hud\HudManager.java","menu\VyraMainMenu.java","menu\HudEditorScreen.java","performance\ClientPerformance.java","util\BenchmarkRecorder.java","util\QuickSettings.java","util\SafeMode.java","util\ScreenshotStudio.java","util\ProfileSync.java","util\UpdateManager.java","util\SettingsSearch.java","util\DiscordPresence.java","mixin\TitleScreenMixin.java")
    $needJ2 = @("VyraClient.java","hud\HudManager.java","menu\VyraMainMenu.java","menu\HudEditorScreen.java","performance\ClientPerformance.java","util\BenchmarkRecorder.java","util\QuickSettings.java","util\SafeMode.java","util\ScreenshotStudio.java","util\ProfileSync.java","util\UpdateManager.java","util\SettingsSearch.java","util\DiscordPresence.java","mixin\TitleScreenMixin.java")
    $allOk = $true
    foreach ($n in $needJ) { if (!(Test-Path (Join-Path $mj $n))) { $allOk = $false; Write-Host ("fehlt: " + $n) } }
    if ($allOk) { Ok "mod-quellen (14)" }
    else {
        $allOk2 = $true
        foreach ($n in $needJ2) { if (!(Test-Path (Join-Path $mj $n))) { $allOk2 = $false } }
        if ($allOk2) { Ok "mod-quellen (14, vyra-namen)" } else { Bad "mod-quellen" }
    }
} catch { Bad ("mod-quellen: " + $_.Exception.Message) }

# --- 7) Artefakte + CI + Repo ---
try {
    $setup = Get-ChildItem (Join-Path $root "releases") -Filter "ASLAN-Client-Setup-0.3.0.exe" -ErrorAction SilentlyContinue
    if ($setup -ne $null) { Ok "setup-artefakt" } else { Bad "setup-artefakt" }
    $wf = Join-Path $root ".github\workflows"
    if ((Test-Path (Join-Path $wf "build.yml")) -and (Test-Path (Join-Path $wf "pages.yml")) -and (Test-Path (Join-Path $wf "release.yml"))) { Ok "ci-workflows" } else { Bad "ci-workflows" }
    foreach ($rf in @(".gitignore","LICENSE","CONTRIBUTING.md","SECURITY.md","CHANGELOG.md")) {
        if (Test-Path (Join-Path $root $rf)) { } else { Bad ("repo-datei " + $rf) }
    }
    Ok "repo-dateien"
} catch { Bad ("release-check: " + $_.Exception.Message) }

Write-Host ("== TESTS FERTIG, Fehler: " + $fail + " ==")
"ASLAN test run $(Get-Date -Format s) failures=$fail" | Set-Content (Join-Path $root "tests\last-run.txt") -Encoding ASCII
exit $fail

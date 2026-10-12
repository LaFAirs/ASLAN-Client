# ASLAN Build 0.3.0 - nur Windows-Bordmittel (csc, javac, jar) + PowerShell.
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$launcherDir = Join-Path $root "apps\launcher"
$outDir = $launcherDir
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

Write-Host "== ASLAN Build 0.3.0 =="

# --- 1) Launcher kompilieren (ohne Setup) ---
if (!(Test-Path $csc)) { throw "csc.exe nicht gefunden." }
$csFiles = Get-ChildItem $launcherDir -Filter "*.cs" | Where-Object { $_.Name -ne "AslanSetup.cs" } | ForEach-Object { $_.FullName }
Write-Host ("Kompiliere {0} C#-Dateien ..." -f $csFiles.Count)
$refs = @("System.dll", "System.Core.dll", "System.Drawing.dll", "System.Windows.Forms.dll", "System.Management.dll", "System.Security.dll", "System.IO.Compression.dll", "System.IO.Compression.FileSystem.dll", "System.Xml.dll")
$refArgs = $refs | ForEach-Object { "/reference:$_" }
& $csc "/target:winexe" ("/out:" + (Join-Path $outDir "AslanLauncher.exe")) $refArgs $csFiles
if ($LASTEXITCODE -ne 0) { throw "Launcher-Kompilierung fehlgeschlagen." }
Write-Host "OK: AslanLauncher.exe"

# --- 2) Installer kompilieren ---
& $csc "/target:winexe" ("/out:" + (Join-Path $outDir "ASLAN-Client-Setup-0.3.0.exe")) $refArgs (Join-Path $launcherDir "AslanSetup.cs")
if ($LASTEXITCODE -ne 0) { throw "Setup-Kompilierung fehlgeschlagen." }
Write-Host "OK: ASLAN-Client-Setup-0.3.0.exe"

# --- 3) Mod-Kernlogik kompilieren + JAR bauen (JDK flexibel: lokal PrismLauncher, CI JAVA_HOME/PATH) ---
$javac = $null
$jarTool = $null
$phHome = Join-Path $env:APPDATA "PrismLauncher\java\java-runtime-epsilon\bin"
if (Test-Path (Join-Path $phHome "javac.exe")) { $javac = Join-Path $phHome "javac.exe"; $jarTool = Join-Path $phHome "jar.exe" }
elseif ($env:JAVA_HOME -ne $null -and (Test-Path (Join-Path $env:JAVA_HOME "bin\javac.exe"))) { $javac = Join-Path $env:JAVA_HOME "bin\javac.exe"; $jarTool = Join-Path $env:JAVA_HOME "bin\jar.exe" }
else {
    try {
        $wj = (& where.exe javac 2>$null | Select-Object -First 1)
        if ($wj -ne $null -and (Test-Path $wj)) { $javac = $wj; $jarTool = Join-Path (Split-Path $wj) "jar.exe" }
        $wr = (& where.exe jar 2>$null | Select-Object -First 1)
        if ($wr -ne $null -and (Test-Path $wr)) { $jarTool = $wr }
    } catch { }
}
$modSrc = Join-Path $root "mods\vyra-client\src\main\java"
if (!(Test-Path $modSrc)) { $modSrc = Join-Path $root "mods\aslan-client\src\main\java" }
$modBuild = Join-Path $root "mods\vyra-client\build"
if (!(Test-Path (Join-Path $root "mods\vyra-client"))) { $modBuild = Join-Path $root "mods\aslan-client\build" }
$classes = Join-Path $modBuild "classes"
New-Item -ItemType Directory -Force -Path $classes | Out-Null
$coreDir = Join-Path $modSrc "gg\vyra\client\core"
if (!(Test-Path $coreDir)) { $coreDir = Join-Path $modSrc "gg\aslan\client\core" }
$coreFiles = Get-ChildItem $coreDir -Filter "*.java" -ErrorAction SilentlyContinue | ForEach-Object { $_.FullName }
if ($coreFiles -and (Test-Path $javac)) {
    Write-Host ("Kompiliere {0} Java-Kerndateien ..." -f $coreFiles.Count)
    & $javac -encoding UTF-8 -d $classes $coreFiles
    if ($LASTEXITCODE -ne 0) { throw "Java-Kompilierung fehlgeschlagen." }
    $resDir = Join-Path $modSrc "..\resources"
    if (Test-Path (Join-Path $resDir "fabric.mod.json")) {
        Copy-Item (Join-Path $resDir "fabric.mod.json") (Join-Path $classes "fabric.mod.json") -Force
    }
    $jarOut = Join-Path $modBuild "aslan-client-0.3.0.jar"
    Push-Location $classes
    & $jarTool cf $jarOut .
    Pop-Location
    if ($LASTEXITCODE -ne 0) { throw "JAR-Erstellung fehlgeschlagen." }
    Write-Host "OK: aslan-client-0.3.0.jar"
    Copy-Item $jarOut (Join-Path $outDir "aslan-client.jar") -Force
    Write-Host "OK: aslan-client.jar neben Launcher gelegt."
} else {
    Write-Host "WARNUNG: keine Java-Kerndateien oder kein javac - JAR uebersprungen."
}

# --- 4) Releases + SHA256 ---
$relDir = Join-Path $root "releases"
New-Item -ItemType Directory -Force -Path $relDir | Out-Null
Copy-Item (Join-Path $outDir "AslanLauncher.exe") (Join-Path $relDir "AslanLauncher-0.3.0.exe") -Force
Copy-Item (Join-Path $outDir "ASLAN-Client-Setup-0.3.0.exe") (Join-Path $relDir "ASLAN-Client-Setup-0.3.0.exe") -Force
if (Test-Path (Join-Path $modBuild "aslan-client-0.3.0.jar")) {
    Copy-Item (Join-Path $modBuild "aslan-client-0.3.0.jar") (Join-Path $relDir "aslan-client-0.3.0.jar") -Force
}
$loomJar = Get-ChildItem (Join-Path $root "mods") -Recurse -Filter "aslan-client-0.3.0.jar" -ErrorAction SilentlyContinue | Where-Object { $_.FullName -match "libs" } | Select-Object -First 1
if ($loomJar -ne $null) {
    Copy-Item $loomJar.FullName (Join-Path $relDir "aslan-client-0.3.0.jar") -Force
    Copy-Item $loomJar.FullName (Join-Path $outDir "aslan-client.jar") -Force
    Write-Host "OK: Loom-Mod als Release-Artefakt uebernommen."
}
$shaFile = Join-Path $relDir "SHA256SUMS.txt"
Get-ChildItem $relDir -File | Where-Object { $_.Name -ne "SHA256SUMS.txt" } | ForEach-Object {
    $h = (Get-FileHash $_.FullName -Algorithm SHA256).Hash
    "{0}  {1}" -f $h.ToLower(), $_.Name
} | Set-Content $shaFile -Encoding ASCII
Write-Host "OK: SHA256SUMS.txt"
Write-Host "== BUILD FERTIG =="

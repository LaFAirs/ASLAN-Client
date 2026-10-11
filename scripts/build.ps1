# VYRA Build 0.2.0 - nur Windows-Bordmittel (csc, javac, jar) + PowerShell.
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$launcherDir = Join-Path $root "apps\launcher"
$outDir = $launcherDir
$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

Write-Host "== VYRA Build 0.2.0 =="

# --- 1) Launcher kompilieren (ohne Setup) ---
if (!(Test-Path $csc)) { throw "csc.exe nicht gefunden." }
$csFiles = Get-ChildItem $launcherDir -Filter "*.cs" | Where-Object { $_.Name -ne "VyraSetup.cs" } | ForEach-Object { $_.FullName }
Write-Host ("Kompiliere {0} C#-Dateien ..." -f $csFiles.Count)
$refs = @("System.dll", "System.Core.dll", "System.Drawing.dll", "System.Windows.Forms.dll", "System.Management.dll", "System.Security.dll", "System.IO.Compression.dll", "System.IO.Compression.FileSystem.dll", "System.Xml.dll")
$refArgs = $refs | ForEach-Object { "/reference:$_" }
& $csc "/target:winexe" ("/out:" + (Join-Path $outDir "VyraLauncher.exe")) $refArgs $csFiles
if ($LASTEXITCODE -ne 0) { throw "Launcher-Kompilierung fehlgeschlagen." }
Write-Host "OK: VyraLauncher.exe"

# --- 2) Installer kompilieren ---
& $csc "/target:winexe" ("/out:" + (Join-Path $outDir "VYRA-Client-Setup-0.2.0.exe")) $refArgs (Join-Path $launcherDir "VyraSetup.cs")
if ($LASTEXITCODE -ne 0) { throw "Setup-Kompilierung fehlgeschlagen." }
Write-Host "OK: VYRA-Client-Setup-0.2.0.exe"

# --- 3) Mod-Kernlogik kompilieren + JAR bauen ---
$javaHome = Join-Path $env:APPDATA "PrismLauncher\java\java-runtime-epsilon\bin"
$javac = Join-Path $javaHome "javac.exe"
$jarTool = Join-Path $javaHome "jar.exe"
$modSrc = Join-Path $root "mods\vyra-client\src\main\java"
$modBuild = Join-Path $root "mods\vyra-client\build"
$classes = Join-Path $modBuild "classes"
New-Item -ItemType Directory -Force -Path $classes | Out-Null
$coreDir = Join-Path $modSrc "gg\vyra\client\core"
$coreFiles = Get-ChildItem $coreDir -Filter "*.java" -ErrorAction SilentlyContinue | ForEach-Object { $_.FullName }
if ($coreFiles -and (Test-Path $javac)) {
    Write-Host ("Kompiliere {0} Java-Kerndateien ..." -f $coreFiles.Count)
    & $javac -encoding UTF-8 -d $classes $coreFiles
    if ($LASTEXITCODE -ne 0) { throw "Java-Kompilierung fehlgeschlagen." }
    Copy-Item (Join-Path $root "mods\vyra-client\src\main\resources\fabric.mod.json") (Join-Path $classes "fabric.mod.json") -Force
    $jarOut = Join-Path $modBuild "vyra-client-0.2.0.jar"
    Push-Location $classes
    & $jarTool cf $jarOut .
    Pop-Location
    if ($LASTEXITCODE -ne 0) { throw "JAR-Erstellung fehlgeschlagen." }
    Write-Host "OK: vyra-client-0.2.0.jar"
    Copy-Item $jarOut (Join-Path $outDir "vyra-client.jar") -Force
    Write-Host "OK: vyra-client.jar neben Launcher gelegt."
} else {
    Write-Host "WARNUNG: keine Java-Kerndateien oder kein javac - JAR uebersprungen."
}

# --- 4) Releases + SHA256 ---
$relDir = Join-Path $root "releases"
New-Item -ItemType Directory -Force -Path $relDir | Out-Null
Copy-Item (Join-Path $outDir "VyraLauncher.exe") (Join-Path $relDir "VyraLauncher-0.2.0.exe") -Force
Copy-Item (Join-Path $outDir "VYRA-Client-Setup-0.2.0.exe") (Join-Path $relDir "VYRA-Client-Setup-0.2.0.exe") -Force
if (Test-Path (Join-Path $modBuild "vyra-client-0.2.0.jar")) {
    Copy-Item (Join-Path $modBuild "vyra-client-0.2.0.jar") (Join-Path $relDir "vyra-client-0.2.0.jar") -Force
}
# Echter Loom-Mod (falls gebaut) ersetzt Kern-JAR als Release-Artefakt + Instanz-Mod.
$loomJar = Join-Path $root "mods\vyra-client\build\libs\vyra-client-0.2.0.jar"
if (Test-Path $loomJar) {
    Copy-Item $loomJar (Join-Path $relDir "vyra-client-0.2.0.jar") -Force
    Copy-Item $loomJar (Join-Path $outDir "vyra-client.jar") -Force
    Write-Host "OK: Loom-Mod als Release-Artefakt uebernommen."
}
$shaFile = Join-Path $relDir "SHA256SUMS.txt"
Get-ChildItem $relDir -File | Where-Object { $_.Name -ne "SHA256SUMS.txt" } | ForEach-Object {
    $h = (Get-FileHash $_.FullName -Algorithm SHA256).Hash
    "{0}  {1}" -f $h.ToLower(), $_.Name
} | Set-Content $shaFile -Encoding ASCII
Write-Host "OK: SHA256SUMS.txt"
Write-Host "== BUILD FERTIG =="

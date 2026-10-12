# ASLAN Integrationstest: echte 1.21.11-Fabric-Instanz installieren + offline starten + ASLAN-Marker pruefen.
# ASCII only. Dauert mehrere Minuten (Downloads). Oeffnet kurz ein Minecraft-Fenster.
$ErrorActionPreference = "Continue"
$root = Split-Path $PSScriptRoot -Parent
$fail = 0
function Ok($n) { Write-Host ("PASS " + $n) }
function Bad($n) { Write-Host ("FAIL " + $n); $script:fail++ }

Write-Host "== ASLAN INTEGRATIONSTEST =="

# --- 1) Launcher-Quellen in-memory kompilieren ---
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
    Add-Type -TypeDefinition (($us -join "`n") + "`n" + ($asm -join "`n") + "`n" + ($bo -join "`n")) -ReferencedAssemblies @("System.dll","System.Core.dll","System.Drawing.dll","System.Windows.Forms.dll","System.Management.dll","System.Security.dll","System.IO.Compression.dll","System.IO.Compression.FileSystem.dll") -ErrorAction Stop
    Ok "quellen-kompiliert"
} catch { Bad ("quellen: " + $_.Exception.Message); exit 1 }

$ct = New-Object Threading.CancellationToken
$pr = New-Object Aslan.InstallProgress
$g = New-Object Aslan.GameProfile
$g.Name = "Integrationstest"
$g.MinecraftVersion = "1.21.11"
$g.Loader = "fabric"
$g.LoaderVersion = "0.19.5"
$g.RamMB = 3072

# --- 2) Instanz installieren ---
try {
    Write-Host "Installiere Instanz (Client, Libraries, Natives, Assets, Fabric) ..."
    [Aslan.InstanceInstaller]::EnsureInstalled($g, $g.LoaderVersion, $pr, $ct)
    Ok ("instanz-installiert (" + $pr.DoneFiles + " Dateien)")
} catch { Bad ("instanz: " + $_.Exception.Message); exit 1 }

# --- 3) Verify ---
try {
    $problems = [Aslan.InstanceInstaller]::Verify($g)
    if ($problems.Count -eq 0) { Ok "instanz-verify-sauber" } else { Bad ("verify: " + ($problems -join " | ")) }
} catch { Bad ("verify: " + $_.Exception.Message) }

# --- 4) fabric-api + modmenu aus Modrinth ---
try {
    $vs = [Aslan.ModrinthService]::GetVersions("fabric-api", "fabric", "1.21.11")
    if ($vs.Count -eq 0) { Bad "fabric-api-keine-version" } else {
        $dp = New-Object Aslan.DownloadProgress
        [Aslan.ModrinthService]::Install($vs[0], $g, $dp, $ct)
        Ok ("fabric-api-installiert (" + $vs[0].FileName + ")")
    }
} catch { Bad ("fabric-api: " + $_.Exception.Message) }

# --- 5) ASLAN-Loom-JAR in Instanz (explizit, da Test-Harness kein Launcher-Verzeichnis hat) ---
$instMods = Join-Path ([Aslan.InstanceInstaller]::InstanceDir($g)) "mods"
$loomJar = Join-Path $root "mods\aslan-client\build\libs\aslan-client-0.3.0.jar"
if (Test-Path $loomJar) { Copy-Item $loomJar (Join-Path $instMods "aslan-client.jar") -Force }
$modJar = Get-ChildItem $instMods -Filter "aslan-client*.jar" -ErrorAction SilentlyContinue | Select-Object -First 1
if ($modJar -ne $null) { Ok ("aslan-mod-in-instanz (" + $modJar.Name + ", " + $modJar.Length + " Bytes)") } else { Bad "aslan-mod-fehlt" }

# --- 6) Startplan bauen + offline starten ---
try {
    $java = "$env:LOCALAPPDATA\Programs\Temurin21\jdk-21.0.12.1+1\bin\java.exe"
    if (!(Test-Path $java)) { Bad "java21-fehlt"; exit 1 }
    $uuid = [Aslan.InstanceInstaller]::OfflineUuid("Spieler")
    $plan = [Aslan.InstanceInstaller]::BuildPlan($g, "Spieler", $uuid, "-", $java, $g.LoaderVersion)
    if ($plan.Arguments.Contains("KnotClient") -and $plan.Arguments.Contains("1.21.11.jar")) { Ok "startplan-fabric" } else { Bad "startplan-falsch" }
    Write-Host "Starte Minecraft (offline, max 240 s, Fenster oeffnet sich) ..."
    $psi = New-Object Diagnostics.ProcessStartInfo
    $psi.FileName = $plan.JavaExe
    $psi.Arguments = $plan.Arguments
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.CreateNoWindow = $true
    $psi.WorkingDirectory = $plan.WorkDir
    $proc = New-Object Diagnostics.Process
    $proc.StartInfo = $psi
    $proc.Start() | Out-Null
    $out = New-Object Text.StringBuilder
    $h = { param($s,$e) if ($e.Data -ne $null) { $out.AppendLine($e.Data) | Out-Null } }.GetNewClosure()
    Register-ObjectEvent -InputObject $proc -EventName OutputDataReceived -Action $h | Out-Null
    Register-ObjectEvent -InputObject $proc -EventName ErrorDataReceived -Action $h | Out-Null
    $proc.BeginOutputReadLine(); $proc.BeginErrorReadLine()
    $exited = $proc.WaitForExit(240000)
    if (!$exited) { Write-Host "Timeout 240 s - Prozess wird beendet."; try { $proc.Kill() } catch { } }
    Start-Sleep 2
    $log = $out.ToString()
    # Konsole ist verzoegert/unvollstaendig -> massgeblich ist logs/latest.log der Instanz.
    try {
        $ll = Join-Path ([Aslan.InstanceInstaller]::InstanceDir($g)) "logs\latest.log"
        if (Test-Path $ll) { $log = [IO.File]::ReadAllText($ll) }
    } catch { }
    $log | Out-File (Join-Path $root "tests\integration-log.txt") -Encoding UTF8
    if ($log.Contains("[ASLAN]")) { Ok "aslan-mod-geladen" } else { Bad "vyra-marker-fehlt" }
    if ($log -match "Minecraft 1.21.11|Setting user|Backend library") { Ok "mc-startet" } else { Bad "mc-start-fehlt" }
    if ($log -match "Failed to load|Exception in thread.*Render|GLFW error") { Write-Host "WARN: Grafik-/Ladefehler im Log (siehe tests/integration-log.txt)" }
    Write-Host ("Exit-Code: " + $proc.ExitCode + ", Log-Zeilen: " + ($log -split "`n").Count)
} catch { Bad ("start: " + $_.Exception.Message) }

Write-Host ("== INTEGRATION FERTIG, Fehler: " + $fail + " ==")
exit $fail

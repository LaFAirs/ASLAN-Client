# Erzeugt apps/website/site/release.json aus releases/ (echte Dateien, Groessen, SHA256).
# Optional: -Github "owner/repo" und -DownloadUrl setzen, sobald das Release live ist.
param([string]$Github = "", [string]$DownloadUrl = "")
$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
$rel = Join-Path $root "releases"
$site = Join-Path $root "apps\website\site"
$shaFile = Join-Path $rel "SHA256SUMS.txt"
if (!(Test-Path $shaFile)) { throw "SHA256SUMS.txt fehlt - erst build.ps1 ausfuehren." }
$lines = Get-Content $shaFile
$files = @()
foreach ($ln in $lines) {
    $parts = $ln.Split(" ", [StringSplitOptions]::RemoveEmptyEntries)
    if ($parts.Count -lt 2) { continue }
    $name = $parts[$parts.Count - 1]
    if ($name -like "*.md") { continue }
    $p = Join-Path $rel $name
    if (!(Test-Path $p)) { Write-Host ("WARN: $name in SHA-Liste, aber Datei fehlt"); continue }
    $size = (Get-Item $p).Length
    $mb = "{0:0.0} MB" -f ($size / 1MB)
    $files += "    {`"name`": `"$name`", `"size`": `"$mb`", `"sha256`": `"$($parts[0])`"}"
}
$date = Get-Date -Format "yyyy-MM-dd"
$json = "{`"version`": `"0.3.0`", `"date`": `"$date`", `"github`": `"$Github`", `"downloadUrl`": `"$DownloadUrl`", `"files`": [`n" + ($files -join ",`n") + "`n]}"
$json | Set-Content (Join-Path $site "release.json") -Encoding ASCII
Write-Host "OK: release.json"

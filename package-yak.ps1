# Builds a Yak package for distribution.
#
# Usage:
#   .\package-yak.ps1                 # Release build + package
#   .\package-yak.ps1 -SkipBuild       # use existing bin\Release\SpaceVisual.gha
#   .\package-yak.ps1 -YakPath "...Yak.exe"   # override auto-detected Yak.exe

[CmdletBinding()]
param(
  [switch]$SkipBuild,
  [string]$YakPath
)

$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot

# ----- locate Yak.exe -----
if (-not $YakPath) {
    $candidates = @(
        "C:\Program Files\Rhino 8\System\Yak.exe",
        "C:\Program Files\Rhino 7\System\Yak.exe",
        "C:\Program Files\Rhino 8 WIP\System\Yak.exe"
    )
    $YakPath = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}
if (-not $YakPath -or -not (Test-Path $YakPath)) {
    throw "Yak.exe not found. Pass -YakPath to point at it (e.g. 'C:\Program Files\Rhino 8\System\Yak.exe')."
}
Write-Host "Yak: $YakPath" -ForegroundColor Cyan

# ----- build Release -----
if (-not $SkipBuild) {
    Write-Host "Building Release..." -ForegroundColor Cyan
    Push-Location $projectRoot
    try {
        & dotnet build -c Release -p:AutoDeployToGrasshopper=false
        if ($LASTEXITCODE -ne 0) { throw "dotnet build failed (exit $LASTEXITCODE)" }
    } finally { Pop-Location }
}

$gha = Join-Path $projectRoot "bin\Release\SpaceVisual.gha"
if (-not (Test-Path $gha)) { throw "Build output not found: $gha" }

# ----- stage into dist/ -----
$dist = Join-Path $projectRoot "dist"
if (Test-Path $dist) { Remove-Item $dist -Recurse -Force }
New-Item -ItemType Directory -Path $dist | Out-Null

Copy-Item $gha (Join-Path $dist "SpaceVisual.gha")
Copy-Item (Join-Path $projectRoot "manifest.yml") $dist
Copy-Item (Join-Path $projectRoot "LICENSE") $dist
Copy-Item (Join-Path $projectRoot "README.md") $dist
Copy-Item (Join-Path $projectRoot "icon") $dist -Recurse

# ----- yak build -----
Write-Host "yak build..." -ForegroundColor Cyan
Push-Location $dist
try {
    & $YakPath build
    if ($LASTEXITCODE -ne 0) { throw "yak build failed (exit $LASTEXITCODE)" }
} finally { Pop-Location }

$yakFile = Get-ChildItem $dist -Filter "*.yak" | Select-Object -First 1
if (-not $yakFile) { throw "No .yak file produced." }

Write-Host ""
Write-Host "Package built: $($yakFile.FullName)" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. (first time) & '$YakPath' login"
Write-Host "  2. Test locally:  & '$YakPath' install $($yakFile.FullName)"
Write-Host "  3. Publish:       & '$YakPath' push $($yakFile.FullName)"

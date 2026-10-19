param([string]$YakPath = (Join-Path $env:ProgramFiles 'Rhino 8\System\Yak.exe'))
$ErrorActionPreference = 'Stop'
$repo = Split-Path $PSScriptRoot -Parent
[xml]$project = Get-Content -LiteralPath (Join-Path $repo 'SpaceVisual.csproj')
$version = [string]$project.Project.PropertyGroup[0].Version
$stage = Join-Path $repo "dist\$version\package"
$release = Join-Path $repo "dist\$version"
New-Item -ItemType Directory -Path $stage -Force | Out-Null
$binary = Join-Path $repo 'bin\Release\SpaceVisual.gha'
if (-not (Test-Path -LiteralPath $binary)) { throw 'Build Release first.' }
Copy-Item -LiteralPath $binary -Destination $stage -Force
Copy-Item -LiteralPath (Join-Path $repo 'LICENSE'),(Join-Path $repo 'README.md'),(Join-Path $repo 'CHANGELOG.md') -Destination $stage -Force
Copy-Item -LiteralPath (Join-Path $repo 'icon\Head icon.png') -Destination (Join-Path $stage 'icon.png') -Force
@"
---
name: spacevisual
version: $version
authors:
- POLY LAB
description: >
  Visibility and sightline analysis for Grasshopper. Includes finite-segment 2D
  occlusion, separate outer boundaries and solid obstacles, filtered graph points,
  VGA metrics, 3D Isovist, visibility paths and surface visibility.
url: https://github.com/YantingShen-dev/SpaceVisual_grasshopper
keywords:
- grasshopper
- visibility
- isovist
- space-syntax
- guid:a3f5c2d1-7b48-4e91-9c3a-5d2e8f1a6b7c
icon: icon.png
"@ | Set-Content -LiteralPath (Join-Path $stage 'manifest.yml') -Encoding utf8NoBOM
Push-Location $stage
try {
    & $YakPath build --platform win
    if ($LASTEXITCODE -ne 0) { throw 'Yak build failed.' }
} finally { Pop-Location }
$yak = @(Get-ChildItem -LiteralPath $stage -Filter '*.yak')
if ($yak.Count -ne 1) { throw 'Expected one Yak package.' }
Copy-Item -LiteralPath $yak[0].FullName -Destination $release -Force
Copy-Item -LiteralPath $binary -Destination (Join-Path $release 'SpaceVisual.gha') -Force
$zip = Join-Path $release "SpaceVisual-$version-Windows.zip"
Compress-Archive -LiteralPath (Join-Path $stage 'SpaceVisual.gha'),(Join-Path $stage 'README.md'),(Join-Path $stage 'LICENSE'),(Join-Path $stage 'CHANGELOG.md') -DestinationPath $zip -Force
$hashes = Get-ChildItem -LiteralPath $release -File | Where-Object { $_.Extension -in '.gha','.yak','.zip' } | ForEach-Object {
    '{0}  {1}' -f (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant(), $_.Name
}
$hashes | Set-Content -LiteralPath (Join-Path $release 'SHA256SUMS.txt') -Encoding utf8NoBOM
Write-Output "Release assets: $release"

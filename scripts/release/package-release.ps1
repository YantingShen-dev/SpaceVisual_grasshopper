param(
    [string]$YakPath = (Join-Path $env:ProgramFiles 'Rhino 8\System\Yak.exe'),
    [string]$BinaryPath
)
$ErrorActionPreference = 'Stop'
$repo = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
[xml]$project = Get-Content -LiteralPath (Join-Path $repo 'SpaceVisual.csproj')
$version = [string]$project.Project.PropertyGroup[0].Version
$stageRoot = [IO.Path]::GetFullPath((Join-Path $repo 'artifacts\packaging'))
$stage = Join-Path $stageRoot ([Guid]::NewGuid().ToString('N'))
$release = Join-Path $repo "dist\$version"
$binaryName = "SpaceVisual_$version.gha"
$binary = if ($BinaryPath) { [IO.Path]::GetFullPath($BinaryPath) } else { Join-Path $repo 'bin\Release\SpaceVisual.gha' }
if (-not (Test-Path -LiteralPath $binary)) { throw 'Build Release first.' }
$info = [Diagnostics.FileVersionInfo]::GetVersionInfo($binary)
if ($info.FileVersion -ne "$version.0" -or $info.OriginalFilename -ne 'SpaceVisual.gha') { throw 'Binary version or edition does not match the production project.' }
if (-not (Test-Path -LiteralPath $YakPath -PathType Leaf)) { throw 'Yak executable not found; supply -YakPath.' }
# Published assets must not be silently regenerated with different checksums.
foreach ($name in @($binaryName, "SpaceVisual-$version-Windows.zip", "spacevisual-$version-rh7_0-win.yak", 'SHA256SUMS.txt')) {
    if (Test-Path -LiteralPath (Join-Path $release $name)) { throw "Release asset already exists: $name. Use a new version for a new release." }
}
New-Item -ItemType Directory -Path $stage -Force | Out-Null
New-Item -ItemType Directory -Path $release -Force | Out-Null
try {
Copy-Item -LiteralPath $binary -Destination (Join-Path $stage $binaryName) -Force
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
Copy-Item -LiteralPath $binary -Destination (Join-Path $release $binaryName) -Force
$zip = Join-Path $release "SpaceVisual-$version-Windows.zip"
Compress-Archive -LiteralPath (Join-Path $stage $binaryName),(Join-Path $stage 'README.md'),(Join-Path $stage 'LICENSE'),(Join-Path $stage 'CHANGELOG.md') -DestinationPath $zip -Force
$hashes = Get-ChildItem -LiteralPath $release -File | Where-Object { $_.Extension -in '.gha','.yak','.zip' } | ForEach-Object {
    '{0}  {1}' -f (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant(), $_.Name
}
$hashes | Set-Content -LiteralPath (Join-Path $release 'SHA256SUMS.txt') -Encoding utf8NoBOM
Copy-Item -LiteralPath $binary -Destination (Join-Path $repo "bin\Release\$binaryName") -Force
$binRoot = [IO.Path]::GetFullPath((Join-Path $repo 'bin\Release'))
foreach ($oldBinary in Get-ChildItem -LiteralPath $binRoot -File) {
    if ($oldBinary.Name -match '^SpaceVisual_\d+\.\d+\.\d+\.gha$' -and $oldBinary.Name -ne $binaryName) {
        if ([IO.Path]::GetDirectoryName($oldBinary.FullName) -ne $binRoot -or ($oldBinary.Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Unsafe obsolete binary path.' }
        Remove-Item -LiteralPath $oldBinary.FullName -Force
    }
}
Write-Output "Release assets: $release"
} finally {
    $resolvedStage = [IO.Path]::GetFullPath($stage)
    if (-not $resolvedStage.StartsWith($stageRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Unsafe staging path: $resolvedStage"
    }
    if (Test-Path -LiteralPath $resolvedStage) { Remove-Item -LiteralPath $resolvedStage -Recurse -Force }
}

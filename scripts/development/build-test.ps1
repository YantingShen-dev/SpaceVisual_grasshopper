param(
    [switch]$Install,
    [string]$LibrariesPath = (Join-Path $env:APPDATA 'Grasshopper\Libraries')
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
$dotnetPath = if ($dotnetCommand) { $dotnetCommand.Source } else { Join-Path $env:ProgramFiles 'dotnet\dotnet.exe' }
if (-not (Test-Path -LiteralPath $dotnetPath)) { throw 'Install a .NET SDK before building.' }

& $dotnetPath build (Join-Path $repoRoot 'SpaceVisual.csproj') -c Test -p:AutoDeployToGrasshopper=false
if ($LASTEXITCODE -ne 0) { throw 'Test build failed. Installed plugins were not changed.' }

$outputDirectory = Join-Path $repoRoot 'bin\Test'
$gha = Join-Path $outputDirectory 'SpaceVisual.Test.gha'
Write-Output "Test plugin: $gha"
if (-not $Install) { return }
if (-not (Test-Path -LiteralPath $LibrariesPath -PathType Container)) {
    throw 'Grasshopper Libraries folder not found. Supply -LibrariesPath from File > Special Folders > Components Folder.'
}

$productionPath = Join-Path $LibrariesPath 'SpaceVisual.gha'
$productionHash = if (Test-Path -LiteralPath $productionPath) { (Get-FileHash -LiteralPath $productionPath).Hash } else { $null }
$targetGha = Join-Path $LibrariesPath 'SpaceVisual.Test.gha'
$targetPdb = Join-Path $LibrariesPath 'SpaceVisual.Test.pdb'

# Replace the previous test build directly; do not accumulate binary backups.
try {
    Copy-Item -LiteralPath $gha -Destination $targetGha -Force
    Copy-Item -LiteralPath (Join-Path $outputDirectory 'SpaceVisual.Test.pdb') -Destination $targetPdb -Force
} catch {
    throw "Could not replace the test plugin. Save your work and close Rhino sessions that loaded it, then rerun this command. $($_.Exception.Message)"
}
if ((Get-FileHash -LiteralPath $targetGha).Hash -ne (Get-FileHash -LiteralPath $gha).Hash) { throw 'Installed test binary hash mismatch.' }
if ($productionHash -and (Get-FileHash -LiteralPath $productionPath).Hash -ne $productionHash) { throw 'Production plugin changed unexpectedly.' }

$reportDirectory = Join-Path $repoRoot 'artifacts\validation'
New-Item -ItemType Directory -Path $reportDirectory -Force | Out-Null
@{
    installedAt = (Get-Date).ToString('o')
    path = $targetGha
    sha256 = (Get-FileHash -LiteralPath $targetGha).Hash
    productionSha256 = $productionHash
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $reportDirectory 'installation.json') -Encoding UTF8
Write-Output "Installed: $targetGha"
Write-Output 'Open a fresh Rhino session to load this build. Existing sessions keep their loaded assembly until restart.'

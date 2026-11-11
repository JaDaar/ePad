<#
.SYNOPSIS
  Builds the ePad release: a self-contained single-file ePad.exe, a portable zip and an installer.

.DESCRIPTION
  Output goes to the artifacts folder:
    artifacts\ePad-Setup-<version>.exe           installer
    artifacts\ePad-<version>-win-x64-portable.zip  portable copy (just ePad.exe)
  The version comes from <Version> in src\ePad\ePad.csproj.
  Requires the .NET 10 SDK and Inno Setup 6 (winget install JRSoftware.InnoSetup).

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File build\release.ps1
#>
param(
    [string]$Runtime = 'win-x64'
)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$project = Join-Path $root 'src\ePad\ePad.csproj'
$artifacts = Join-Path $root 'artifacts'
$publish = Join-Path $artifacts "publish\$Runtime"

# Version from the project file.
[xml]$xml = Get-Content $project
$version = @($xml.Project.PropertyGroup.Version | Where-Object { $_ })[0]
if (-not $version) { throw "No <Version> found in $project" }
Write-Host "Building ePad $version for $Runtime" -ForegroundColor Cyan

# 1. Publish a self-contained single-file exe (no .NET install needed on the target PC).
if (Test-Path $publish) { Remove-Item $publish -Recurse -Force }
dotnet publish $project `
    -c Release `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=none `
    -o $publish
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed' }

# 2. Portable zip.
$zip = Join-Path $artifacts "ePad-$version-$Runtime-portable.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $publish 'ePad.exe') -DestinationPath $zip

# 3. Installer.
$iscc = @(
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) { throw 'Inno Setup 6 not found. Install it with: winget install JRSoftware.InnoSetup' }

& $iscc /Q "/DAppVersion=$version" "/DSourceDir=$publish" (Join-Path $root 'installer\ePad.iss')
if ($LASTEXITCODE -ne 0) { throw 'Inno Setup failed' }

Write-Host "`nRelease files:" -ForegroundColor Green
Get-ChildItem $artifacts -File | ForEach-Object { '  {0,-40} {1,8:N1} MB' -f $_.Name, ($_.Length / 1MB) }

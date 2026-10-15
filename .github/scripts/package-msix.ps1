<#
.SYNOPSIS
  Builds HushLittlePC (self-contained) and packages it as an unsigned MSIX for Microsoft Store upload.
  The Store signs the package itself, so no certificate is needed.

.PARAMETER Version
  Semantic version (e.g. 1.2.3). The MSIX version becomes 1.2.3.0 (the 4th part is reserved by the Store).

.NOTES
  Identity values come from Partner Center > Product management > Product identity:
    MSIX_IDENTITY_NAME            Package/Identity/Name
    MSIX_PUBLISHER                Package/Identity/Publisher   (e.g. CN=XXXXXXXX-XXXX-...)
    MSIX_PUBLISHER_DISPLAY_NAME   Package/Properties/PublisherDisplayName
  If they aren't set, placeholder values are used (fine for CI builds, not uploadable to the Store).
#>
param(
    [Parameter(Mandatory = $true)][string] $Version,
    [string] $Project = 'HushLittlePC.csproj',
    [string] $OutputDir = 'artifacts/msix'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
Set-Location $repoRoot

$core = ($Version -split '[-+]')[0]
if ($core -notmatch '^\d+\.\d+\.\d+$') { throw "Version '$Version' is not a valid semantic version." }
$msixVersion = "$core.0"

function Get-OrDefault($value, $default) { if ([string]::IsNullOrWhiteSpace($value)) { $default } else { $value } }
if (-not $env:MSIX_IDENTITY_NAME) {
    Write-Warning 'MSIX_* variables not set: using placeholder identity (this package cannot be submitted to the Store).'
}
$identityName     = Get-OrDefault $env:MSIX_IDENTITY_NAME 'HushLittlePC.Dev'
$publisher        = Get-OrDefault $env:MSIX_PUBLISHER 'CN=HushLittlePC'
$publisherDisplay = Get-OrDefault $env:MSIX_PUBLISHER_DISPLAY_NAME 'HushLittlePC'

$publishDir = Join-Path $repoRoot 'artifacts/publish'
$stageDir   = Join-Path $repoRoot 'artifacts/msix-layout'
$outDir     = Join-Path $repoRoot $OutputDir
Remove-Item $publishDir, $stageDir -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force -Path $stageDir, $outDir | Out-Null

# 1. Self-contained publish (the Store package can't rely on a user-installed .NET runtime)
dotnet publish $Project -c Release -r win-x64 --self-contained true "-p:Version=$core" -p:PublishSingleFile=false -o $publishDir
if ($LASTEXITCODE -ne 0) { throw 'dotnet publish failed.' }

# 2. Lay out the package: app files + assets + rendered manifest
Copy-Item (Join-Path $publishDir '*') $stageDir -Recurse -Exclude '*.pdb'
Copy-Item (Join-Path $repoRoot 'packaging/Assets') (Join-Path $stageDir 'Assets') -Recurse

$esc = { param($s) [System.Security.SecurityElement]::Escape($s) }
$manifest = Get-Content (Join-Path $repoRoot 'packaging/AppxManifest.xml') -Raw
$manifest = $manifest.Replace('{{IDENTITY_NAME}}', (& $esc $identityName))
$manifest = $manifest.Replace('{{PUBLISHER}}', (& $esc $publisher))
$manifest = $manifest.Replace('{{PUBLISHER_DISPLAY_NAME}}', (& $esc $publisherDisplay))
$manifest = $manifest.Replace('{{VERSION}}', $msixVersion)
[System.IO.File]::WriteAllText((Join-Path $stageDir 'AppxManifest.xml'), $manifest, [System.Text.UTF8Encoding]::new($false))

# 3. Find the newest makeappx.exe from the Windows SDK preinstalled on the runner
$sdkBin = Join-Path ${env:ProgramFiles(x86)} 'Windows Kits/10/bin'
$makeappx = Get-ChildItem $sdkBin -Recurse -Filter makeappx.exe -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -match '\\10\.\d+\.\d+\.\d+\\x64\\' } |
    Sort-Object { [version]([regex]::Match($_.FullName, '10\.\d+\.\d+\.\d+').Value) } -Descending |
    Select-Object -First 1
if (-not $makeappx) { throw "makeappx.exe not found under $sdkBin" }

# 4. Pack
$msix = Join-Path $outDir "HushLittlePC_${msixVersion}_x64.msix"
& $makeappx.FullName pack /d $stageDir /p $msix /o
if ($LASTEXITCODE -ne 0) { throw 'makeappx pack failed.' }

Write-Host "Created $msix"

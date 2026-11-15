[CmdletBinding()]
param(
    [switch] $Disable
)

$ErrorActionPreference = 'Stop'

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = [Security.Principal.WindowsPrincipal]::new($identity)
if (-not $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Run this script from an elevated PowerShell session (Run as administrator).'
}

$registryPath = 'HKLM:\SOFTWARE\Microsoft\Windows\Windows Error Reporting\LocalDumps\HushLittlePC.WinUI.exe'

if ($Disable) {
    if (Test-Path -LiteralPath $registryPath) {
        Remove-Item -LiteralPath $registryPath -Recurse -Force
    }

    Write-Host 'Disabled local crash dumps for HushLittlePC.WinUI.exe.'
    exit 0
}

$dumpFolder = Join-Path $env:LOCALAPPDATA 'HushLittlePC\CrashDumps'
New-Item -ItemType Directory -Path $dumpFolder -Force | Out-Null
New-Item -Path $registryPath -Force | Out-Null

New-ItemProperty -Path $registryPath -Name DumpFolder -PropertyType ExpandString -Value $dumpFolder -Force | Out-Null
New-ItemProperty -Path $registryPath -Name DumpCount -PropertyType DWord -Value 5 -Force | Out-Null
New-ItemProperty -Path $registryPath -Name DumpType -PropertyType DWord -Value 2 -Force | Out-Null

Write-Host "Enabled full local crash dumps for HushLittlePC.WinUI.exe."
Write-Host "Dump folder: $dumpFolder"
Write-Host 'Reproduce the crash, then disable collection with: .\scripts\Enable-WinUICrashDumps.ps1 -Disable'

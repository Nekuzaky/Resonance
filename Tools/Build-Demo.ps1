<#
.SYNOPSIS
    Regenerates the demo: procedural audio clips, Resonance assets and the demo scene in Assets/Demo.
.DESCRIPTION
    Runs Nekuzaky.Resonance.Dev.DemoBuilder.Build in batch mode. Run Tools/Sync-Sample.ps1 afterwards to update the
    package sample.
.EXAMPLE
    ./Tools/Build-Demo.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'Resonance.Tools.psm1') -Force

$logFile = Join-Path (Get-OutputDirectory 'logs') 'demo.log'
Write-Host "Building the demo with Unity $(Get-UnityVersion)..."
$exitCode = Invoke-Unity -Arguments @('-executeMethod', 'Nekuzaky.Resonance.Dev.DemoBuilder.Build', '-quit') -LogFile $logFile
if ($exitCode -ne 0 -or -not (Select-String -Path $logFile -Pattern 'Resonance demo rebuilt' -Quiet)) {
    Write-Host "The demo was not rebuilt (exit code $exitCode). Log: $logFile" -ForegroundColor Red
    exit 1
}
Write-Host 'Demo rebuilt in Assets/Demo.' -ForegroundColor Green

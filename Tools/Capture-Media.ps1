<#
.SYNOPSIS
    Records the README screenshots of Resonance Studio from the real Unity Editor.
.DESCRIPTION
    Opens the editor (not in batch mode), shows the Studio on the demo assets, plays the demo scene and saves the
    Events, Mixer, Music and Profiler tabs to docs/images. Each view is rendered into a texture, never read from the
    screen, so other windows never end up in the images. It takes about a minute.
.EXAMPLE
    ./Tools/Capture-Media.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'Resonance.Tools.psm1') -Force

$logFile = Join-Path (Get-OutputDirectory 'logs') 'capture.log'
Write-Host 'Opening the editor to capture the Studio...'
$exitCode = Invoke-Unity -Interactive -Arguments @('-executeMethod', 'Nekuzaky.Resonance.Dev.MediaCapture.Run') -LogFile $logFile
$captured = @(Select-String -Path $logFile -Pattern '^Captured ').Count
if ($captured -lt 5) {
    Write-Host "Only $captured images were captured (exit code $exitCode). Log: $logFile" -ForegroundColor Red
    exit 1
}
Write-Host "$captured images saved in docs/images." -ForegroundColor Green

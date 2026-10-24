<#
.SYNOPSIS
    Checks remote profiling end to end: a development player sends snapshots to the editor's Resonance Profiler.
.DESCRIPTION
    Opens the editor (not in batch mode, so it can listen for players), builds a development Windows player that
    connects to it, runs the player with the smoke test and counts the snapshots the Studio receives. Takes about a
    minute; a small player window appears while it runs.
.EXAMPLE
    ./Tools/Remote-Check.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'Resonance.Tools.psm1') -Force

$logFile = Join-Path (Get-OutputDirectory 'logs') 'remote.log'
Write-Host 'Opening the editor to profile a development player...'
$exitCode = Invoke-Unity -Interactive -Arguments @('-executeMethod', 'Nekuzaky.Resonance.Dev.RemoteCheck.Run') -LogFile $logFile
Select-String -Path $logFile -Pattern '\[Remote\]' | ForEach-Object { Write-Host $_.Line }
if ($exitCode -ne 0) {
    Write-Host "Remote profiling check failed (exit code $exitCode). Log: $logFile" -ForegroundColor Red
    exit 1
}
Write-Host 'Remote profiling: OK' -ForegroundColor Green

<#
.SYNOPSIS
    Builds the demo as a Windows player, runs it headless with the smoke test and checks the result.
.DESCRIPTION
    The player is started with -resonanceSmoke: it plays the demo for about 11 seconds, checks that sounds played,
    that the narrator ducked the music and that the score moved to the combat segment, then quits with exit code 0
    on success. It also plays a Timeline through a PlayableDirector, loads the demo bank through Addressables and
    measures the engine with 400 sounds at once. -Record runs the player again and records its real mix from the
    Audio Listener (the speakers stay silent) to Tools/Output/media/demo-mix.wav, then checks it with ffmpeg: no
    sample may reach 0 dBFS. -Platforms also compiles the player scripts for Android and WebGL (no SDK needed).
.EXAMPLE
    ./Tools/Build-Check.ps1
    ./Tools/Build-Check.ps1 -Platforms
    ./Tools/Build-Check.ps1 -Record
#>
[CmdletBinding()]
param([switch] $Platforms, [switch] $Record)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'Resonance.Tools.psm1') -Force

$root = Get-RepoRoot
$logFile = Join-Path (Get-OutputDirectory 'logs') 'build.log'
Write-Host 'Building the Windows player...'
$exitCode = Invoke-Unity -Arguments @('-executeMethod', 'Nekuzaky.Resonance.Dev.BuildCheck.Run') -LogFile $logFile
Select-String -Path $logFile -Pattern '\[BuildCheck\]' | ForEach-Object { Write-Host $_.Line }
if ($exitCode -ne 0) { Write-Host "Build failed. Log: $logFile" -ForegroundColor Red; exit 1 }

$exe = Join-Path $root 'Tools\Output\build\windows\ResonanceDemo.exe'
$playerLog = Join-Path (Get-OutputDirectory 'logs') 'player.log'
Write-Host 'Running the player smoke test...'
$player = Start-Process -FilePath $exe -ArgumentList @('-batchmode', '-nographics', '-resonanceSmoke', '-logFile', "`"$playerLog`"") -PassThru -WindowStyle Hidden
if (-not $player.WaitForExit(60000)) {
    $player.Kill()
    Write-Host 'The player did not quit within 60 s.' -ForegroundColor Red
    exit 1
}
Select-String -Path $playerLog -Pattern '\[Smoke\]' | ForEach-Object { Write-Host $_.Line }
if ($player.ExitCode -ne 0) {
    Write-Host "Smoke test failed (exit code $($player.ExitCode)). Log: $playerLog" -ForegroundColor Red
    exit 1
}
Write-Host 'Windows player: OK' -ForegroundColor Green

if ($Record) {
    $wav = Join-Path (Get-OutputDirectory 'media') 'demo-mix.wav'
    $recordLog = Join-Path (Get-OutputDirectory 'logs') 'record.log'
    Write-Host 'Recording the player mix...'
    $player = Start-Process -FilePath $exe -ArgumentList @('-batchmode', '-nographics', '-resonanceSmoke', '-resonanceRecord', "`"$wav`"", '-logFile', "`"$recordLog`"") -PassThru -WindowStyle Hidden
    if (-not $player.WaitForExit(60000)) { $player.Kill(); Write-Host 'The recording did not finish within 60 s.' -ForegroundColor Red; exit 1 }
    Select-String -Path $recordLog -Pattern '\[Record\] wrote' | ForEach-Object { Write-Host $_.Line }
    $report = & cmd /c ffmpeg -hide_banner -nostats -i $wav -af 'astats=measure_perchannel=none,ebur128=framelog=quiet' -f null - "2>&1" | Out-String
    $peak = [double]([regex]::Match($report, 'Peak level dB:\s+(-?[\d.]+|-inf)').Groups[1].Value)
    $loudness = [regex]::Matches($report, 'I:\s+(-?[\d.]+) LUFS') | Select-Object -Last 1
    Write-Host ('Recorded mix: peak {0:0.0} dBFS, {1} LUFS integrated' -f $peak, $loudness.Groups[1].Value)
    if ($peak -ge 0) { Write-Host 'The mix clips.' -ForegroundColor Red; exit 1 }
    Write-Host 'Recorded mix: OK' -ForegroundColor Green
}

if ($Platforms) {
    foreach ($platform in 'Android', 'WebGL') {
        $platformLog = Join-Path (Get-OutputDirectory 'logs') "scripts-$platform.log"
        $exitCode = Invoke-Unity -Arguments @('-executeMethod', "Nekuzaky.Resonance.Dev.BuildCheck.Run$platform") -LogFile $platformLog
        Select-String -Path $platformLog -Pattern '\[BuildCheck\]|error CS' | ForEach-Object { Write-Host $_.Line }
        if ($exitCode -ne 0) {
            Write-Host "$platform scripts failed. Log: $platformLog" -ForegroundColor Red
            exit 1
        }
    }
    Write-Host 'Android and WebGL player scripts: OK' -ForegroundColor Green
}

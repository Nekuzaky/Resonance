<#
.SYNOPSIS
    Records the README screenshots of Resonance Studio from the real Unity Editor.
.DESCRIPTION
    Opens the editor (not in batch mode), shows the Studio on the demo assets, plays the demo scene and saves the
    Events, Mixer, Music and Profiler tabs to docs/images, then opens it again to record the demo GIF and MP4. Each view is rendered into a texture, never read from the
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

$ffmpeg = Get-Command ffmpeg -ErrorAction SilentlyContinue
if (-not $ffmpeg) { throw 'ffmpeg was not found on PATH. Install it, for example with: winget install Gyan.FFmpeg' }
$gifLog = Join-Path (Get-OutputDirectory 'logs') 'gif.log'
Write-Host 'Opening the editor again to record the GIF...'
$exitCode = Invoke-Unity -Interactive -Arguments @('-executeMethod', 'Nekuzaky.Resonance.Dev.GifRecorder.Run') -LogFile $gifLog
$frames = Join-Path (Get-OutputDirectory 'media') 'frames'
$count = @(Get-ChildItem $frames -Filter 'frame_*.png' -ErrorAction SilentlyContinue).Count
if ($count -lt 30) {
    Write-Host "Only $count frames were recorded (exit code $exitCode). Log: $gifLog" -ForegroundColor Red
    exit 1
}
$images = Join-Path (Get-RepoRoot) 'docs\images'
$pattern = Join-Path $frames 'frame_%04d.png'
$palette = Join-Path $frames 'palette.png'
$gif = Join-Path $images 'resonance-demo.gif'
$mp4 = Join-Path $images 'resonance-demo.mp4'
$scale = 'fps=15,scale=960:-1:flags=lanczos'
& $ffmpeg.Source -y -loglevel error -framerate 15 -i $pattern -vf "$scale,palettegen=stats_mode=diff" $palette
& $ffmpeg.Source -y -loglevel error -framerate 15 -i $pattern -i $palette -lavfi "$scale [x]; [x][1:v] paletteuse=dither=sierra2_4a" $gif
& $ffmpeg.Source -y -loglevel error -framerate 15 -i $pattern -vf 'scale=trunc(iw/2)*2:trunc(ih/2)*2,format=yuv420p' -c:v libx264 -crf 20 -movflags +faststart $mp4
Write-Host "$count frames -> $gif ($([math]::Round((Get-Item $gif).Length / 1MB, 1)) MB)" -ForegroundColor Green

<#
.SYNOPSIS
    Checks the editor-side features that need a running editor: Studio preview audio, loudness accuracy and gizmos.
.DESCRIPTION
    1. Plays a sound through the Studio preview in Edit Mode (master muted) and checks that its AudioSource advances.
       Then plays sounds and music through buses with effects and records the real output at the Audio Listener
       (the speakers stay silent): the low pass cuts a tone, the delay and reverb tails ring after the sound ends, the
       distortion drive ignores the bus fader, and music layers are filtered too.
    2. Measures every demo clip with the Audit loudness meter and compares it with ffmpeg's EBU R128 meter; clips
       shorter than 400 ms are skipped because the standard does not measure them.
    3. Opens the editor, selects the demo car and saves the Scene view with its gizmos to docs/images.
.EXAMPLE
    ./Tools/Editor-Check.ps1
#>
[CmdletBinding()]
param([double] $Tolerance = 0.3)

$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'Resonance.Tools.psm1') -Force
$root = Get-RepoRoot
$logs = Get-OutputDirectory 'logs'
$failed = $false

$previewLog = Join-Path $logs 'preview.log'
$exitCode = Invoke-Unity -Arguments @('-executeMethod', 'Nekuzaky.Resonance.Dev.PreviewCheck.Run') -LogFile $previewLog
Select-String -Path $previewLog -Pattern '\[PreviewCheck\]' | ForEach-Object { Write-Host $_.Line }
if ($exitCode -ne 0) { $failed = $true }

$effectsLog = Join-Path $logs 'effects.log'
$exitCode = Invoke-Unity -Arguments @('-executeMethod', 'Nekuzaky.Resonance.Dev.EffectsCheck.Run') -LogFile $effectsLog
Select-String -Path $effectsLog -Pattern '\[EffectsCheck\]' | ForEach-Object { Write-Host $_.Line }
if ($exitCode -ne 0) { $failed = $true }

$ffmpeg = Get-Command ffmpeg -ErrorAction SilentlyContinue
if (-not $ffmpeg) { throw 'ffmpeg was not found on PATH. Install it, for example with: winget install Gyan.FFmpeg' }
$loudnessLog = Join-Path $logs 'loudness.log'
$exitCode = Invoke-Unity -Arguments @('-executeMethod', 'Nekuzaky.Resonance.Dev.LoudnessCheck.Run') -LogFile $loudnessLog
if ($exitCode -ne 0) { throw "The loudness export failed. Log: $loudnessLog" }
foreach ($line in Get-Content (Join-Path $root 'Tools\Output\loudness.tsv')) {
    $path, $ours = $line -split "`t"
    $full = Join-Path $root $path
    $seconds = [double](& cmd /c ffprobe -v error -show_entries format=duration -of csv=p=0 $full)
    if ($seconds -lt 0.4) { continue }
    $report = & cmd /c ffmpeg -hide_banner -nostats -i $full -af 'ebur128=framelog=quiet' -f null - "2>&1" | Out-String
    $reference = [double]([regex]::Matches($report, 'I:\s+(-?[\d.]+) LUFS') | Select-Object -Last 1).Groups[1].Value
    $difference = [double]::Parse($ours, [Globalization.CultureInfo]::InvariantCulture) - $reference
    $ok = [math]::Abs($difference) -le $Tolerance
    if (-not $ok) { $failed = $true }
    Write-Host ('{0} {1,-28} Resonance {2,7} LUFS   ffmpeg {3,6:0.0} LUFS' -f $(if ($ok) { 'PASS' } else { 'FAIL' }), (Split-Path $path -Leaf), $ours, $reference)
}

$gizmoLog = Join-Path $logs 'gizmos.log'
$exitCode = Invoke-Unity -Interactive -Arguments @('-executeMethod', 'Nekuzaky.Resonance.Dev.GizmoCapture.Run') -LogFile $gizmoLog
Select-String -Path $gizmoLog -Pattern '\[GizmoCapture\]' | ForEach-Object { Write-Host $_.Line }
if ($exitCode -ne 0) { $failed = $true }

if ($failed) { Write-Host 'Editor checks failed.' -ForegroundColor Red; exit 1 }
Write-Host 'Editor checks: OK' -ForegroundColor Green

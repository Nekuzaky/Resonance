# Tools

PowerShell scripts for every development task. They work with Windows PowerShell 5.1 and PowerShell 7. Scripts that
start Unity find the editor from `ProjectSettings/ProjectVersion.txt` (set `UNITY_EDITOR` to override) and need the
project to be closed in the editor. Logs and results go to `Tools/Output`, which git ignores.

| Script | What it does |
|---|---|
| `Compile.ps1` | Compiles every assembly; fails on any error or warning. `-Full` rebuilds everything. |
| `Test.ps1` | Runs the EditMode tests. `-Filter` runs a subset. |
| `Check-Rules.ps1` | Checks function length, recursion, `goto`, `unsafe`, silenced warnings and `-warnaserror+` placement. |
| `Build-Demo.ps1` | Regenerates the demo audio, Resonance assets and scene in `Assets/Demo`. |
| `Capture-Media.ps1` | Records the README screenshots from the real editor. |
| `Sync-Sample.ps1` | Copies `Assets/Demo` into the package sample, without `csc.rsp`. |

The banner and social preview are drawn by `docs/brand/render_brand.py` (Python with Pillow).

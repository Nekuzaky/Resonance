# Contributing to Resonance

Thanks for helping! Bug reports, ideas and pull requests are all welcome.

## Getting started

1. Install **Unity 6000.3** (6000.3.13f1 is the tested version).
2. Add the repository root in Unity Hub (*Add project from disk*). The tool is the embedded package in
   `Packages/com.nekuzaky.resonance`; the demo lives in `Assets/Demo` and development-only tools in `Assets/Dev`.
3. Open `Assets/Demo/Resonance Demo.unity`, press Play and open **Tools > Resonance > Resonance Studio**.

Every development task is a script in [`Tools/`](Tools/README.md). Those that start Unity need the project to be closed
in the editor.

| Script | What it does |
|---|---|
| `Tools/Compile.ps1` | Compiles every assembly and lists errors and warnings. |
| `Tools/Test.ps1` | Runs the EditMode tests. |
| `Tools/Check-Rules.ps1` | Checks the code rules listed below. |
| `Tools/Build-Demo.ps1` | Regenerates the demo audio, assets and scene. |
| `Tools/Capture-Media.ps1` | Records the README screenshots from the real editor. |
| `Tools/Sync-Sample.ps1` | Copies `Assets/Demo` into the package sample. |

## Rules the code follows

Resonance follows the spirit of NASA's *Power of 10* rules, adapted to C#:

- No recursion and no `goto`; every loop has a fixed upper bound. Sound trees are walked with explicit stacks and a
  node limit.
- Functions stay short, under 60 lines.
- No coroutines: the engine is ticked from one `LateUpdate` (or `EditorApplication.update` for the preview).
- No `unsafe` code.
- Zero compiler warnings. `Tools/Compile.ps1` fails on any warning (`-Full` rebuilds everything), and the development
  assemblies under `Assets` build with `-warnaserror+`. The package itself does not force warnings as errors: a
  deprecation in a future Unity version must never stop someone else's project from compiling.
- Every `MonoBehaviour` and `ScriptableObject` lives in a file named after it, so Unity can find its script and icon.

Style: 4 spaces, `_camelCase` private fields, `#region` blocks like the existing files, English everywhere.

## Tests

Engine tests run with the engine's simulated playback, so they do not depend on an audio device and give the same
result on every machine. Add a test with every fix.

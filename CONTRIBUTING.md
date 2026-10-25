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
| `Tools/Build-Check.ps1` | Builds the demo player, runs a headless smoke test in it (Timeline, Addressables, bus effects, 400 sounds); `-Record` records and checks the real mix; `-Platforms` compiles for Android and WebGL. |
| `Tools/Editor-Check.ps1` | Checks preview audio and bus effects in Edit Mode, the loudness meter against ffmpeg, and the Scene view gizmos. |
| `Tools/Remote-Check.ps1` | Profiles a development player from the editor, end to end. |
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

Engine tests use simulated playback, so they do not depend on an audio device and give the same result on every
machine. `PerformanceTests` checks that playing and updating sounds allocates no managed memory once warmed up. The
suite also passes in URP and HDRP projects. Add a test with every fix.
## Continuous integration

`.github/workflows/tests.yml` checks on every push and pull request that every package file has its `.meta` and that
none is a Git LFS pointer, then runs the package tests with [GameCI](https://game.ci). The Unity steps need three
repository secrets:

| Secret | Value |
| --- | --- |
| `UNITY_LICENSE` | Contents of your `Unity_lic.ulf` file (Personal licenses work) |
| `UNITY_EMAIL` | Your Unity account email |
| `UNITY_PASSWORD` | Your Unity account password |

See [GameCI activation](https://game.ci/docs/github/activation) for how to get the license file. Without these secrets
the workflow skips the Unity steps and reports a notice instead of failing.

# Resonance Documentation

Resonance is game audio middleware that lives inside Unity. Sound designers build sounds, mixes and adaptive music in the
**Resonance Studio** window and attach them to the scene with components. Programmers get a small, typed API when they
need one. There are no soundbanks to build and no external tool to keep in sync.

## Pages

| Page | What it covers |
| --- | --- |
| [Getting started](getting-started.md) | Install the package, make your first sound, hear it in the game |
| [Sound Events](sound-events.md) | Containers (Random, Sequence, Switch, Blend, Layer), randomization, 3D, voice limits |
| [Game syncs](game-syncs.md) | Parameters, switches and states, and how sounds react to them |
| [Mixing](mixing.md) | Buses, ducking, snapshots, mute and solo, Audio Mixer output |
| [Interactive music](music.md) | Segments, beat-synced transitions, state links, stingers |
| [Components](components.md) | Every component, for setups that need no code |
| [Scripting](scripting.md) | The `Resonance` API for programmers |
| [Profiler](profiler.md) | Reading the live view of voices, events and game syncs |
| [Coming from Wwise](wwise.md) | Wwise concepts and their Resonance equivalents |

## Requirements

- Unity 6000.3 or newer
- Any render pipeline, any platform that supports Unity's built-in audio

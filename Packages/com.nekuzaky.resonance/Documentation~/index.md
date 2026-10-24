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
| [Distance, occlusion and reverb](space.md) | Attenuation curves and presets, occlusion, reverb zones |
| [Memory and loading](memory.md) | Sound Banks, loading on demand, import settings |
| [Interactive music](music.md) | Segments, beat-synced transitions, state links, bridges, stingers |
| [Dialogue and subtitles](dialogue.md) | Lines in every language, fallback language, subtitle events |
| [Components](components.md) | Every component, for setups that need no code |
| [Scripting](scripting.md) | The `Resonance` API for programmers |
| [Profiler](profiler.md) | Live view of voices, events and game syncs, its 60 second history, and profiling a player build |
| [Workflow tools](workflow.md) | Bulk import, project audit, Used By |
| [Timeline](timeline.md) | Sound Event and Game State clips on a Resonance Track |
| [Coming from Wwise](wwise.md) | Wwise concepts and their Resonance equivalents |

## Requirements

- Unity 6000.3 or newer
- Any render pipeline, any platform that supports Unity's built-in audio

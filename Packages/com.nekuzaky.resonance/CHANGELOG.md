# Changelog

All notable changes to this package are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project uses [Semantic Versioning](https://semver.org/).

## [0.3.0] - 2026-10-24

### Added

- Dialogue nodes: one line per language with subtitles, a fallback language, `Resonance.Language`, and
  `SubtitleStarted` and `SubtitleEnded` engine events. Subtitle Relay component to show subtitles without code.
- Music bridges: a state link can play a bridge segment before the linked segment.
- Binaural option on 3D settings, rendered through the project's spatializer plugin when one is selected.
- Remote profiling: development builds send snapshots to the editor, and the Profiler tab shows the player with its
  history.
- Mobile import overrides (Android and iOS) suggested by the Audit tab.
- The demo narrator speaks in English or French with subtitles on screen, and the score plays a riser before combat.

### Verified

- A development Windows player streamed 80 snapshots to the editor's Profiler during the smoke test.

## [0.2.0] - 2026-10-24

### Added

- Audit tab: errors and warnings in Sound Events, Music Tracks and Buses, sounds nothing references, and import
  settings to improve. Used By button in the Sound Event editor.
- Bulk import: a folder of clips becomes Sound Events, grouped by trailing numbers into Random containers and by a
  Switch Group into Switch containers.
- Timeline integration (when the Timeline package is installed): Resonance Track with Sound Event and Game State clips.
- Profiler history: the last 60 seconds, scrubbable, with real and virtual voices and the engine cost.
- Occlusion column in the Profiler.

- Occlusion: three rays between each sound and the listener muffle it behind walls, with volume, low pass, smoothing
  and check interval per event.
- Reverb Zone component: colliders with a reverb preset, intensity and fade distance, blended at the listener into a
  single reverb. Reverb Send per Sound Event.
- Attenuation Presets shared by many events, a custom rolloff curve, and a low pass that closes with distance.
- Delay and Delay Random on Sound Events, Random Start on clips, Shuffle on Random nodes.
- Sound Banks and the Sound Bank Loader component: reference-counted loading and unloading of audio data.
- Tools > Resonance > Optimize Audio Import Settings: suggested load type and preload for every clip in use.
- Profiler counters for clips held by banks, clips loaded on demand and the reverb around the listener.
- The demo has a stone hall with reverb, a wall that occludes the car, a shared attenuation preset and a bank.

### Fixed

- Bus meters and levels in Play Mode stayed at zero when the Studio had not used the preview yet.

### Changed

- Bus meters now measure each voice from a cached RMS envelope of its clip, times its real gain and distance
  attenuation, instead of reading the audio source output. Meters read true dBFS levels.
- Playing and updating sounds allocates no managed memory once warmed up: sound instances are pooled and the event
  log keeps references instead of names. A test checks it with the Unity Test Framework allocation constraint.

### Verified

- The test suite passes in built-in, URP (Unity 6000.3) and HDRP (Unity 6000.5) projects, and the demo renders without
  missing materials in URP and HDRP.
- A Windows player build of the demo runs a headless smoke test: sounds play, the narrator ducks the music and the score
  moves to combat on the next bar. Engine update in the player: about 0.005 ms per frame.
- Player scripts compile for Android and WebGL.

## [0.1.0] - 2026-10-24

First preview.

### Added

- Sound Events with Clip, Random, Sequence (step and continuous), Switch, Blend and Layer nodes, volume and pitch
  randomization, 3D settings, fades, instance limits, priority, cooldown and parameter modulation curves.
- Parameters (global or per emitter, seek speed), Switch Groups and State Groups.
- Buses with hierarchy, ducking rules, session mute and solo, Unity Audio Mixer output, and Mix Snapshots.
- Voice pool with a global voice budget, priority stealing and virtual voices.
- Music Tracks with segments, state links, beat, bar and segment-end transitions on the audio clock, crossfades and
  stingers.
- Components: Sound Emitter, Sound Listener, Parameter Driver, Switch Setter, State Zone, Music Source, Animation
  Sounds and Snapshot Trigger, with icons.
- Resonance Studio window: asset browser with drag and drop of clips, sound tree editor with undo, preview controls,
  waveform, Mixer, Music and Profiler tabs.
- `Resonance` scripting API and `SoundHandle`.
- Resonance Demo sample with procedurally generated audio.

# Changelog

All notable changes to this package are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Fixed

- A play refused for lack of voices no longer steals lower priority sounds first: voices are only stolen when enough
  of them can be freed, and a sound already stopped by its instance limit gives its voice back before any other sound
  is stolen.
- Virtual voices keep their exact position across the clips of a continuous sequence, and advance at the pitch the
  audio source really plays (clamped to 4x), so they resume in the right place.
- Unloading a Sound Bank while one of its clips plays (a sound, a music segment, a layer or a stinger) no longer cuts
  it: the clip's audio data is released when nothing plays it any more, unless the bank is loaded again first.
- A Sound Bank releases exactly the clips it loaded, even if its events were edited while it was loaded.
- The Profiler ignores a truncated or corrupted frame from a remote player instead of throwing in the editor and
  showing half of a frame.
- When the game stalls past the end of a music segment (a long frame, a breakpoint, the editor paused), the next
  segment starts a moment later on a new beat grid instead of being scheduled in the past, which played it late and
  cut it before its end.
- A Snapshot Trigger removes its snapshot when its scene is unloaded; before, the snapshot stayed applied for the
  rest of the game.

### Changed

- The engine update only writes audio source, filter and transform properties that changed, computes each bus gain
  once per frame, and skips occlusion rays for sounds out of range (they are virtual, so they cannot be heard). In
  the demo player, 400 sounds cost 0.15 ms per frame instead of 0.17 ms; 250 occluded sounds on a two-level bus
  hierarchy with a snapshot, half of them out of range, went from about 0.48 ms to 0.24 ms in the editor tests.

## [0.5.1] - 2026-10-24

### Changed

- Loudness is now measured in LUFS as defined by ITU-R BS.1770 (K-weighting, overlapping 400 ms blocks, absolute and
  relative gates). It matches the EBU Tech 3341 reference signals within 0.1 LU and ffmpeg's EBU R128 meter within
  0.2 LU on the demo clips. Clips shorter than 400 ms, which the standard does not measure, use the K-weighted level of
  the whole clip.
- The demo's Master bus leaves 6 dB of headroom: a recording of the demo showed peaks above 0 dBFS when a coin and a
  footstep played together. The narrator lines fade out instead of ending with a click.

### Verified

- In a Windows player: a Timeline played by a PlayableDirector, the demo bank loaded through Addressables, and 400
  sounds at once (0.17 ms per frame on average).
- The player's real mix, recorded from the Audio Listener: no clipping, no unexpected silence.
- The Studio preview plays audio in Edit Mode, and the Scene view draws the Resonance gizmos.

## [0.5.0] - 2026-10-24

### Added

- Music layers: stems that play in sync with a segment, faded by a parameter.
- Beat events: `MusicBeat` and `MusicSegmentStarted` on the engine, and the Music Beat Events component.
- Clip markers with `MarkerReached`, `SoundEnded`, and the Sound Callbacks component. Markers are drawn on the waveform.
- Accessibility: captions and visual indicators on Sound Events, and the Sound Indicators component.
- Volume Setting component for options menus, saved between sessions.
- Debug Overlay component for the Game view.
- Scene view gizmos: attenuation spheres, occlusion line, labeled state zones.
- Loudness analysis (gated RMS) with Balance in the Audit tab; loudness and peak under each clip's waveform.
- Addressable Bank Loader, compiled only when the Addressables package is installed.
- Mobile import suggestions are grouped per clip.

### Fixed

These come from an independent code review; each has a regression test.

- Engine events (subtitles, markers, sound ended) are queued and raised after the engine update, so a handler can play
  a sound without corrupting the engine's state.
- Sounds stolen for the voice budget now raise `SoundEnded`, and a sound that already ended can no longer be stolen.
- `StopAll` and per-emitter limits no longer treat a destroyed GameObject as "every object".
- Cooldowns belong to each engine, so auditioning in the Studio no longer blocks the sound in Play Mode.
- Fade In starts after Delay instead of being spent during it.
- Each dialogue line in a continuous sequence sends its own subtitle; a line that goes virtual and comes back does not
  repeat its subtitle.
- Steal Quietest uses an estimated level in builds, where meters are off.
- Virtual voices no longer duck other buses; music ducks other buses in builds too.
- Stopping the music during a pending transition fades out instead of cutting at the boundary.
- Going back to the current segment before a pending transition cancels it instead of restarting the segment.
- A request right at a transition no longer halts the music that is already playing or leaves a gap.
- Stopping or replaying a track forgets a planned bridge; stingers stop with the music and follow the mix.
- `Resonance.Engine` no longer recreates the runtime while the game quits or leaves Play Mode.
- Binaural voices no longer allocate on play, and meters build clip envelopes a little at a time instead of all at once.
- The Sound Listener inspector no longer uses an API deprecated in Unity 6000.5.

## [0.4.0] - 2026-10-24

### Added

- An inspector for every component, made for people who do not code: menus for switch values, states, tags, Animator
  floats and speakers; warnings in plain words with buttons that fix the problem (add a trigger collider, make a
  collider a trigger, use the object's Animator); test buttons and a live status line in Play Mode.
- Music Track inspector: state links, bridges, "then play" and "start with" are menus of the track's segments and
  states.

### Fixed

- The Reverb Zone inspector no longer throws on objects without a Mesh Collider.

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

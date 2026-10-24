# Changelog

All notable changes to this package are documented here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and the project uses [Semantic Versioning](https://semver.org/).

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

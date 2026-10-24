# Coming from Wwise

| Wwise | Resonance |
| --- | --- |
| Authoring tool, SoundBanks | Resonance Studio inside Unity; assets are used directly, nothing to generate |
| Event, Play action | Sound Event |
| Sound SFX | Clip node |
| Random Container (shuffle, avoid repeat) | Random node (weights, Avoid Last) |
| Sequence Container (step, continuous) | Sequence node (Step, Continuous, Loop) |
| Switch Container | Switch node + Switch Group |
| Blend Container, blend tracks | Blend node + Parameter, per child range and fade width |
| Game Parameter (RTPC) | Parameter (global or per emitter, seek speed) + modulation curves |
| Switch Group | Switch Group |
| State Group | State Group |
| Audio Bus, Master Audio Bus | Bus with parent, optional Unity Audio Mixer output |
| Auto-ducking | Duck Rules on the bus |
| States on buses | Mix Snapshots |
| Playback limit, priority, virtual voices | Max Instances, Priority, Virtualize When Far, voice budget |
| Attenuation ShareSets | Attenuation Presets, custom curve and distance low pass |
| Obstruction and occlusion | Occlusion with three rays, per event |
| Auxiliary busses with reverb, Rooms | Reverb Zones blended at the listener, Reverb Send per event |
| SoundBanks for memory | Sound Banks and Sound Bank Loader, reference counted; Optimize Audio Import Settings |
| Initial delay, random start offset, shuffle | Delay and Delay Random, Random Start, Shuffle |
| Music Switch Container, Music Segment, transitions | Music Track: segments, state links, transition sync, crossfade |
| Stingers | Stingers |
| AkGameObj, AkEvent, AkState, AkSwitch, AkRTPC | Sound Emitter, State Zone, Switch Setter, Parameter Driver |
| Profiler, Voices view, capture log | Profiler tab with a 60 second history you can scrub |
| Integrity report, references | Audit tab, Used By |
| Wwise Timeline track | Resonance Track with Sound Event and Game State clips |

What Resonance does not do in this version: its own DSP and plug-ins (use a bus Output to a Unity Audio Mixer for
effects), sound propagation through portals and diffraction, dialogue and localization pipelines, and memory budgets
per bank.

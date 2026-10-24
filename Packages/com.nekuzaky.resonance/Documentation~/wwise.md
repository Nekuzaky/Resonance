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
| Music Switch Container, Music Segment, transitions | Music Track: segments, state links, transition sync, crossfade |
| Stingers | Stingers |
| AkGameObj, AkEvent, AkState, AkSwitch, AkRTPC | Sound Emitter, State Zone, Switch Setter, Parameter Driver |
| Profiler, Voices view | Profiler tab |

What Resonance does not do in this version: its own DSP and plug-ins (use a bus Output to a Unity Audio Mixer for
effects), spatial audio rooms and portals, dialogue and localization pipelines, and memory budgets per bank.

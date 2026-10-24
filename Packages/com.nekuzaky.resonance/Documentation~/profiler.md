# Profiler

The Profiler tab shows the engine of Play Mode while the game runs, and the preview engine otherwise (the pill in the
top right says which).

## Counters

| Counter | Meaning |
| --- | --- |
| Sounds playing | Event instances alive, including those fading out |
| Real voices | Voices with an audio source playing, against the voice budget |
| Virtual voices | Voices too far to hear: no audio source, no CPU, still tracking their time |
| Audio sources pooled | Audio sources Resonance created; they are reused, never destroyed during play |
| Update cost | Time the engine took last frame, in milliseconds |
| Played / rejected | Totals since the engine started |
| Clips held by banks | Clips whose data a loaded Sound Bank keeps in memory |
| Loaded on demand | Clips that were played before their data was loaded (they may have started late) |
| Reverb around listener | The strongest Reverb Zone at the listener and how much reverb is applied |

## History

The Profiler records the last 60 seconds, ten times a second, while the Studio is open, even when another tab is
shown. The strip under the toolbar draws real voices (teal), virtual voices (amber) and the engine cost (line).
Click or drag on it to stop on that moment: the counters and the Active Sounds table show what was playing then.
Press **Live** to follow the game again, **Clear History** to start over.

## Tables

- **Active Sounds**: event, emitter, bus, voices (or virtual), distance to the listener, occlusion and level.
- **Event Log**: the last plays, with why a play was refused: instance limit, no free voice, cooldown, nothing to
  play (an empty container or a missing clip).
- **Game States** and **Global Parameters**: current values.

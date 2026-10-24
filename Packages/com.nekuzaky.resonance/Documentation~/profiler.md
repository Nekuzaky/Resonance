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

## Tables

- **Active Sounds**: event, emitter, bus, voices (or virtual), distance to the listener and level.
- **Event Log**: the last plays, with why a play was refused: instance limit, no free voice, cooldown, nothing to
  play (an empty container or a missing clip).
- **Game States** and **Global Parameters**: current values.

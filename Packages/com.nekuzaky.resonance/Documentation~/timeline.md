# Timeline

When the Timeline package is installed, Resonance adds a **Resonance Track** to the Timeline window (right-click in the
track list > Resonance > Resonance Track). Without Timeline, this part of Resonance is not compiled and nothing
depends on it.

Bind the track to a GameObject to play sounds from it (in 3D, with its switches and parameters), or leave it empty to
play them without an emitter.

## Clips

| Clip | Does |
| --- | --- |
| Sound Event | Plays a Sound Event when the clip starts. With **Stop At End**, the sound stops when the clip ends (with **Stop Fade**, or the event's own fade out when it is -1); without it, one-shots and tails play to their end |
| Game State | Sets a state when the clip starts: switch the music to a cutscene segment, duck the ambience with a State Zone snapshot. With **Restore At End**, the previous state comes back when the clip ends |

Clips only play while the Timeline plays in Play Mode, not while you scrub in the Timeline window, so moving the
playhead never fires a hundred footsteps at once.

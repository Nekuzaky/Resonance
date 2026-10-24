# Interactive music

A **Music Track** holds the pieces of a score and the rules to move between them in time with the music.

## Settings

| Setting | Meaning |
| --- | --- |
| BPM, Beats Per Bar | The tempo grid every transition snaps to |
| Segments | Named parts of the score, each with a clip, a length in bars (0 uses the clip length) and the segment that follows it (empty loops the segment) |
| First Segment | Where the track starts |
| State Group, Links | Which segment plays for each state |
| Transition Sync | When a change happens: immediately, next beat, next bar, or end of the segment |
| Crossfade | Overlap between the old and the new segment |
| Stingers | Short one-shots layered over the music on the next beat or bar |

Transitions are scheduled on the audio clock (`AudioSettings.dspTime`), so they land exactly on the beat whatever the
frame rate.

## Playing music

- A **Music Source** component plays a track when the scene starts.
- A **State Zone** changes the state when the player enters it; the track moves to the linked segment on the next
  boundary.
- From code: `Resonance.PlayMusic(track)`, `Resonance.SetState(group, "Combat")`, `Resonance.PlayStinger("Alert")`.

## The Music tab

The Music tab plays the track, shows the bar and beat, highlights the current segment (and the one waiting for its
boundary), and has buttons for every state and stinger so you can test transitions without running the game.

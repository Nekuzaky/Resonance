# Sound Events

A Sound Event is what the game plays. It holds a tree of nodes and the settings that apply to every play: bus, volume,
3D behavior, fades and voice limits. Create one with **Create > Resonance > Sound Event**, the **+** button of the
Studio, or by dropping audio clips on the Studio.

## Nodes

| Node | Plays | Typical use |
| --- | --- | --- |
| Clip | One audio file, optionally looped | Any single sound |
| Random | One child, picked at random by weight. **Avoid Last** skips the last N picks | Footsteps, impacts, barks |
| Sequence, Step | The next child each time the event plays | Gun reload steps, a dialogue line per press |
| Sequence, Continuous | Every child, one after the other, optionally looping | Intro then loop, playlists |
| Switch | The child whose **Plays When** value matches the switch on the emitter | Footsteps by surface, weapon by type |
| Blend | Every child at once, each faded in and out by a parameter range | Engine by speed, rain by intensity |
| Layer | Every child at the same time | Explosion = boom + debris + tail |

Nodes nest freely: a Switch of Randoms, a Layer of a Blend and a Random, and so on.

In the Studio tree:

- **Add** puts a new node in the selected container. **Wrap** puts the selected node inside a new container.
- Drag audio clips onto any row to add them.
- Right-click a row for the same actions. Undo works for everything.
- Rows in red are missing something: a clip, a switch group or a parameter.

## Randomization

Every node has **Volume Random** and **Pitch Random**. The value is a range in both directions: a Pitch Random of 1
plays anywhere between one semitone lower and one semitone higher. Values add up through the tree.

## 3D sound

| Setting | Meaning |
| --- | --- |
| Spatialize | Off for UI, music and ambience beds; on for sounds that come from an object |
| 3D Amount | 0 is flat, 1 is fully positioned |
| Full Volume Within / Silent Beyond | Distance range of the attenuation |
| Follow Emitter | Keep moving with the emitter after the sound starts |
| Virtualize When Far | Beyond the silent distance the voice stops using an audio source and CPU, keeps its place in time, and resumes when the listener comes back |
| Stop With Emitter | Stop when the emitter GameObject is destroyed |

## Voices

| Setting | Meaning |
| --- | --- |
| Max Instances | How many times this event can play at once (0 means no limit) |
| Limit Per | The whole game, or each emitter |
| When Full | Steal the oldest, steal the quietest, or refuse the new play |
| Priority | When the global voice budget (64 by default) is full, a play may steal a voice from a sound of equal or lower priority. The oldest of the lowest priority goes first |
| Cooldown | Minimum time between two plays |

## Parameter modulation

A modulation reads a parameter and changes the volume (dB), pitch (semitones), low pass or high pass (Hz) of the whole
event through a curve. On the curve, X goes from the parameter's minimum (0) to its maximum (1).

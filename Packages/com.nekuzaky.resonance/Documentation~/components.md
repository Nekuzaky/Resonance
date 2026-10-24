# Components

Every component is under **Add Component > Resonance**.

Every component has an inspector made for people who do not code:

- **Menus instead of typing.** Switch values, game states, tags, Animator parameters and speakers are picked from a
  list filled from your project. A value that no longer exists shows in red with "(not found)".
- **Warnings that explain and fix.** A trigger without a collider, a solid collider where a trigger is needed, an
  empty field or a state that is not in its group: the inspector says what is wrong in plain words, and a button fixes
  it when it can ("Add a trigger collider", "Make it a trigger", "Use the Animator on this object").
- **Test buttons.** Play and Stop on emitters and music sources, Apply on switches and snapshots, and a live line in
  Play Mode that shows what the component is doing.

Music Tracks get the same treatment: state links, bridges, "then play" and "start with" are menus of the track's own
segments and states.

## Sound Emitter

Plays a Sound Event from its GameObject.

| Field | Meaning |
| --- | --- |
| Sound | The Sound Event |
| Play On | Start, Enable, Disable, Destroy, Trigger Enter or Exit, Collision Enter or Exit, Mouse Down (several at once) |
| Stop On | Same list. Disable is the default, so looping sounds stop with their object |
| Stop Fade | Fade out time when stopping; -1 uses the event's own Fade Out |
| Trigger Tag | Only react to triggers and collisions with this tag (empty reacts to everything) |
| Impact Parameter | Receives the collision speed, so harder hits can sound louder |

`Play()`, `Stop()` and `PlayEvent(SoundEvent)` can be called from UI buttons and Unity Events.

## Sound Listener

Marks where the player hears from. Put it on the camera or the player. Without it, Resonance uses the Audio Listener.

## Parameter Driver

Sets a parameter every frame from this GameObject: speed, vertical speed, distance to the listener, height, an
Animator float, or a constant. Multiplier and Offset scale the value.

## Switch Setter

Sets a switch on this GameObject (or globally) when the scene starts. `SetValue(string)` changes it later, for
example from a trigger.

## State Zone

A trigger collider that sets a state while the player is inside (and restores the previous one on exit), and can
apply a mix snapshot at the same time.

## Music Source

Plays a Music Track when the scene starts and stops it when the object is disabled. `PlaySegment` and `PlayStinger`
can be called from Unity Events.

## Animation Sounds

Plays sounds from Animation Events: add an event that calls `PlaySound` and drop a Sound Event in its Object field,
or call `PlaySoundNamed` with a name from the list.

## Reverb Zone

Turns a collider into a reverberant space with a preset (room, corridor, cave...), an intensity and a fade distance.
See [Distance, occlusion and reverb](space.md).

## Sound Bank Loader

Loads a Sound Bank while the object is enabled and unloads it when it is disabled. See
[Memory and loading](memory.md).

## Subtitle Relay

Forwards the subtitles of Dialogue nodes to Unity Events, so a text field shows them without code. See
[Dialogue and subtitles](dialogue.md).

## Snapshot Trigger

Applies a mix snapshot while the object is enabled, or when `Apply()` and `Remove()` are called.

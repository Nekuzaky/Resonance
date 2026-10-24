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

In the Scene view, a selected Sound Emitter shows its attenuation as two spheres (full volume inside the inner one,
silent beyond the outer one) and its name; in Play Mode a line to the listener turns from green to red as the sound
gets occluded. State Zones are drawn as blue volumes labeled with the state they set.

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

## Volume Setting

Lets the player set a bus volume (Music, SFX, Voice) from your options menu, and remembers it between sessions.
Connect a UI Slider (0 to 1) to **SetVolume**, and **On Loaded** back to the slider so it shows the saved value. The
slider is mapped to decibels so that half way sounds about half as loud.

## Music Beat Events

Calls Unity Events on every beat (or every N beats), every bar and every new segment of the music: pulse lights,
spawn enemies in time, flash the UI on the downbeat.

## Sound Callbacks

Calls Unity Events when a sound playing on this object reaches a **marker** (see [Sound Events](sound-events.md)) or
ends: open the door when the creak reaches "unlatch", spawn the impact effect on the "hit" marker of a swing.

## Sound Indicators

Shows captions and arrows for sounds, for players who are deaf or hard of hearing. See
[Accessibility](accessibility.md).

## Debug Overlay

Draws every playing 3D sound in the Game view with its name, distance, occlusion and level, and a summary line. By
default it only shows in the editor and in development builds.

## Addressable Bank Loader

When the Addressables package is installed: loads a Sound Bank through an Addressable reference, then its audio, and
releases both when the object is disabled. See [Memory and loading](memory.md).

## Snapshot Trigger

Applies a mix snapshot while the object is enabled, or when `Apply()` and `Remove()` are called.

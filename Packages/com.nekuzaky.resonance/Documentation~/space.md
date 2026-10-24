# Distance, occlusion and reverb

## Attenuation

Every Sound Event has distance settings in its **3D Sound** group:

| Setting | Meaning |
| --- | --- |
| Full Volume Within / Silent Beyond | Distance range of the attenuation |
| Rolloff | Logarithmic (natural), Linear, or Custom |
| Custom Curve | With Custom rolloff: volume (Y, 0 to 1) by distance (X, from 0 to Silent Beyond) |
| Low Pass At Max | Far sounds lose their highs. At Silent Beyond the low pass reaches this frequency; 22000 Hz turns it off |
| Spread, Doppler | Same as Unity's audio source |

An **Attenuation Preset** (Create > Resonance > Attenuation Preset) holds the same settings once, for many events:
small objects, footsteps, explosions, voices. Set it in the event's **Attenuation Preset** field and the event's own
distance settings are ignored. Change the preset and every event that uses it follows.

## Occlusion

Turn on **Occlusion** in an event and Resonance casts three rays between the sound and the listener, a little apart
(**Ray Spread**). Each ray blocked by a collider on the **Blocking Layers** muffles the sound by a third: a sound half
hidden behind a corner is half occluded.

| Setting | Meaning |
| --- | --- |
| Occluded Volume | Volume change when fully occluded |
| Occluded Low Pass | Low pass frequency when fully occluded |
| Smoothing | Time to move between open and occluded |
| Check Every | How often the rays are cast; occlusion is cheap, but there is no need to cast every frame |

Trigger colliders, the emitter's own colliders and the listener's colliders never occlude. The listener is the object
with a **Sound Listener** component, or the Audio Listener.

## Reverb zones

A **Reverb Zone** component turns a collider into a space with its own reverb: a room, a corridor, a cave, a hangar.
Pick a preset, an **Intensity**, and a **Fade Distance**: outside the collider the reverb fades out over that distance,
so walking into a room is smooth. Where zones overlap, their presets blend by weight.

Resonance drives a single reverb at the listener's position, so the cost does not grow with the number of zones or
sounds. Each Sound Event chooses how much of it goes into the reverb with **Reverb Send** (0 for UI and music).

Use box, sphere, capsule or convex mesh colliders: the zone measures the distance to the collider with
`Collider.ClosestPoint`, which does not work with concave meshes.

## Profiler

The Profiler shows the reverb around the listener (preset and amount) and, for each active sound, how occluded it is.

# Getting started

## Install

In Unity, open **Window > Package Manager**, click **+**, choose **Install package from git URL** and paste:

```
https://github.com/Nekuzaky/Resonance.git?path=Packages/com.nekuzaky.resonance
```

To pin a version, add `#v0.1.0` at the end of the URL.

## Open the Studio

**Tools > Resonance > Resonance Studio** (or `Ctrl+Alt+R`). The Studio has four tabs:

- **Events**: every Resonance asset in the project, and the editor for the selected one
- **Mixer**: one strip per bus, with faders, meters, mute and solo, and the mix snapshots
- **Music**: transport, segments, game states and stingers of a Music Track
- **Profiler**: what is playing right now, the event log, states and parameters

## Make a sound in one minute

1. Click **Create Default Mix** in the Studio. It creates Master, Music, SFX, Voice and Ambience buses, with the voice
   bus already ducking music and SFX.
2. Select a few audio clips in the Project window (for example four footstep variations) and drag them onto the asset
   list on the left of the Studio. Resonance creates a Sound Event that picks one of them at random and never plays
   the same one twice in a row.
3. Press **Play** in the Studio (or the space bar) to hear it. Set its **Bus** to SFX and give it some
   **Volume Random** and **Pitch Random** so every step sounds a little different.
4. Add a **Sound Emitter** component to a GameObject, drop the Sound Event in its **Sound** field and choose when it
   plays: on Start, on a trigger, on a collision, on click.

That is it: press Play in Unity and the sound plays from that object, in 3D, on the SFX bus.

## Next steps

- Use a **Switch** container to pick footsteps by surface, and a **Switch Setter** component to set the surface.
- Use a **Blend** container and a **Parameter Driver** component to crossfade an engine sound with the car's speed.
- Put your score in a **Music Track** and let a **State Zone** switch it to combat music on the next bar.

The demo sample (Package Manager > Resonance > Samples) shows all of these in one scene.

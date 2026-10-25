# Mixing

## Buses

A **Bus** groups sounds. Its volume applies to every sound on it and to every bus under it (**Parent**). A typical
mix is Master with Music, SFX, Voice and Ambience below it. **Tools > Resonance > Create Default Mix** makes that
layout for you.

The **Mixer** tab shows one strip per bus with a fader (it edits the asset), a level meter, and mute and solo buttons
that only last for the session, so you can listen without changing the mix.

To send a bus through Unity's Audio Mixer (for effects Resonance does not have), set its **Output** to an Audio Mixer
group. Child buses inherit it. A bus can use its own effects and an Audio Mixer group together: the effects run
first.

## Effects

Every bus has an ordered effect chain. In the **Mixer** tab, click the funnel button of a bus (or the effect names
under its fader), then **Add Effect**. Each effect has an **On** switch (bypass without losing the settings), up and
down arrows to reorder it, and a remove button. The chain is also editable in the bus inspector.

| Effect | Settings | Notes |
| --- | --- | --- |
| Low Pass | Cutoff (Hz), Resonance | 12 dB per octave. Resonance 1 is a flat (Butterworth) response. |
| High Pass | Cutoff (Hz), Resonance | 12 dB per octave. |
| Reverb | Routing, Mix, Decay (s), HF Ratio, Pre-delay (s) | **Send** keeps the dry sound and adds the reverb at Mix. **Insert** crossfades dry to wet. |
| Delay | Time (ms), Feedback, Wet, Dry | Changing Time restarts the echo, so it is not meant to be swept by a parameter. |
| Distortion | Drive, Output (dB), Mix | Soft clipping with up to 36 dB of drive. |
| Compressor | Threshold (dB), Ratio, Attack (s), Release (s), Makeup (dB) | Ratio 20 behaves as a limiter (see below). |

The chain of a bus applies to every sound on it and on the buses under it, and to music tracks that use it. A child
bus's chain runs before its parent's, like the signal flowing up the hierarchy.

**Order.** Low Pass, High Pass and Distortion run in Resonance's own DSP, exactly in chain order (up to 8 of them per
sound, counting the buses above). Delay and Reverb use Unity's echo and reverb filters and always come after them; a
sound gets one delay and one reverb, so when several buses on its path have one, the one closest to the sound wins.
Filters, delays and reverbs are linear, so their relative order does not change the result; only a distortion placed
after a delay or reverb is not reproduced (it is applied before them).

**Tails.** When a sound ends or is stopped, its audio source keeps running on silence until the delay repeats and the
reverb have died out (up to 10 seconds), so tails are never cut. Up to 32 voices ring at once; when the voice budget
is full, the oldest tail is cut first.

**Distortion level.** The drive is measured against the level of the sound when it reaches the bus: bus faders,
ducking and snapshots do not change how hard a sound distorts, but the sound's own volume and fades do.

### Driving effects with parameters

Under each effect, **Driven By Parameters** adds curves exactly like the volume and filter curves of a Sound Event:
the curve maps the parameter, from its minimum (0) to its maximum (1), to the value of one setting. **Use The Full
Range** resets the curve to a line across the whole range of the setting. Buses are shared by many sounds, so they
use the parameter's global value. The Mixer tab shows the value now, after parameters and snapshots, next to each
setting.

### Effects in snapshots

A **Mix Snapshot** has an **Effects** list next to its bus offsets: pick a bus, one of its effects, a setting and the
value it should reach. The setting fades towards that value with the snapshot's fade in and back with its fade out
(cutoffs fade on a musical, logarithmic scale). A typical underwater snapshot fades a Low Pass on the SFX bus from
22000 Hz to 800 Hz and a Reverb Mix up to 0.6. When several active snapshots change the same setting, they apply in
the order they were applied.

### Compressor

The compressor follows the level of everything on the bus and on the buses under it, before the bus fader, and turns
the bus down above the threshold. It works on the mix of the bus (not sound by sound) but at the frame rate: the
level comes from each clip's envelope (RMS over 1024 samples) and the gain changes once per frame. It is a leveler,
good for dialogue, music under gameplay or keeping a busy SFX bus in check. It cannot catch single-sample peaks, so it
is not a brickwall limiter; for that, add a limiter to an Audio Mixer group. Clips whose samples Unity cannot read
(Streaming, or Compressed In Memory) are measured as a steady -12 dBFS. The Mixer tab shows the gain reduction under
the bus fader.

### Cost and platforms

Effects only cost something on buses that have them: a bus without effects runs nothing. Each sound runs its own
filters and its own reverb, so 30 sounds on a reverb bus run 30 reverbs; for one shared room sound, prefer Reverb
Zones. Everything is allocation free once warmed up and works the same in the Edit Mode preview and in Play Mode.

On WebGL, Unity's web audio has no audio filters and no `OnAudioFilterRead`: Low Pass, High Pass, Distortion, Delay and
Reverb do nothing there (the filters of Sound Events and occlusion do nothing either). The compressor still works,
because it only changes volumes. Android, iOS and desktop players run every effect.

## Ducking

A bus can lower other buses while it plays. Each **Duck Rule** has a target bus, an amount in dB, and attack and
release times. The default mix makes the Voice bus lower Music by 9 dB and SFX by 4 dB while someone speaks. When
several rules duck the same bus, the strongest one wins.

## Snapshots

A **Mix Snapshot** offsets several buses at once, with fade in and fade out times: pause menu, underwater, low health,
cutscene. Apply them with a **Snapshot Trigger**, a **State Zone**, the Mixer tab, or `Resonance.ApplySnapshot` and
`Resonance.RemoveSnapshot`. Several snapshots can be active together; their offsets add up. Snapshots can also fade
effect settings (see [Effects in snapshots](#effects-in-snapshots)).

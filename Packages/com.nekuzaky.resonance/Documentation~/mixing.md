# Mixing

## Buses

A **Bus** groups sounds. Its volume applies to every sound on it and to every bus under it (**Parent**). A typical
mix is Master with Music, SFX, Voice and Ambience below it. **Tools > Resonance > Create Default Mix** makes that
layout for you.

The **Mixer** tab shows one strip per bus with a fader (it edits the asset), a level meter, and mute and solo buttons
that only last for the session, so you can listen without changing the mix.

To send a bus through Unity's Audio Mixer (for reverb, compression or effects), set its **Output** to an Audio Mixer
group. Child buses inherit it.

## Ducking

A bus can lower other buses while it plays. Each **Duck Rule** has a target bus, an amount in dB, and attack and
release times. The default mix makes the Voice bus lower Music by 9 dB and SFX by 4 dB while someone speaks. When
several rules duck the same bus, the strongest one wins.

## Snapshots

A **Mix Snapshot** offsets several buses at once, with fade in and fade out times: pause menu, underwater, low health,
cutscene. Apply them with a **Snapshot Trigger**, a **State Zone**, the Mixer tab, or `Resonance.ApplySnapshot` and
`Resonance.RemoveSnapshot`. Several snapshots can be active together; their offsets add up.

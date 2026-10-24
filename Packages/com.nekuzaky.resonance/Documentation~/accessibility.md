# Accessibility

Players who are deaf or hard of hearing miss the information that sound carries: someone behind them, a door
opening, an engine coming closer. Resonance can show that information on screen, from the same Sound Events, with no
extra code.

## Captions and indicators

In the Studio, select a Sound Event and fill its **Accessibility** group:

| Setting | Meaning |
| --- | --- |
| Caption | A short description: "Footsteps", "Gunfire", "Door opens", "Narrator speaking" |
| Visual Indicator | Show this sound on screen while it plays |

Then add a **Sound Indicators** component (to the camera, for example). While the game runs:

- **3D sounds** get an arrow around the center of the screen that points toward them, with their caption. The arrow
  fades with the sound's loudness at the listener, so a far or occluded sound is faint.
- **2D sounds** (and 3D sounds playing on the listener itself, such as the player's own footsteps) show their caption
  at the top of the screen, like closed captions.

| Setting | Meaning |
| --- | --- |
| Camera | The camera the arrows are relative to; empty uses the main camera |
| Radius | Distance of the arrows from the screen center, in pixels |
| Hide Below | Sounds quieter than this at the listener are not shown |
| Show Captions, Show 2D Sounds | Turn captions and 2D sounds on or off |
| Color, Text Size | Look of the indicators |

Expose these settings in your options menu: a player who needs them can turn them on, everyone else never sees them.

## Subtitles

Dialogue lines have their own subtitles; see [Dialogue and subtitles](dialogue.md).

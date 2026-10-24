# Game syncs

Game syncs are how the game tells the audio what is happening.

## Parameters

A **Parameter** is a number with a minimum, a maximum and a default: speed, health, distance, rain intensity.

- **Scope Global**: one value for the whole game.
- **Scope Per Emitter**: each GameObject has its own value (each car has its own speed). A value set without an
  emitter is used by every emitter that has none of its own.
- **Seek Speed**: units per second the value moves toward its target. 0 jumps immediately. Use it to smooth values
  that change abruptly.

Sounds use parameters in **Blend** containers and in **Parameter Modulation** curves. Set values with a
**Parameter Driver** component or `Resonance.SetParameter`.

## Switches

A **Switch Group** is a list of values, such as Grass, Stone, Wood, Metal. Each emitter has its own current value.
A **Switch** container plays the child that matches. Set values with a **Switch Setter** component or
`Resonance.SetSwitch`.

## States

A **State Group** is a global value, such as Explore, Combat, Menu, Dead. Music Tracks follow a state group to change
segment, and **State Zones** change states when the player walks in and out. Set states with
`Resonance.SetState`.

## Testing

Select a parameter, switch group or state group in the Studio: the **Live** section at the top changes it now, in Play
Mode or in the preview. The Sound Event editor also has **Preview Controls** for every parameter and switch the event
uses.

# Scripting

Everything goes through the static `Resonance` class in the `Nekuzaky.Resonance` namespace.

```csharp
using Nekuzaky.Resonance;
using UnityEngine;

public sealed class Weapon : MonoBehaviour
{
    [SerializeField] private SoundEvent shot;
    [SerializeField] private SoundEvent reloadLoop;
    [SerializeField] private SwitchGroup weaponType;

    private SoundHandle _reload;

    private void Start() => Resonance.SetSwitch(weaponType, "Rifle", gameObject);

    public void Fire() => Resonance.Play(shot, gameObject);

    public void BeginReload() => _reload = Resonance.Play(reloadLoop, gameObject);

    public void EndReload() => _reload.Stop(fade: 0.1f);
}
```

## Playing

| Call | Does |
| --- | --- |
| `Resonance.Play(sound, gameObject)` | Plays from a GameObject (3D, follows it, uses its switches and parameters) |
| `Resonance.PlayAt(sound, position)` | Plays at a fixed point |
| `handle.Stop(fade)` / `Resonance.Stop(handle, fade)` | Stops one play; a negative fade uses the event's Fade Out |
| `Resonance.Stop(sound)` | Stops every play of an event |
| `Resonance.StopAll(gameObject)` | Stops everything on an emitter, or everything when null |
| `handle.IsPlaying` | Whether that play is still going |

`Play` returns `SoundHandle.None` when the play was refused (instance limit, cooldown, no free voice). The reason is in
the Profiler's event log.

## Game syncs

```csharp
Resonance.SetParameter(speed, rigidbody.linearVelocity.magnitude, gameObject);
Resonance.SetSwitch(surface, "Metal", gameObject);
Resonance.SetState(gameState, "Combat");
```

## Music and mix

```csharp
Resonance.PlayMusic(score);
Resonance.PlayMusicSegment("Boss", MusicSync.NextBar);
Resonance.PlayStinger("Victory");
Resonance.ApplySnapshot(pauseMenu);
Resonance.SetBusVolume(musicBus, -6f);
```

`SetBusVolume` is a runtime offset in dB on top of the bus asset, made for options menus.

## Engine

`Resonance.Engine` is the running engine (created on first use in Play Mode, kept across scenes). `Resonance.Stats`
gives the live counts the Profiler shows. `Engine.MaxVoices` sets the voice budget.

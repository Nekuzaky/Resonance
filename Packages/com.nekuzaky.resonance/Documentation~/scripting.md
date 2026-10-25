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

## Reacting to sounds

`Resonance.Engine.SoundStarted` is raised once for every play the engine admits, right after the sound exists, so its
handle is already valid. It is not raised when `Play` returns `SoundHandle.None`. It is raised in Play Mode and in the
Studio preview, like the engine's other events, and `Resonance.Engine.SoundEnded` is its counterpart.

```csharp
private ResonanceEngine _engine;

private void OnEnable()
{
    _engine = Resonance.Engine;
    if (_engine != null)
        _engine.SoundStarted += OnSoundStarted;
}

private void OnDisable()
{
    if (_engine != null)
        _engine.SoundStarted -= OnSoundStarted;
    _engine = null;
}

private void OnSoundStarted(SoundStartInfo sound)
{
    if (sound.MaxDistance > 0f && Vector3.Distance(sound.Position, transform.position) < sound.MaxDistance)
        Debug.Log($"{sound.Event.name} is within range");
}
```

| Field | Value |
| --- | --- |
| `Handle` | The play, valid for `IsPlaying` and `Stop` |
| `Event` | The Sound Event that was played |
| `Emitter` | The GameObject passed to `Play`, or null for `PlayAt` |
| `Position` | The emitter's position when the play started, or the position given to `PlayAt`. 2D sounds report the same position, never the listener's |
| `Volume` | Linear start volume: the event Volume with its parameter modulations, times the loudest starting clip (node volume and blend weight). Buses, snapshots, fades and distance are not included |
| `MaxDistance` | The event's attenuation Max Distance (from its Attenuation preset when set), or 0 when Spatialize is off or the Spatial Blend is 0 |

`SoundStartInfo` is a struct, so raising the event allocates nothing, with or without subscribers. The engine is the
same object for the whole Play Mode session, so subscribe once (as above) rather than every frame.

## Game syncs

```csharp
Resonance.SetParameter(speed, rigidbody.linearVelocity.magnitude, gameObject);
Resonance.SetSwitch(surface, "Metal", gameObject);
Resonance.SetState(gameState, "Combat");
```

## Dialogue

```csharp
Resonance.Language = "fr";
Resonance.Engine.SubtitleStarted += subtitle => subtitles.text = subtitle.Text;
Resonance.Engine.SubtitleEnded += _ => subtitles.text = string.Empty;
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

Bus effects are data on the bus asset, so a script can build or change a chain at runtime:

```csharp
var lowPass = sfxBus.AddEffect(BusEffectType.LowPass);
lowPass.SetValue(BusEffects.FilterCutoff, 800f);
lowPass.bypass = false;
```

## Engine

`Resonance.Engine` is the running engine (created on first use in Play Mode, kept across scenes). `Resonance.Stats`
gives the live counts the Profiler shows. `Engine.MaxVoices` sets the voice budget.

# Memory and loading

Unity loads an audio clip's data when the clip is first played, unless its import settings say to preload it. On a
small game that is fine. On a larger one, loading every clip at start wastes memory, and loading on first play can
make a sound start late. Resonance gives you two tools.

## Sound Banks

A **Sound Bank** (Create > Resonance > Sound Bank) lists Sound Events and Music Tracks that belong together: a level,
a vehicle, a boss fight. Loading a bank loads the audio data of every clip they use; unloading it frees them.

- Add a **Sound Bank Loader** component to a GameObject in the level: the bank loads when the object is enabled and
  unloads when it is disabled.
- From code: `Resonance.LoadBank(bank)`, `Resonance.UnloadBank(bank)`, `Resonance.IsBankReady(bank)`.

Banks are reference counted, at the bank and at the clip level: two banks that share a clip keep it loaded until both
are unloaded, and loading the same bank twice needs two unloads.

The Profiler shows how many clips banks hold, and how many clips were **loaded on demand**: played while their data
was not loaded. Each of those may have started late. Add their events to a bank that is loaded before they play.

## Addressables

With the Addressables package installed, the **Addressable Bank Loader** component loads a Sound Bank through an
Addressable reference, so the bank and its clips can live in remote or downloadable content. Without Addressables this
part of Resonance is not compiled.

## Import settings

**Tools > Resonance > Optimize Audio Import Settings** checks every clip used by a Sound Event or a Music Track and
suggests:

| Clip | Load type | Why |
| --- | --- | --- |
| Music Track segments and stingers | Compressed In Memory | Streaming adds start latency, which would break transitions on the beat |
| Longer than 15 s | Streaming | Long ambiences and dialogue are read from disk instead of filling memory |
| 3 to 15 s | Compressed In Memory | Decompressed while playing, small in memory |
| Up to 3 s | Decompress On Load | Footsteps, impacts and UI start instantly |

Clips used by a Sound Bank are set not to preload, so the bank decides when they load. The tool lists the changes and
asks before applying them.

For **Android** and **iOS**, the Audit tab also suggests a platform override for every clip that has none: Vorbis at
35% quality for effects and 50% for music, with the sample rate optimized. Phones get smaller builds and less memory
use, desktop keeps the full quality.

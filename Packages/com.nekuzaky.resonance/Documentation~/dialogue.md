# Dialogue and subtitles

## Dialogue nodes

A **Dialogue** node holds one line of dialogue in every language you recorded. Each line has a language code, an
audio clip and a subtitle. When the event plays, Resonance picks the line in the current language; when there is none,
it plays the **Fallback Language** line, and when that is missing too, the first line with a clip.

Dialogue nodes go anywhere in a sound tree: a Random of several dialogue nodes picks one bark among several, each in
the player's language. Set the **Speaker** to show a name before the subtitle and to filter subtitles by character.

The current language is global:

```csharp
Resonance.Language = "fr";
```

The Preview Controls of a Sound Event with dialogue have a Language menu to hear every version.

## Subtitles

A dialogue line raises two events on the engine: `SubtitleStarted` when its clip starts and `SubtitleEnded` when it
ends or is stopped. Each carries the text, the speaker, the language, the emitter, the duration and the handle of the
sound.

```csharp
Resonance.Engine.SubtitleStarted += subtitle => label.text = subtitle.Text;
Resonance.Engine.SubtitleEnded += _ => label.text = string.Empty;
```

Without code, add a **Subtitle Relay** component and connect its **On Subtitle** event to a text field (for example
a TextMeshPro text's `text` property) and **On Subtitle End** to clearing it. **Speaker Filter** limits it to one
character, and **Include Speaker Name** prefixes the text with the speaker.

## Memory

Dialogue clips are part of the event: Sound Banks load every language of the lines they contain. For large dialogue
sets, put each language in its own bank, or let long lines stream (see [Memory and loading](memory.md)).

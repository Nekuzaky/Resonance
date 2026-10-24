using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekuzaky.Resonance
{
    [Serializable]
    public sealed class DialogueLine
    {
        public string language = "en";
        public AudioClip clip;
        [TextArea(1, 4)] public string subtitle = string.Empty;
    }

    [Serializable]
    public sealed class DialogueNode : SoundNode
    {
        #region Public

        public string speaker = string.Empty;
        public string fallbackLanguage = "en";
        public List<DialogueLine> lines = new();

        public override NodeKind Kind => NodeKind.Dialogue;

        public DialogueLine LineFor(string language)
        {
            DialogueLine fallback = null;
            DialogueLine first = null;
            foreach (var line in lines)
            {
                if (line == null || line.clip == null)
                    continue;
                first ??= line;
                if (string.Equals(line.language, language, StringComparison.OrdinalIgnoreCase))
                    return line;
                if (fallback == null && string.Equals(line.language, fallbackLanguage, StringComparison.OrdinalIgnoreCase))
                    fallback = line;
            }
            return fallback ?? first;
        }

        #endregion


        #region Private and Protected

        protected override SoundNode Copy()
        {
            var copy = (DialogueNode)base.Copy();
            copy.lines = new List<DialogueLine>(lines.Count);
            foreach (var line in lines)
                copy.lines.Add(line == null ? null : new DialogueLine { language = line.language, clip = line.clip, subtitle = line.subtitle });
            return copy;
        }

        #endregion
    }

    public readonly struct Subtitle
    {
        public readonly SoundHandle Handle;
        public readonly string Text;
        public readonly string Speaker;
        public readonly string Language;
        public readonly GameObject Emitter;
        public readonly float Duration;

        public Subtitle(SoundHandle handle, string text, string speaker, string language, GameObject emitter, float duration)
        {
            Handle = handle;
            Text = text;
            Speaker = speaker;
            Language = language;
            Emitter = emitter;
            Duration = duration;
        }
    }
}

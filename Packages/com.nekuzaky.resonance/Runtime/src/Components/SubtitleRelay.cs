using System;
using UnityEngine;
using UnityEngine.Events;

namespace Nekuzaky.Resonance
{
    [Serializable]
    public sealed class SubtitleTextEvent : UnityEvent<string>
    {
    }

    [AddComponentMenu("Resonance/Subtitle Relay")]
    [HelpURL("https://github.com/Nekuzaky/Resonance/blob/main/Packages/com.nekuzaky.resonance/Documentation~/dialogue.md")]
    public sealed class SubtitleRelay : MonoBehaviour
    {
        #region Public

        public string speakerFilter = string.Empty;
        public bool includeSpeakerName = true;
        public SubtitleTextEvent onSubtitle = new();
        public UnityEvent onSubtitleEnd = new();

        public string Current { get; private set; } = string.Empty;

        #endregion


        #region Private and Protected

        private ResonanceEngine _engine;
        private SoundHandle _shown;

        private bool Accepts(Subtitle subtitle) =>
            string.IsNullOrEmpty(speakerFilter) || string.Equals(subtitle.Speaker, speakerFilter, StringComparison.OrdinalIgnoreCase);

        private void OnStarted(Subtitle subtitle)
        {
            if (!Accepts(subtitle) || string.IsNullOrEmpty(subtitle.Text))
                return;
            _shown = subtitle.Handle;
            Current = includeSpeakerName && !string.IsNullOrEmpty(subtitle.Speaker) ? $"{subtitle.Speaker}: {subtitle.Text}" : subtitle.Text;
            onSubtitle.Invoke(Current);
        }

        private void OnEnded(Subtitle subtitle)
        {
            if (!subtitle.Handle.Equals(_shown))
                return;
            _shown = SoundHandle.None;
            Current = string.Empty;
            onSubtitleEnd.Invoke();
        }

        #endregion


        #region Unity Callbacks

        private void OnEnable()
        {
            _engine = Resonance.Engine;
            if (_engine == null)
                return;
            _engine.SubtitleStarted += OnStarted;
            _engine.SubtitleEnded += OnEnded;
        }

        private void OnDisable()
        {
            if (_engine == null)
                return;
            _engine.SubtitleStarted -= OnStarted;
            _engine.SubtitleEnded -= OnEnded;
            _engine = null;
        }

        #endregion
    }
}

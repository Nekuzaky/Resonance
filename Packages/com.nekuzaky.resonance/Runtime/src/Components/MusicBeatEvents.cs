using System;
using UnityEngine;
using UnityEngine.Events;

namespace Nekuzaky.Resonance
{
    [Serializable]
    public sealed class MusicCountEvent : UnityEvent<int>
    {
    }

    [Serializable]
    public sealed class MusicSegmentEvent : UnityEvent<string>
    {
    }

    [AddComponentMenu("Resonance/Music Beat Events")]
    [HelpURL("https://github.com/Nekuzaky/Resonance/blob/main/Packages/com.nekuzaky.resonance/Documentation~/music.md")]
    public sealed class MusicBeatEvents : MonoBehaviour
    {
        #region Public

        [Min(1)] public int everyBeats = 1;
        public MusicCountEvent onBeat = new();
        public MusicCountEvent onBar = new();
        public MusicSegmentEvent onSegment = new();

        public int Bar { get; private set; }

        public int Beat { get; private set; }

        #endregion


        #region Private and Protected

        private ResonanceEngine _engine;
        private int _count;

        private void OnBeat(int bar, int beat)
        {
            Bar = bar;
            Beat = beat;
            if (beat == 1)
                onBar.Invoke(bar);
            if (_count++ % Mathf.Max(1, everyBeats) == 0)
                onBeat.Invoke(beat);
        }

        private void OnSegment(MusicSegment segment)
        {
            _count = 0;
            onSegment.Invoke(segment != null ? segment.name : string.Empty);
        }

        #endregion


        #region Unity Callbacks

        private void OnEnable()
        {
            _engine = Resonance.Engine;
            if (_engine == null)
                return;
            _engine.MusicBeat += OnBeat;
            _engine.MusicSegmentStarted += OnSegment;
        }

        private void OnDisable()
        {
            if (_engine == null)
                return;
            _engine.MusicBeat -= OnBeat;
            _engine.MusicSegmentStarted -= OnSegment;
            _engine = null;
        }

        #endregion
    }
}

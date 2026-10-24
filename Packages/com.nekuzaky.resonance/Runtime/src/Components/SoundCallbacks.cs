using UnityEngine;
using UnityEngine.Events;

namespace Nekuzaky.Resonance
{
    [AddComponentMenu("Resonance/Sound Callbacks")]
    [HelpURL("https://github.com/Nekuzaky/Resonance/blob/main/Packages/com.nekuzaky.resonance/Documentation~/components.md")]
    public sealed class SoundCallbacks : MonoBehaviour
    {
        #region Public

        public string markerFilter = string.Empty;
        public SubtitleTextEvent onMarker = new();
        public UnityEvent onSoundEnded = new();

        #endregion


        #region Private and Protected

        private ResonanceEngine _engine;

        private void OnMarker(SoundMarker marker)
        {
            if (marker.Emitter != gameObject)
                return;
            if (!string.IsNullOrEmpty(markerFilter) && marker.Name != markerFilter)
                return;
            onMarker.Invoke(marker.Name);
        }

        private void OnEnded(SoundHandle handle, GameObject emitter)
        {
            if (emitter == gameObject)
                onSoundEnded.Invoke();
        }

        #endregion


        #region Unity Callbacks

        private void OnEnable()
        {
            _engine = Resonance.Engine;
            if (_engine == null)
                return;
            _engine.MarkerReached += OnMarker;
            _engine.SoundEnded += OnEnded;
        }

        private void OnDisable()
        {
            if (_engine == null)
                return;
            _engine.MarkerReached -= OnMarker;
            _engine.SoundEnded -= OnEnded;
            _engine = null;
        }

        #endregion
    }
}

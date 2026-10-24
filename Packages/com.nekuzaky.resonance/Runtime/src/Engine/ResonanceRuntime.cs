using System;
using UnityEngine;

namespace Nekuzaky.Resonance
{
    [AddComponentMenu("")]
    [DefaultExecutionOrder(10000)]
    internal sealed class ResonanceRuntime : MonoBehaviour
    {
        #region Public

        public ResonanceEngine Engine { get; private set; }

        public static ResonanceRuntime Ensure()
        {
            if (_instance != null)
                return _instance;
            var host = new GameObject("Resonance") { hideFlags = HideFlags.NotEditable };
            DontDestroyOnLoad(host);
            _instance = host.AddComponent<ResonanceRuntime>();
            return _instance;
        }

        #endregion


        #region Private and Protected

        private static ResonanceRuntime _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _instance = null;

        #endregion


        #region Unity Callbacks

        private void Awake() => Engine = new ResonanceEngine(transform);

        private void LateUpdate() => Engine.Tick(Time.unscaledDeltaTime);

        private void OnDestroy()
        {
            Engine?.Shutdown();
            if (_instance == this)
                _instance = null;
        }

        #endregion
    }
}

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
            if (_closed)
                return null;
            var host = new GameObject("Resonance") { hideFlags = HideFlags.NotEditable };
            DontDestroyOnLoad(host);
            _instance = host.AddComponent<ResonanceRuntime>();
            return _instance;
        }

        #endregion


        #region Private and Protected

        private static ResonanceRuntime _instance;
        private static bool _closed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
            _closed = false;
        }

        #endregion


        #region Unity Callbacks

        private void Awake() => Engine = new ResonanceEngine(transform) { Metering = Application.isEditor };

        private readonly RemoteSender _remote = new();

        private void LateUpdate()
        {
            Engine.Tick(Time.unscaledDeltaTime);
            _remote.Tick(Engine);
        }

        private void OnDestroy()
        {
            Engine?.Shutdown();
            if (_instance != this)
                return;
            _instance = null;
            _closed = true;
        }

        #endregion
    }
}

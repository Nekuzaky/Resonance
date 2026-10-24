using UnityEditor;
using UnityEngine;

namespace Nekuzaky.Resonance.Editor
{
    [InitializeOnLoad]
    internal static class PreviewEngine
    {
        #region Public

        public static ResonanceEngine Engine => Ensure();

        public static ResonanceEngine Active => Resonance.Engine;

        public static ResonanceEngine Ensure()
        {
            if (_engine != null && _host != null)
                return _engine;

            _host = new GameObject("Resonance Preview") { hideFlags = HideFlags.HideAndDontSave };
            _engine = new ResonanceEngine(_host.transform) { Preview = true, Metering = true };
            _lastTime = EditorApplication.timeSinceStartup;
            return _engine;
        }

        #endregion


        #region Private and Protected

        private static GameObject _host;
        private static ResonanceEngine _engine;
        private static double _lastTime;

        static PreviewEngine()
        {
            Resonance.EditorEngine = Ensure;
            EditorApplication.update += Update;
            AssemblyReloadEvents.beforeAssemblyReload += Dispose;
            EditorApplication.quitting += Dispose;
        }

        private static void Update()
        {
            var now = EditorApplication.timeSinceStartup;
            var delta = (float)(now - _lastTime);
            _lastTime = now;
            if (_engine == null || _host == null)
                return;
            _engine.Tick(Mathf.Min(delta, 0.25f));
            var runtime = Application.isPlaying ? Resonance.Engine : null;
            if (runtime != null)
                runtime.Metering = true;
        }

        private static void Dispose()
        {
            _engine?.Shutdown();
            _engine = null;
            if (_host != null)
                Object.DestroyImmediate(_host);
            _host = null;
        }

        #endregion
    }
}

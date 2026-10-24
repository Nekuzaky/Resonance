using Nekuzaky.Resonance.Editor;
using UnityEditor;
using UnityEngine;

namespace Nekuzaky.Resonance.Dev
{
    internal static class PreviewCheck
    {
        #region Public

        public static void Run()
        {
            _wasMuted = EditorUtility.audioMasterMute;
            EditorUtility.audioMasterMute = true;
            var sound = AssetDatabase.LoadAssetAtPath<SoundEvent>($"{DemoBuilder.AssetFolder}/Narrator.asset");
            _handle = PreviewEngine.Engine.Play(sound, null, Vector3.zero);
            _started = EditorApplication.timeSinceStartup;
            EditorApplication.update += Update;
        }

        #endregion


        #region Private and Protected

        private static SoundHandle _handle;
        private static double _started;
        private static bool _wasMuted;

        private static void Update()
        {
            if (EditorApplication.timeSinceStartup - _started < 1.5)
                return;
            EditorApplication.update -= Update;
            var engine = PreviewEngine.Engine;
            var playing = engine.IsPlaying(_handle.Id);
            var source = playing ? engine.Instances[0].Voices[0].Source : null;
            var time = source != null ? source.time : 0f;
            var advancing = source != null && source.isPlaying && time > 1f;
            Debug.Log($"[PreviewCheck] {(advancing ? "PASS" : "FAIL")} the Studio preview plays real audio in Edit Mode: " +
                      $"playing={playing} source.isPlaying={source != null && source.isPlaying} time={time:0.00}s");
            EditorUtility.audioMasterMute = _wasMuted;
            EditorApplication.Exit(advancing ? 0 : 1);
        }

        #endregion
    }
}

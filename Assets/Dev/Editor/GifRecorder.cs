using System.IO;
using Nekuzaky.Resonance.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Nekuzaky.Resonance.Dev
{
    [InitializeOnLoad]
    internal static class GifRecorder
    {
        #region Public

        public static void Run()
        {
            var frames = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Tools", "Output", "media", "frames"));
            if (Directory.Exists(frames))
                Directory.Delete(frames, true);
            Directory.CreateDirectory(frames);
            SessionState.SetString(FramesKey, frames);
            PlayerSettings.runInBackground = true;
            EditorSceneManager.OpenScene(DemoBuilder.ScenePath);
            foreach (var window in Resources.FindObjectsOfTypeAll<StudioWindow>())
                window.Close();
            var studio = StudioWindow.Open();
            studio.position = new Rect(40f, 60f, 1280f, 760f);
            studio.ShowTab(StudioWindow.MixerTab);
            Set(Stage.EnterPlay, 1.0);
            EditorApplication.update += Update;
        }

        #endregion


        #region Private and Protected

        private enum Stage
        {
            Idle,
            EnterPlay,
            RecordMixer,
            SwitchToMusic,
            RecordMusic,
            Quit
        }

        private const string StageKey = "Resonance.Gif.Stage";
        private const string FramesKey = "Resonance.Gif.Frames";
        private const string FrameKey = "Resonance.Gif.Frame";
        private const double FrameInterval = 1.0 / 15.0;

        private static Stage _stage;
        private static double _due;
        private static double _stageEnd;
        private static int _frame;

        static GifRecorder()
        {
            _stage = (Stage)SessionState.GetInt(StageKey, 0);
            _frame = SessionState.GetInt(FrameKey, 0);
            if (_stage == Stage.Idle)
                return;
            _due = EditorApplication.timeSinceStartup + 3.5;
            _stageEnd = _due + 7.0;
            EditorApplication.update += Update;
        }

        private static void Set(Stage stage, double delay)
        {
            _stage = stage;
            _due = EditorApplication.timeSinceStartup + delay;
            SessionState.SetInt(StageKey, (int)stage);
        }

        private static void Update()
        {
            if (_stage == Stage.Idle || EditorApplication.timeSinceStartup < _due)
                return;
            switch (_stage)
            {
                case Stage.EnterPlay:
                    Set(Stage.RecordMixer, 3.5);
                    EditorApplication.EnterPlaymode();
                    break;
                case Stage.RecordMixer:
                    Record(Stage.SwitchToMusic);
                    break;
                case Stage.SwitchToMusic:
                    SwitchToMusic();
                    break;
                case Stage.RecordMusic:
                    Record(Stage.Quit);
                    break;
                case Stage.Quit:
                    Quit();
                    break;
            }
        }

        private static void Record(Stage next)
        {
            var window = Studio();
            var texture = MediaCapture.GrabView(window);
            if (texture != null)
            {
                var path = Path.Combine(SessionState.GetString(FramesKey, string.Empty), $"frame_{_frame:0000}.png");
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                _frame++;
                SessionState.SetInt(FrameKey, _frame);
            }
            _due = EditorApplication.timeSinceStartup + FrameInterval;
            if (EditorApplication.timeSinceStartup >= _stageEnd)
                Set(next, 0.0);
        }

        private static void SwitchToMusic()
        {
            Studio().ShowTab(StudioWindow.MusicTab);
            Resonance.SetState(AssetDatabase.LoadAssetAtPath<StateGroup>($"{DemoBuilder.AssetFolder}/Game.asset"), "Combat");
            Set(Stage.RecordMusic, 0.0);
            _stageEnd = EditorApplication.timeSinceStartup + 6.0;
        }

        private static StudioWindow Studio()
        {
            var windows = Resources.FindObjectsOfTypeAll<StudioWindow>();
            return windows.Length > 0 ? windows[0] : StudioWindow.Open();
        }

        private static void Quit()
        {
            _stage = Stage.Idle;
            SessionState.EraseInt(StageKey);
            SessionState.EraseInt(FrameKey);
            Debug.Log($"Recorded {_frame} frames");
            EditorApplication.Exit(0);
        }

        #endregion
    }
}

using System.IO;
using System.Reflection;
using Nekuzaky.Resonance.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Nekuzaky.Resonance.Dev
{
    [InitializeOnLoad]
    internal static class MediaCapture
    {
        #region Public

        public static void Run()
        {
            _output = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "docs", "images"));
            Directory.CreateDirectory(_output);
            SessionState.SetString(OutputKey, _output);
            PlayerSettings.runInBackground = true;
            EditorSceneManager.OpenScene(DemoBuilder.ScenePath);
            foreach (var window in Resources.FindObjectsOfTypeAll<StudioWindow>())
                window.Close();
            Advance(Stage.OpenStudio, 1.0);
            EditorApplication.update += Update;
        }

        #endregion


        #region Private and Protected

        private enum Stage
        {
            Idle,
            OpenStudio,
            CaptureEvents,
            CaptureBlend,
            EnterPlay,
            CaptureMixer,
            CaptureMusic,
            CaptureProfiler,
            ExitPlay,
            Quit
        }

        private const string StageKey = "Resonance.Capture.Stage";
        private const string OutputKey = "Resonance.Capture.Output";
        private const string DueKey = "Resonance.Capture.Due";
        private static readonly Rect StudioRect = new(40f, 60f, 1480f, 900f);

        private static Stage _stage;
        private static string _output;
        private static double _due;
        private static bool _tabShown;

        static MediaCapture()
        {
            _stage = (Stage)SessionState.GetInt(StageKey, 0);
            _output = SessionState.GetString(OutputKey, string.Empty);
            _due = EditorApplication.timeSinceStartup + SessionState.GetFloat(DueKey, 0f);
            if (_stage != Stage.Idle)
                EditorApplication.update += Update;
        }

        private static void Advance(Stage stage, double delay)
        {
            _stage = stage;
            _due = EditorApplication.timeSinceStartup + delay;
            SessionState.SetInt(StageKey, (int)stage);
            SessionState.SetFloat(DueKey, (float)delay);
        }

        private static void Update()
        {
            if (_stage == Stage.Idle || EditorApplication.timeSinceStartup < _due)
                return;
            switch (_stage)
            {
                case Stage.OpenStudio: OpenStudio(); break;
                case Stage.CaptureEvents: CaptureEvents(); break;
                case Stage.CaptureBlend: CaptureBlend(); break;
                case Stage.EnterPlay: EnterPlay(); break;
                case Stage.CaptureMixer: CaptureTab(StudioWindow.MixerTab, "studio-mixer", Stage.CaptureMusic); break;
                case Stage.CaptureMusic: CaptureTab(StudioWindow.MusicTab, "studio-music", Stage.CaptureProfiler); break;
                case Stage.CaptureProfiler: CaptureTab(StudioWindow.ProfilerTab, "studio-profiler", Stage.ExitPlay); break;
                case Stage.ExitPlay: ExitPlay(); break;
                case Stage.Quit: Quit(); break;
            }
        }

        private static StudioWindow Studio()
        {
            var window = StudioWindow.Open();
            window.position = StudioRect;
            window.Focus();
            return window;
        }

        private static void OpenStudio()
        {
            Studio();
            StudioWindow.Open(Load<SoundEvent>("Footsteps"));
            Advance(Stage.CaptureEvents, 2.0);
        }

        private static void CaptureEvents()
        {
            Save(Studio(), "studio-events");
            StudioWindow.Open(Load<SoundEvent>("Car Engine"));
            Advance(Stage.CaptureBlend, 1.5);
        }

        private static void CaptureBlend()
        {
            Save(Studio(), "studio-blend");
            Advance(Stage.EnterPlay, 0.5);
        }

        private static void EnterPlay()
        {
            Advance(Stage.CaptureMixer, 9.0);
            EditorApplication.EnterPlaymode();
        }

        private static void CaptureTab(int tab, string name, Stage next)
        {
            var window = Studio();
            if (!_tabShown)
            {
                if (tab == StudioWindow.MusicTab)
                    Resonance.SetState(Load<StateGroup>("Game"), "Combat");
                window.ShowTab(tab);
                window.Repaint();
                _tabShown = true;
                _due = EditorApplication.timeSinceStartup + 0.6;
                return;
            }
            _tabShown = false;
            Save(window, name);
            Advance(next, tab == StudioWindow.MixerTab ? 3.0 : 1.5);
        }

        private static void ExitPlay()
        {
            Advance(Stage.Quit, 2.0);
            EditorApplication.ExitPlaymode();
        }

        private static void Quit()
        {
            _stage = Stage.Idle;
            SessionState.EraseInt(StageKey);
            foreach (var window in Resources.FindObjectsOfTypeAll<StudioWindow>())
                window.Close();
            EditorApplication.Exit(0);
        }

        private static T Load<T>(string name) where T : Object => AssetDatabase.LoadAssetAtPath<T>($"{DemoBuilder.AssetFolder}/{name}.asset");

        private static void Save(EditorWindow window, string name)
        {
            var texture = GrabView(window);
            if (texture == null)
            {
                Debug.LogWarning($"Could not capture {name}");
                return;
            }
            File.WriteAllBytes(Path.Combine(_output, name + ".png"), texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            Debug.Log($"Captured {name}");
        }

        private static Texture2D GrabView(EditorWindow window)
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
            var parent = typeof(EditorWindow).GetField("m_Parent", flags)?.GetValue(window);
            var grab = parent?.GetType().GetMethod("GrabPixels", flags);
            if (grab == null)
                return null;

            var scale = EditorGUIUtility.pixelsPerPoint;
            var width = Mathf.RoundToInt(window.position.width * scale);
            var height = Mathf.RoundToInt(window.position.height * scale);
            var target = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            typeof(EditorWindow).GetMethod("RepaintImmediately", flags)?.Invoke(window, null);
            grab.Invoke(parent, new object[] { target, new Rect(0f, 0f, width, height) });
            var previous = RenderTexture.active;
            RenderTexture.active = target;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            FlipVertically(texture);
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            return texture;
        }

        private static void FlipVertically(Texture2D texture)
        {
            var width = texture.width;
            var height = texture.height;
            var pixels = texture.GetPixels32();
            var flipped = new Color32[pixels.Length];
            for (var y = 0; y < height; y++)
                System.Array.Copy(pixels, y * width, flipped, (height - 1 - y) * width, width);
            texture.SetPixels32(flipped);
            texture.Apply();
        }

        #endregion
    }
}

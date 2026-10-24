using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Nekuzaky.Resonance.Dev
{
    internal static class GizmoCapture
    {
        #region Public

        public const string Output = "docs/images/scene-gizmos.png";

        public static void Run()
        {
            EditorSceneManager.OpenScene(DemoBuilder.ScenePath);
            var car = GameObject.Find("Car");
            Selection.activeGameObject = car;
            _view = EditorWindow.GetWindow<SceneView>();
            _view.position = new Rect(60f, 60f, 1280f, 760f);
            _view.drawGizmos = true;
            _view.LookAt(car.transform.position + new Vector3(4f, 0f, 4f), Quaternion.Euler(38f, 20f, 0f), 22f, false, true);
            _view.Focus();
            _due = EditorApplication.timeSinceStartup + 3.0;
            EditorApplication.update += Capture;
        }

        #endregion


        #region Private and Protected

        private static readonly string[] Drawn = { "SoundEmitter", "StateZone", "SoundListener", "ReverbZone" };

        private static double _due;
        private static SceneView _view;

        private static void Capture()
        {
            if (EditorApplication.timeSinceStartup < _due)
                return;
            EditorApplication.update -= Capture;
            var registered = Registered();
            Debug.Log($"[GizmoCapture] {registered} of {Drawn.Length} components have an enabled gizmo");
            var texture = MediaCapture.GrabView(_view);
            if (texture != null)
                File.WriteAllBytes(Output, texture.EncodeToPNG());
            Debug.Log(texture != null ? $"[GizmoCapture] saved {Output}" : "[GizmoCapture] could not grab the Scene view");
            EditorApplication.Exit(texture != null && registered == Drawn.Length ? 0 : 1);
        }

        private static int Registered()
        {
            const int hasGizmo = 2;
            var type = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.AnnotationUtility");
            var annotations = type?.GetMethod("GetAnnotations", BindingFlags.Static | BindingFlags.NonPublic)?.Invoke(null, null) as System.Array;
            var found = 0;
            foreach (var annotation in annotations ?? System.Array.Empty<object>())
            {
                var kind = annotation.GetType();
                var name = kind.GetField("scriptClass")?.GetValue(annotation) as string;
                var flags = kind.GetField("flags")?.GetValue(annotation) is int value ? value : 0;
                var enabled = kind.GetField("gizmoEnabled")?.GetValue(annotation) is int on && on != 0;
                if (System.Array.IndexOf(Drawn, name) >= 0 && (flags & hasGizmo) != 0 && enabled)
                    found++;
            }
            return found;
        }

        #endregion
    }
}

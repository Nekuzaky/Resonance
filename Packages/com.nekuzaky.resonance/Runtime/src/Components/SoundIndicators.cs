using System.Collections.Generic;
using UnityEngine;

namespace Nekuzaky.Resonance
{
    [AddComponentMenu("Resonance/Sound Indicators")]
    [HelpURL("https://github.com/Nekuzaky/Resonance/blob/main/Packages/com.nekuzaky.resonance/Documentation~/accessibility.md")]
    public sealed class SoundIndicators : MonoBehaviour
    {
        #region Public

        public Camera viewCamera;
        [Range(60f, 600f)] public float radius = 180f;
        [Range(0f, 1f)] public float minimumLoudness = 0.02f;
        public bool showCaptions = true;
        public bool showNonDirectional = true;
        public Color color = new(1f, 0.85f, 0.35f, 1f);
        [Range(12, 48)] public int fontSize = 20;

        public int Visible { get; private set; }

        #endregion


        #region Private and Protected

        private readonly List<string> _ambient = new();
        private const int ArrowSize = 32;
        private const float OnListener = 1f;
        private static Texture2D _arrow;
        private GUIStyle _caption;

        private void EnsureStyles()
        {
            _arrow ??= Arrow();
            _caption ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, wordWrap = false };
            _caption.fontSize = fontSize;
        }

        private static Texture2D Arrow()
        {
            var texture = new Texture2D(ArrowSize, ArrowSize, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            var pixels = new Color32[ArrowSize * ArrowSize];
            for (var y = 0; y < ArrowSize; y++)
            {
                var half = (ArrowSize - 1 - y) * 0.3f;
                for (var x = 0; x < ArrowSize; x++)
                {
                    var inside = Mathf.Abs(x - (ArrowSize - 1) * 0.5f) <= half;
                    pixels[y * ArrowSize + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        private static float Loudness(SoundInstance instance)
        {
            var sound = instance.Event;
            var distance = sound.spatialize ? sound.Spatial.Attenuation(instance.Distance) : 1f;
            return Mathf.Clamp01(instance.Gain * instance.Fade * distance);
        }

        private void DrawDirectional(SoundInstance instance, Camera view, float loudness)
        {
            var local = view.transform.InverseTransformPoint(instance.Position);
            var angle = Mathf.Atan2(local.x, local.z);
            var center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            var point = center + new Vector2(Mathf.Sin(angle), -Mathf.Cos(angle)) * radius;
            var previous = GUI.color;
            GUI.color = new Color(color.r, color.g, color.b, Mathf.Lerp(0.35f, 1f, loudness));
            var matrix = GUI.matrix;
            GUIUtility.RotateAroundPivot(angle * Mathf.Rad2Deg, point);
            GUI.DrawTexture(new Rect(point.x - ArrowSize * 0.5f, point.y - ArrowSize * 0.5f, ArrowSize, ArrowSize), _arrow);
            GUI.matrix = matrix;
            if (showCaptions && !string.IsNullOrEmpty(instance.Event.caption))
                GUI.Label(new Rect(point.x - 150f, point.y + 18f, 300f, fontSize + 10f), instance.Event.caption, _caption);
            GUI.color = previous;
        }

        private void DrawAmbient()
        {
            var y = 48f;
            var previous = GUI.color;
            GUI.color = color;
            foreach (var caption in _ambient)
            {
                GUI.Label(new Rect(0f, y, Screen.width, fontSize + 8f), $"[{caption}]", _caption);
                y += fontSize + 8f;
            }
            GUI.color = previous;
        }

        #endregion


        #region Unity Callbacks

        private void OnGUI()
        {
            var engine = Resonance.Engine;
            var view = viewCamera != null ? viewCamera : Camera.main;
            if (engine == null || view == null || Event.current.type != EventType.Repaint)
                return;
            EnsureStyles();
            _ambient.Clear();
            Visible = 0;
            foreach (var instance in engine.Instances)
            {
                var sound = instance.Event;
                if (sound == null || !sound.indicator || instance.State != InstanceState.Playing)
                    continue;
                var loudness = Loudness(instance);
                if (loudness < minimumLoudness)
                    continue;
                Visible++;
                if (sound.spatialize && instance.Distance > OnListener)
                    DrawDirectional(instance, view, loudness);
                else if (showNonDirectional && !string.IsNullOrEmpty(sound.caption) && !_ambient.Contains(sound.caption))
                    _ambient.Add(sound.caption);
            }
            DrawAmbient();
        }

        #endregion
    }
}

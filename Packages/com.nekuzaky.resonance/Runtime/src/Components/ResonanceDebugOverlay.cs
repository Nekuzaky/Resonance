using UnityEngine;

namespace Nekuzaky.Resonance
{
    [AddComponentMenu("Resonance/Debug Overlay")]
    [HelpURL("https://github.com/Nekuzaky/Resonance/blob/main/Packages/com.nekuzaky.resonance/Documentation~/profiler.md")]
    public sealed class ResonanceDebugOverlay : MonoBehaviour
    {
        #region Public

        public Camera viewCamera;
        public bool onlyInDevelopmentBuilds = true;
        public bool showLabels = true;
        public bool showSummary = true;

        public bool IsActive => !onlyInDevelopmentBuilds || Debug.isDebugBuild;

        #endregion


        #region Private and Protected

        private GUIStyle _label;
        private GUIStyle _box;
        private static readonly Color Live = new(0.18f, 0.66f, 0.72f, 1f);
        private static readonly Color Muted = new(0.87f, 0.67f, 0.28f, 1f);

        private void DrawSound(SoundInstance instance, Camera view)
        {
            var screen = view.WorldToScreenPoint(instance.Position);
            if (screen.z <= 0f)
                return;
            var x = screen.x;
            var y = Screen.height - screen.y;
            var sound = instance.Event;
            var occluded = instance.Occlusion > 0.01f ? $"  occl {instance.Occlusion * 100f:0}%" : string.Empty;
            var text = $"{sound.name}  {instance.Distance:0.0} m{occluded}";
            GUI.color = instance.IsVirtual ? Muted : Live;
            GUI.Label(new Rect(x - 120f, y - 34f, 240f, 20f), text, _label);
            var level = Mathf.Clamp01((AudioMath.ToDecibels(instance.Level) + 60f) / 60f);
            GUI.DrawTexture(new Rect(x - 40f, y - 12f, 80f * level, 4f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void DrawSummary(ResonanceEngine engine)
        {
            var stats = engine.Stats;
            var music = engine.Music.Current != null ? engine.Music.Current.name : "none";
            var text = $"Resonance  ·  {stats.Instances} sounds  ·  {stats.RealVoices} real, {stats.VirtualVoices} virtual voices  ·  " +
                       $"{stats.TickMilliseconds:0.000} ms  ·  music: {music}";
            GUI.Label(new Rect(10f, 10f, 640f, 24f), text, _box);
        }

        #endregion


        #region Unity Callbacks

        private void OnGUI()
        {
            var engine = Resonance.Engine;
            var view = viewCamera != null ? viewCamera : Camera.main;
            if (!IsActive || engine == null || view == null || Event.current.type != EventType.Repaint)
                return;
            _label ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = 12 };
            _box ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleLeft, fontSize = 12 };
            if (showSummary)
                DrawSummary(engine);
            if (!showLabels)
                return;
            foreach (var instance in engine.Instances)
            {
                if (instance.Event != null && instance.Event.spatialize)
                    DrawSound(instance, view);
            }
        }

        #endregion
    }
}

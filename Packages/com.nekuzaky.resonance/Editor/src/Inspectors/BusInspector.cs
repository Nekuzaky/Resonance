using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Nekuzaky.Resonance.Editor
{
    [CustomEditor(typeof(AudioBus))]
    internal sealed class AudioBusInspector : UnityEditor.Editor
    {
        #region Public

        public const int RefreshMilliseconds = 200;

        internal EffectsPanel Effects { get; private set; }

        public override VisualElement CreateInspectorGUI()
        {
            var bus = (AudioBus)target;
            var root = new VisualElement();
            if (Glyphs.StyleSheet != null)
                root.styleSheets.Add(Glyphs.StyleSheet);
            root.Add(InspectorHeader.Build(nameof(AudioBus), Summary(bus),
                InspectorHeader.Button("Open in Studio", () => StudioWindow.Open(bus))));
            var iterator = serializedObject.GetIterator();
            for (var enter = true; iterator.NextVisible(enter); enter = false)
            {
                if (iterator.name == "m_Script" || iterator.name == EffectsProperty)
                    continue;
                root.Add(new PropertyField(iterator.Copy()));
            }
            Effects = new EffectsPanel();
            Effects.Element.AddToClassList("rs-effects--inspector");
            Effects.Show(bus);
            root.Add(Effects.Element);
            root.schedule.Execute(() => Effects.Refresh(Resonance.Engine)).Every(RefreshMilliseconds);
            return root;
        }

        #endregion


        #region Private and Protected

        private const string EffectsProperty = "effects";

        private static string Summary(AudioBus bus)
        {
            var parent = bus.parent != null ? $"to {bus.parent.name}" : "output";
            var count = bus.effects != null ? bus.effects.Count : 0;
            return $"{parent}  ·  {count} effect{(count == 1 ? string.Empty : "s")}";
        }

        #endregion
    }
}

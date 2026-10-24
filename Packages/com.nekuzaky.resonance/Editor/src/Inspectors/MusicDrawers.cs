using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Nekuzaky.Resonance.Editor
{
    internal static class MusicChoices
    {
        public static IEnumerable<string> Segments(SerializedProperty property)
        {
            if (property.serializedObject.targetObject is not MusicTrack track)
                yield break;
            foreach (var segment in track.segments)
            {
                if (segment != null && !string.IsNullOrEmpty(segment.name))
                    yield return segment.name;
            }
        }

        public static IEnumerable<string> States(SerializedProperty property) =>
            property.serializedObject.targetObject is MusicTrack track ? InspectorKit.States(track.stateGroup) : new string[0];

        public static void RefreshOnChange(VisualElement root, SerializedProperty property, params ChoiceField[] fields)
        {
            root.TrackSerializedObjectValue(property.serializedObject, _ =>
            {
                foreach (var field in fields)
                    field.Refresh();
            });
        }
    }

    [CustomPropertyDrawer(typeof(MusicStateLink))]
    internal sealed class MusicStateLinkDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            var state = new ChoiceField(property.FindPropertyRelative("state"), "When State Is", () => MusicChoices.States(property), "(choose)",
                "A state of the track's State Group.");
            var segment = new ChoiceField(property.FindPropertyRelative("segment"), "Play Segment", () => MusicChoices.Segments(property), "(choose)", null);
            var bridge = new ChoiceField(property.FindPropertyRelative("bridge"), "Through Bridge", () => MusicChoices.Segments(property), "(none)",
                "Optional: a short segment played on the way, like a riser or a drum fill.");
            root.Add(state);
            root.Add(segment);
            root.Add(bridge);
            MusicChoices.RefreshOnChange(root, property, state, segment, bridge);
            return root;
        }
    }

    [CustomPropertyDrawer(typeof(MusicSegment))]
    internal sealed class MusicSegmentDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var root = new VisualElement();
            root.Add(new PropertyField(property.FindPropertyRelative("name"), "Name"));
            root.Add(new PropertyField(property.FindPropertyRelative("clip"), "Clip"));
            root.Add(new PropertyField(property.FindPropertyRelative("bars"), "Length (bars)") { tooltip = "0 uses the clip length." });
            var next = new ChoiceField(property.FindPropertyRelative("next"), "Then Play", () => MusicChoices.Segments(property), "(loop this one)", null);
            root.Add(next);
            root.Add(new PropertyField(property.FindPropertyRelative("volume"), "Volume (dB)"));
            root.Add(new PropertyField(property.FindPropertyRelative("layers"), "Layers")
            {
                tooltip = "Stems that play in sync with the segment. Each fades in and out with a parameter, like intensity."
            });
            MusicChoices.RefreshOnChange(root, property, next);
            return root;
        }
    }
}

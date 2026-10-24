using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.Resonance.Editor
{
    internal static class InspectorHeader
    {
        public static VisualElement Build(string icon, string summary, params Button[] buttons)
        {
            var root = new VisualElement();
            root.styleSheets.Add(Glyphs.StyleSheet);
            var box = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    marginBottom = 6f,
                    marginTop = 2f
                }
            };
            box.Add(Glyphs.AssetImage(icon, "rs-asset-icon"));
            var label = new Label(summary) { style = { flexGrow = 1f, whiteSpace = WhiteSpace.Normal } };
            box.Add(label);
            foreach (var button in buttons)
            {
                button.style.height = 22f;
                box.Add(button);
            }
            root.Add(box);
            return root;
        }

        public static Button Button(string text, System.Action click) => new(click) { text = text };
    }

    [CustomEditor(typeof(SoundEvent))]
    internal sealed class SoundEventInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var sound = (SoundEvent)target;
            var root = new VisualElement();
            root.Add(InspectorHeader.Build(nameof(SoundEvent), Summary(sound),
                InspectorHeader.Button("Play", () => PreviewEngine.Engine.Play(sound, null, Vector3.zero)),
                InspectorHeader.Button("Stop", () => PreviewEngine.Engine.StopEvent(sound, -1f)),
                InspectorHeader.Button("Open in Studio", () => StudioWindow.Open(sound))));
            var help = new HelpBox("Edit the sound tree in Resonance Studio (double-click the asset). The fields below are the event settings.",
                HelpBoxMessageType.None);
            root.Add(help);
            var iterator = serializedObject.GetIterator();
            for (var enter = true; iterator.NextVisible(enter); enter = false)
            {
                if (iterator.name == "m_Script" || iterator.name == "root")
                    continue;
                root.Add(new PropertyField(iterator.Copy()));
            }
            return root;
        }

        private static string Summary(SoundEvent sound)
        {
            var clips = 0;
            var walker = new NodeWalker(sound.root);
            for (var guard = 0; guard < NodeWalker.MaxNodes && walker.Next(out var node); guard++)
            {
                if (node is ClipNode { clip: not null })
                    clips++;
            }
            var bus = sound.bus != null ? sound.bus.name : "no bus";
            return $"{clips} clip{(clips == 1 ? string.Empty : "s")}  ·  {bus}  ·  {(sound.spatialize ? "3D" : "2D")}";
        }
    }

    [CustomEditor(typeof(SoundEmitter))]
    [CanEditMultipleObjects]
    internal sealed class SoundEmitterInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var emitter = (SoundEmitter)target;
            var root = new VisualElement();
            root.Add(InspectorHeader.Build(nameof(SoundEmitter), "Plays a Sound Event from this GameObject.",
                InspectorHeader.Button("Play", () => Preview(emitter)),
                InspectorHeader.Button("Stop", () => Stop(emitter)),
                InspectorHeader.Button("Edit Sound", () =>
                {
                    if (emitter.sound != null)
                        StudioWindow.Open(emitter.sound);
                })));
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            return root;
        }

        private static void Preview(SoundEmitter emitter)
        {
            if (Application.isPlaying)
                emitter.Play();
            else if (emitter.sound != null)
                PreviewEngine.Engine.Play(emitter.sound, null, Vector3.zero);
        }

        private static void Stop(SoundEmitter emitter)
        {
            if (Application.isPlaying)
                emitter.Stop();
            else if (emitter.sound != null)
                PreviewEngine.Engine.StopEvent(emitter.sound, -1f);
        }
    }

    [CustomEditor(typeof(MusicTrack))]
    internal sealed class MusicTrackInspector : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            var track = (MusicTrack)target;
            var root = new VisualElement();
            root.Add(InspectorHeader.Build(nameof(MusicTrack), $"{track.segments.Count} segments  ·  {track.bpm:0.#} BPM",
                InspectorHeader.Button("Play", () => Resonance.PlayMusic(track)),
                InspectorHeader.Button("Stop", () => Resonance.StopMusic(track.crossfade)),
                InspectorHeader.Button("Open in Studio", () => StudioWindow.Open(track))));
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            return root;
        }
    }
}

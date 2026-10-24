using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.Resonance.Editor
{
    internal static class Glyphs
    {
        #region Public

        public const string Logo = "soundwave";
        public const string Event = "volume-up-fill";
        public const string Clip = "file-earmark-music-fill";
        public const string Random = "shuffle";
        public const string Sequence = "list-ol";
        public const string Switch = "signpost-split-fill";
        public const string Blend = "intersect";
        public const string Layer = "layers-fill";
        public const string Parameter = "sliders";
        public const string SwitchGroup = "toggles";
        public const string StateGroup = "flag-fill";
        public const string Bus = "sliders2-vertical";
        public const string Snapshot = "camera-fill";
        public const string Music = "music-note-beamed";
        public const string Stinger = "lightning-fill";
        public const string Profiler = "activity";
        public const string Cost = "cpu";
        public const string Play = "play-fill";
        public const string Stop = "stop-fill";
        public const string Add = "plus-lg";
        public const string Remove = "trash3";
        public const string Duplicate = "copy";
        public const string Up = "arrow-up";
        public const string Down = "arrow-down";
        public const string Search = "search";
        public const string Mute = "volume-mute-fill";
        public const string Solo = "headphones";
        public const string Folder = "folder2-open";
        public const string Settings = "gear-fill";
        public const string Docs = "book";
        public const string GitHub = "github";
        public const string Coffee = "cup-hot-fill";
        public const string Emitter = "broadcast";
        public const string Info = "info-circle";
        public const string Close = "x-lg";
        public const string Warning = "exclamation-triangle-fill";
        public const string Expanded = "chevron-down";
        public const string Collapsed = "chevron-right";
        public const string Ping = "box-arrow-up-right";
        public const string Hierarchy = "diagram-3-fill";
        public const string Voices = "collection-play-fill";
        public const string History = "clock-history";
        public const string Loop = "arrow-repeat";
        public const string Next = "skip-forward-fill";
        public const string Waiting = "hourglass-split";
        public const string Ok = "check-circle-fill";
        public const string Rejected = "x-octagon-fill";
        public const string Virtual = "dash-circle-fill";
        public const string Playing = "record-circle-fill";
        public const string Filter = "funnel-fill";
        public const string Pin = "pin-angle-fill";
        public const string Note = "music-note";
        public const string Magic = "magic";
        public const string Attenuation = "reception-4";
        public const string Bank = "box-seam";
        public const string Reverb = "bricks";
        public const string Memory = "hdd-stack";
        public const string Delay = "clock";
        public const string Dialogue = "chat-quote-fill";
        public const string Language = "translate";
        public const string Remote = "phone";
        public const string Accessibility = "universal-access-circle";
        public const string Loudness = "bar-chart-fill";

        public static string Of(NodeKind kind)
        {
            switch (kind)
            {
                case NodeKind.Random: return Random;
                case NodeKind.Sequence: return Sequence;
                case NodeKind.Switch: return Switch;
                case NodeKind.Blend: return Blend;
                case NodeKind.Layer: return Layer;
                case NodeKind.Dialogue: return Dialogue;
                default: return Clip;
            }
        }

        public static Texture2D AssetIcon(string name) => AssetDatabase.LoadAssetAtPath<Texture2D>($"{IconRoot}/{name}.png");

        public static StyleSheet StyleSheet => AssetDatabase.LoadAssetAtPath<StyleSheet>($"{Root}/Resonance.uss");

        #endregion


        #region Private and Protected

        private const string Package = "Packages/com.nekuzaky.resonance/Editor";
        private const string Root = Package + "/UI";
        private const string IconRoot = Package + "/Icons";
        private const string GlyphFolder = Root + "/Icons";

        private static readonly Dictionary<string, VectorImage> Cache = new();

        #endregion


        #region Main API

        public static VectorImage Get(string name)
        {
            if (Cache.TryGetValue(name, out var cached) && cached != null)
                return cached;

            var image = AssetDatabase.LoadAssetAtPath<VectorImage>($"{GlyphFolder}/{name}.svg");
            Debug.Assert(image != null, $"Missing Resonance icon '{name}'.");
            Cache[name] = image;
            return image;
        }

        public static VisualElement Icon(string name, params string[] classes)
        {
            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("rs-icon");
            icon.style.backgroundImage = new StyleBackground(Get(name));
            foreach (var className in classes)
                icon.AddToClassList(className);
            return icon;
        }

        public static void Set(VisualElement icon, string name) => icon.style.backgroundImage = new StyleBackground(Get(name));

        public static Image AssetImage(string name, string className)
        {
            var image = new Image { image = AssetIcon(name), pickingMode = PickingMode.Ignore };
            image.AddToClassList(className);
            return image;
        }

        #endregion
    }
}

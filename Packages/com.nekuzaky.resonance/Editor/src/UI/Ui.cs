using System;
using UnityEngine.UIElements;

namespace Nekuzaky.Resonance.Editor
{
    internal static class Ui
    {
        public static Button IconButton(string glyph, string tooltip, Action click, params string[] classes)
        {
            var button = new Button(click) { tooltip = tooltip };
            button.AddToClassList("rs-button");
            button.AddToClassList("rs-button--icon");
            button.Add(Glyphs.Icon(glyph));
            foreach (var className in classes)
                button.AddToClassList(className);
            return button;
        }

        public static Button TextButton(string glyph, string text, Action click, params string[] classes)
        {
            var button = new Button(click);
            button.AddToClassList("rs-button");
            var label = new Label(text);
            if (!string.IsNullOrEmpty(glyph))
            {
                button.Add(Glyphs.Icon(glyph));
                label.AddToClassList("rs-button-label");
            }
            button.Add(label);
            foreach (var className in classes)
                button.AddToClassList(className);
            return button;
        }

        public static VisualElement GroupTitle(string glyph, string text)
        {
            var title = new VisualElement();
            title.AddToClassList("rs-group-title");
            if (!string.IsNullOrEmpty(glyph))
                title.Add(Glyphs.Icon(glyph, "rs-icon--small", "rs-icon--dim"));
            title.Add(new Label(text.ToUpperInvariant()));
            return title;
        }

        public static Label Help(string text)
        {
            var label = new Label(text);
            label.AddToClassList("rs-help");
            return label;
        }

        public static Label Styled(string text, params string[] classes)
        {
            var label = new Label(text);
            foreach (var className in classes)
                label.AddToClassList(className);
            return label;
        }

        public static VisualElement Spacer()
        {
            var spacer = new VisualElement();
            spacer.AddToClassList("rs-spacer");
            return spacer;
        }

        public static VisualElement Box(params string[] classes)
        {
            var box = new VisualElement();
            foreach (var className in classes)
                box.AddToClassList(className);
            return box;
        }

        public static string Decibels(float value) => value <= AudioMath.SilenceDb + 0.01f ? "-inf dB" : $"{value:+0.0;-0.0;0.0} dB";

        public static string KindClass(NodeKind kind) => "rs-kind--" + kind.ToString().ToLowerInvariant();
    }
}

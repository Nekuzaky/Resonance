using Nekuzaky.Resonance.Editor;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.Resonance.Dev
{
    internal sealed class ComponentPreviewWindow : EditorWindow
    {
        public static ComponentPreviewWindow Show(Rect position, params Component[] components)
        {
            var window = CreateInstance<ComponentPreviewWindow>();
            window.titleContent = new GUIContent("Resonance Components");
            window.ShowUtility();
            window.position = position;
            var root = window.rootVisualElement;
            root.styleSheets.Add(Glyphs.StyleSheet);
            root.style.backgroundColor = new Color(0.22f, 0.22f, 0.22f);
            var scroll = new ScrollView();
            root.Add(scroll);
            foreach (var component in components)
            {
                if (component == null)
                    continue;
                var title = new Label($"{component.gameObject.name}  ·  {ObjectNames.NicifyVariableName(component.GetType().Name)}")
                {
                    style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 10f, marginLeft = 6f, color = new Color(0.86f, 0.87f, 0.9f) }
                };
                scroll.Add(title);
                var inspector = new InspectorElement(component) { style = { marginLeft = 4f, marginRight = 4f } };
                scroll.Add(inspector);
            }
            return window;
        }
    }
}

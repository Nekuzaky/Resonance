using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.Resonance.Editor
{
    internal sealed class ChoiceField : DropdownField
    {
        #region Public

        public const string MissingSuffix = "  (not found)";

        public ChoiceField(SerializedProperty property, string label, Func<IEnumerable<string>> source, string empty, string tooltip)
            : base(label)
        {
            _property = property;
            _source = source;
            _empty = empty;
            this.tooltip = tooltip;
            AddToClassList(alignedFieldUssClassName);
            this.RegisterValueChangedCallback(evt => Select(evt.newValue));
            Refresh();
        }

        public void Refresh()
        {
            _values.Clear();
            var labels = new List<string>();
            if (_empty != null)
                Add(labels, _empty, string.Empty);
            foreach (var choice in _source() ?? Array.Empty<string>())
            {
                if (!string.IsNullOrEmpty(choice) && !_values.ContainsValue(choice))
                    Add(labels, choice, choice);
            }
            var current = _property.stringValue;
            var shown = Label(current);
            if (shown == null)
            {
                shown = current + MissingSuffix;
                Add(labels, shown, current);
            }
            choices = labels;
            SetValueWithoutNotify(shown);
            EnableInClassList("rs-choice--missing", shown.EndsWith(MissingSuffix));
        }

        #endregion


        #region Private and Protected

        private readonly SerializedProperty _property;
        private readonly Func<IEnumerable<string>> _source;
        private readonly string _empty;
        private readonly Dictionary<string, string> _values = new();

        private void Add(List<string> labels, string label, string value)
        {
            labels.Add(label);
            _values[label] = value;
        }

        private string Label(string value)
        {
            foreach (var pair in _values)
            {
                if (pair.Value == (value ?? string.Empty))
                    return pair.Key;
            }
            return null;
        }

        internal void Select(string label)
        {
            if (!_values.TryGetValue(label, out var value))
                return;
            _property.serializedObject.Update();
            _property.stringValue = value;
            _property.serializedObject.ApplyModifiedProperties();
            Refresh();
        }

        #endregion
    }

    internal sealed class Warnings : VisualElement
    {
        private readonly List<(HelpBoxMessageType Type, string Message, string Fix, Action Action)> _items = new();

        public void Clear(bool refresh)
        {
            _items.Clear();
            if (refresh)
                Render();
        }

        public void Add(HelpBoxMessageType type, string message, string fix = null, Action action = null) =>
            _items.Add((type, message, fix, action));

        private string _shown;

        public void Render()
        {
            var key = string.Empty;
            foreach (var item in _items)
                key += $"{item.Type}|{item.Message}|{item.Fix};";
            if (key == _shown)
                return;
            _shown = key;
            base.Clear();
            foreach (var item in _items)
            {
                var box = new HelpBox(item.Message, item.Type);
                if (item.Action != null)
                {
                    var action = item.Action;
                    box.Add(new Button(action) { text = item.Fix });
                }
                Add(box);
            }
        }
    }

    internal static class InspectorKit
    {
        #region Public

        public static VisualElement Root(string icon, string summary, params Button[] buttons) => InspectorHeader.Build(icon, summary, buttons);

        public static PropertyField Field(SerializedObject serialized, string path, string label, string tooltip = null) =>
            new(serialized.FindProperty(path), label) { tooltip = tooltip };

        public static ChoiceField Choice(SerializedObject serialized, string path, string label, Func<IEnumerable<string>> source,
            string empty = null, string tooltip = null) => new(serialized.FindProperty(path), label, source, empty, tooltip);

        public static ChoiceField Tag(SerializedObject serialized, string path, string label, string tooltip) =>
            Choice(serialized, path, label, () => UnityEditorInternal.InternalEditorUtility.tags, "(any object)", tooltip);

        public static Button PlayButton(string text, Action click, string tooltip = "Works in Play Mode")
        {
            var button = new Button(click) { text = text, tooltip = tooltip };
            button.schedule.Execute(() => button.SetEnabled(Application.isPlaying)).Every(250);
            return button;
        }

        public static Label Live(Func<string> text)
        {
            var label = new Label { style = { color = new Color(0.4f, 0.75f, 0.52f), marginTop = 4f, whiteSpace = WhiteSpace.Normal } };
            label.schedule.Execute(() =>
            {
                label.style.display = Application.isPlaying ? DisplayStyle.Flex : DisplayStyle.None;
                if (Application.isPlaying)
                    label.text = text();
            }).Every(200);
            return label;
        }

        public static void Watch(VisualElement root, SerializedObject serialized, Action validate)
        {
            root.TrackSerializedObjectValue(serialized, _ => validate());
            root.schedule.Execute(validate).Every(1000);
            validate();
        }

        public static IEnumerable<string> Values(SwitchGroup group) => group != null ? group.values : Array.Empty<string>();

        public static IEnumerable<string> States(StateGroup group) => group != null ? group.states : Array.Empty<string>();

        public static bool HasCollider(GameObject target) => target.GetComponent<Collider>() != null || target.GetComponent<Collider2D>() != null;

        public static bool IsTrigger(GameObject target)
        {
            var collider = target.GetComponent<Collider>();
            if (collider != null)
                return collider.isTrigger;
            var collider2D = target.GetComponent<Collider2D>();
            return collider2D != null && collider2D.isTrigger;
        }

        public static void MakeTrigger(GameObject target)
        {
            var collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                Undo.RecordObject(collider, "Make trigger");
                collider.isTrigger = true;
            }
            var collider2D = target.GetComponent<Collider2D>();
            if (collider2D != null)
            {
                Undo.RecordObject(collider2D, "Make trigger");
                collider2D.isTrigger = true;
            }
        }

        public static void AddBoxCollider(GameObject target, bool trigger)
        {
            var collider = Undo.AddComponent<BoxCollider>(target);
            collider.isTrigger = trigger;
        }

        #endregion
    }
}

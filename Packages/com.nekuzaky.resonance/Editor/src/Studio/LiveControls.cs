using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.Resonance.Editor
{
    internal static class LiveControls
    {
        #region Main API

        public static VisualElement Build(ScriptableObject asset)
        {
            switch (asset)
            {
                case StateGroup states: return States(states);
                case SwitchGroup switches: return Switches(switches);
                case SoundParameter parameter: return Parameter(parameter);
                case MixSnapshot snapshot: return Snapshot(snapshot);
                default: return null;
            }
        }

        #endregion


        #region Private and Protected

        private static VisualElement Section(string help)
        {
            var section = new VisualElement();
            section.Add(Ui.GroupTitle(Glyphs.Playing, "Live"));
            section.Add(Ui.Help(help));
            return section;
        }

        private static VisualElement States(StateGroup group)
        {
            var section = Section("Click a state to set it now. In Play Mode this changes the running game; otherwise it drives the preview.");
            var row = Ui.Box("rs-cards");
            var buttons = new Button[group.states.Count];
            for (var i = 0; i < group.states.Count; i++)
            {
                var state = group.states[i];
                buttons[i] = Ui.TextButton(Glyphs.StateGroup, state, () => Resonance.SetState(group, state));
                buttons[i].userData = state;
                row.Add(buttons[i]);
            }
            section.Add(row);
            section.schedule.Execute(() =>
            {
                var current = Resonance.GetState(group);
                foreach (var button in buttons)
                    button.EnableInClassList("rs-button--on", (string)button.userData == current);
            }).Every(150);
            return section;
        }

        private static VisualElement Switches(SwitchGroup group)
        {
            var section = Section("Click a value to set it for every emitter that has no value of its own.");
            var row = Ui.Box("rs-cards");
            foreach (var value in group.values)
                row.Add(Ui.TextButton(Glyphs.SwitchGroup, value, () => Resonance.SetSwitch(group, value)));
            section.Add(row);
            return section;
        }

        private static VisualElement Parameter(SoundParameter parameter)
        {
            var section = Section("Drag to change the global value. Sounds that use this parameter react immediately.");
            var slider = new Slider(parameter.name, Mathf.Min(parameter.min, parameter.max), Mathf.Max(parameter.min, parameter.max))
            {
                showInputField = true,
                value = Resonance.GetParameter(parameter)
            };
            slider.RegisterValueChangedCallback(evt => Resonance.SetParameter(parameter, evt.newValue));
            section.Add(slider);
            return section;
        }

        private static VisualElement Snapshot(MixSnapshot snapshot)
        {
            var section = Section("Apply the snapshot to hear the mix change, remove it to go back.");
            var button = Ui.TextButton(Glyphs.Snapshot, "Apply", null);
            button.clicked += () =>
            {
                var engine = Resonance.Engine;
                if (engine == null)
                    return;
                if (engine.Mixer.IsActive(snapshot))
                    engine.Mixer.Remove(snapshot);
                else
                    engine.Mixer.Apply(snapshot);
            };
            var label = button.Q<Label>();
            section.schedule.Execute(() =>
            {
                var active = Resonance.Engine != null && Resonance.Engine.Mixer.IsActive(snapshot);
                label.text = active ? "Remove" : "Apply";
                button.EnableInClassList("rs-button--on", active);
            }).Every(150);
            section.Add(button);
            return section;
        }

        #endregion
    }
}

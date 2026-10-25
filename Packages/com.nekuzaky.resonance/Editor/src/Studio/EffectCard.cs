using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.Resonance.Editor
{
    internal sealed class EffectCard
    {
        #region Public

        public readonly VisualElement Element;

        public BusEffect Effect { get; }

        public EffectCard(AudioBus bus, int index, Action rebuild)
        {
            _bus = bus;
            _index = index;
            _rebuild = rebuild;
            Effect = bus.effects[index];
            EffectEditing.EnsureValues(bus, Effect);
            Element = Ui.Box("rs-effect");
            Element.Add(Header());
            if (Effect.type == BusEffectType.Reverb)
                Element.Add(Routing());
            var parameters = BusEffects.Parameters(Effect.type);
            for (var p = 0; p < parameters.Count; p++)
            {
                var row = new ParameterRow(bus, Effect, p);
                _rows.Add(row);
                Element.Add(row.Element);
            }
            if (Effect.type == BusEffectType.Compressor)
            {
                _reduction = Ui.Styled(string.Empty, "rs-effect-live");
                Element.Add(_reduction);
            }
            Element.Add(Modulations());
            Element.EnableInClassList("rs-effect--bypassed", Effect.bypass);
        }

        public void Refresh(ResonanceEngine engine)
        {
            _on.SetValueWithoutNotify(!Effect.bypass);
            Element.EnableInClassList("rs-effect--bypassed", Effect.bypass);
            var live = engine?.Mixer.EffectValues(_bus, _index);
            foreach (var row in _rows)
                row.Refresh(live);
            if (_reduction != null)
            {
                var reduction = engine != null ? engine.Mixer.GainReduction(_bus) : 0f;
                _reduction.text = reduction > 0.05f ? $"Gain reduction now: -{reduction:0.0} dB" : "Gain reduction now: none";
            }
        }

        #endregion


        #region Private and Protected

        private readonly AudioBus _bus;
        private readonly int _index;
        private readonly Action _rebuild;
        private readonly List<ParameterRow> _rows = new();
        private Toggle _on;
        private Label _reduction;

        private VisualElement Header()
        {
            var header = Ui.Box("rs-effect-header");
            header.Add(Ui.Styled($"{_index + 1}", "rs-effect-index"));
            header.Add(Ui.Styled(BusEffects.Label(Effect.type), "rs-effect-name"));
            header.Add(Ui.Spacer());
            _on = new Toggle("On") { value = !Effect.bypass, tooltip = "Turn the effect off without losing its settings." };
            _on.RegisterValueChangedCallback(evt => EffectEditing.Change(_bus, "Bypass bus effect", () => Effect.bypass = !evt.newValue));
            header.Add(_on);
            header.Add(Ui.IconButton(Glyphs.Up, "Move up (earlier in the chain)", () => Move(-1), "rs-button--flat"));
            header.Add(Ui.IconButton(Glyphs.Down, "Move down (later in the chain)", () => Move(1), "rs-button--flat"));
            header.Add(Ui.IconButton(Glyphs.Remove, "Remove the effect", Remove, "rs-button--flat"));
            return header;
        }

        private VisualElement Routing()
        {
            var field = new EnumField("Routing", Effect.routing)
            {
                tooltip = "Send keeps the dry sound at full level and adds the reverb. Insert crossfades from dry to wet with Mix."
            };
            field.RegisterValueChangedCallback(evt => EffectEditing.Change(_bus, "Change reverb routing", () => Effect.routing = (ReverbRouting)evt.newValue));
            return field;
        }

        private VisualElement Modulations()
        {
            var serialized = new SerializedObject(_bus);
            var property = serialized.FindProperty($"effects.Array.data[{_index}].modulations");
            var field = new PropertyField(property, "Driven By Parameters")
            {
                tooltip = "Each curve maps a parameter (0 to 1 across its range) to the value of one effect setting."
            };
            field.Bind(serialized);
            return field;
        }

        private void Move(int direction)
        {
            var target = _index + direction;
            if (target < 0 || target >= _bus.effects.Count)
                return;
            EffectEditing.Change(_bus, "Move bus effect", () =>
            {
                _bus.effects[_index] = _bus.effects[target];
                _bus.effects[target] = Effect;
            });
            _rebuild();
        }

        private void Remove()
        {
            EffectEditing.Change(_bus, "Remove bus effect", () => _bus.effects.RemoveAt(_index));
            _rebuild();
        }

        #endregion
    }

    internal sealed class ParameterRow
    {
        #region Public

        public readonly VisualElement Element;

        public ParameterRow(AudioBus bus, BusEffect effect, int index)
        {
            _bus = bus;
            _effect = effect;
            _index = index;
            _info = BusEffects.Parameters(effect.type)[index];
            Element = Ui.Box("rs-effect-row");
            Element.Add(Ui.Styled(_info.Name, "rs-effect-label"));
            _slider = new Slider(0f, 1f) { value = Normalize(effect.Value(index)) };
            _slider.AddToClassList("rs-effect-slider");
            _slider.RegisterValueChangedCallback(evt => Set(Denormalize(evt.newValue)));
            Element.Add(_slider);
            _field = new FloatField { value = effect.Value(index) };
            _field.AddToClassList("rs-effect-value");
            _field.RegisterValueChangedCallback(evt => Set(evt.newValue));
            Element.Add(_field);
            Element.Add(Ui.Styled(_info.Unit, "rs-effect-unit"));
            _live = Ui.Styled(string.Empty, "rs-effect-live");
            _live.tooltip = "The value now, after parameters and snapshots.";
            Element.Add(_live);
        }

        public void Refresh(float[] live)
        {
            var value = _effect.Value(_index);
            if (_slider.focusController?.focusedElement != _slider)
                _slider.SetValueWithoutNotify(Normalize(value));
            if (_field.focusController?.focusedElement != _field)
                _field.SetValueWithoutNotify(value);
            var now = live != null && _index < live.Length ? live[_index] : value;
            _live.text = Mathf.Abs(now - value) > 0.0001f * Mathf.Max(1f, Mathf.Abs(value)) ? $"now {Format(now)}" : string.Empty;
        }

        #endregion


        #region Private and Protected

        private readonly AudioBus _bus;
        private readonly BusEffect _effect;
        private readonly int _index;
        private readonly EffectParameterInfo _info;
        private readonly Slider _slider;
        private readonly FloatField _field;
        private readonly Label _live;

        internal void Set(float value)
        {
            var rounded = Round(_info.Clamp(value));
            if (Mathf.Approximately(rounded, _effect.Value(_index)))
                return;
            EffectEditing.Change(_bus, "Change bus effect", () => _effect.SetValue(_index, rounded));
            _field.SetValueWithoutNotify(_effect.Value(_index));
        }

        private float Normalize(float value)
        {
            if (_info.Logarithmic)
                return Mathf.InverseLerp(Mathf.Log(_info.Min), Mathf.Log(_info.Max), Mathf.Log(Mathf.Max(_info.Min, value)));
            return Mathf.InverseLerp(_info.Min, _info.Max, value);
        }

        private float Denormalize(float t) =>
            _info.Logarithmic ? Mathf.Exp(Mathf.Lerp(Mathf.Log(_info.Min), Mathf.Log(_info.Max), t)) : Mathf.Lerp(_info.Min, _info.Max, t);

        private float Round(float value)
        {
            var step = _info.Max - _info.Min > 100f ? 1f : _info.Max - _info.Min > 10f ? 0.1f : 0.001f;
            return Mathf.Round(value / step) * step;
        }

        private string Format(float value) => _info.Max - _info.Min > 100f ? $"{value:0}" : $"{value:0.##}";

        #endregion
    }

    internal static class EffectEditing
    {
        public static void Change(AudioBus bus, string undo, Action apply)
        {
            if (bus == null)
                return;
            Undo.RecordObject(bus, undo);
            apply();
            EditorUtility.SetDirty(bus);
        }

        public static void EnsureValues(AudioBus bus, BusEffect effect)
        {
            var count = BusEffects.Parameters(effect.type).Count;
            if (effect.values != null && effect.values.Length >= count)
                return;
            Change(bus, "Upgrade bus effect", () => effect.SetValue(0, effect.Value(0)));
        }

        public static List<string> ParameterNames(BusEffectType type)
        {
            var names = new List<string>();
            var parameters = BusEffects.Parameters(type);
            for (var i = 0; i < parameters.Count; i++)
                names.Add(string.IsNullOrEmpty(parameters[i].Unit) ? parameters[i].Name : $"{parameters[i].Name} ({parameters[i].Unit})");
            return names;
        }
    }
}

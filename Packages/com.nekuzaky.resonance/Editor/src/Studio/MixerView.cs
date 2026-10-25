using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.Resonance.Editor
{
    internal sealed class MixerView : IStudioView
    {
        #region Public

        public VisualElement Root { get; }

        public MixerView()
        {
            Root = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            Root.AddToClassList("rs-mixer");
            Rebuild();
        }

        public void Refresh()
        {
            var engine = Resonance.Engine;
            foreach (var strip in _strips)
                strip.Refresh(engine, _effects.Bus);
            _effects.Refresh(engine);
            foreach (var snapshot in _snapshots)
                snapshot.Refresh(engine);
        }

        public void ProjectChanged() => Rebuild();

        #endregion


        #region Private and Protected

        private readonly List<BusStrip> _strips = new();
        private readonly List<SnapshotRow> _snapshots = new();
        private readonly EffectsPanel _effects = new();

        private void Rebuild()
        {
            Root.Clear();
            _strips.Clear();
            _snapshots.Clear();

            var buses = Ordered(ResonanceAssets.FindAll<AudioBus>());
            if (buses.Count == 0)
            {
                Root.Add(Empty());
                return;
            }

            Root.Add(Ui.Help("    Faders change the bus asset. Mute and solo only last for this session. Meters show what is playing now."));
            var strips = Ui.Box("rs-strips");
            foreach (var bus in buses)
            {
                var strip = new BusStrip(bus, _effects.Show);
                _strips.Add(strip);
                strips.Add(strip.Element);
            }
            Root.Add(strips);
            _effects.Show(buses.Contains(_effects.Bus) ? _effects.Bus : null);
            Root.Add(_effects.Element);

            var snapshots = Ui.Box("rs-snapshots");
            snapshots.Add(Ui.GroupTitle(Glyphs.Snapshot, "Mix Snapshots"));
            foreach (var snapshot in ResonanceAssets.FindAll<MixSnapshot>())
            {
                var row = new SnapshotRow(snapshot);
                _snapshots.Add(row);
                snapshots.Add(row.Element);
            }
            if (_snapshots.Count == 0)
                snapshots.Add(Ui.Help("No snapshot yet. A snapshot lowers or raises several buses at once: pause menu, underwater, low health..."));
            Root.Add(snapshots);
        }

        private VisualElement Empty()
        {
            var empty = Ui.Box("rs-empty");
            empty.Add(Glyphs.AssetImage(nameof(AudioBus), "rs-asset-icon--large"));
            empty.Add(Ui.Styled("No bus yet", "rs-empty-title"));
            empty.Add(Ui.Styled("Buses group sounds so you can set their volume together and duck the music when someone speaks.", "rs-empty-text"));
            empty.Add(Ui.TextButton(Glyphs.Magic, "Create Default Mix", () =>
            {
                ResonanceAssets.CreateDefaultMix();
                Rebuild();
            }, "rs-button--primary"));
            return empty;
        }

        private static List<AudioBus> Ordered(List<AudioBus> buses)
        {
            var ordered = new List<AudioBus>(buses.Count);
            var depth = new Dictionary<AudioBus, int>();
            foreach (var bus in buses)
            {
                var level = 0;
                var current = bus.parent;
                for (var guard = 0; guard < AudioBus.MaxDepth && current != null; guard++)
                {
                    level++;
                    current = current.parent;
                }
                depth[bus] = level;
                ordered.Add(bus);
            }
            ordered.Sort((a, b) => depth[a] != depth[b] ? depth[a].CompareTo(depth[b]) : string.CompareOrdinal(a.name, b.name));
            return ordered;
        }

        #endregion
    }

    internal sealed class BusStrip
    {
        #region Public

        public readonly VisualElement Element;

        public BusStrip(AudioBus bus, System.Action<AudioBus> select)
        {
            _bus = bus;
            Element = Ui.Box("rs-strip");
            Element.Add(Ui.Styled(bus.name, "rs-strip-name"));
            Element.Add(Ui.Styled(bus.parent != null ? $"to {bus.parent.name}" : "output", "rs-strip-parent"));

            var body = Ui.Box("rs-strip-body");
            _fader = new Slider(AudioMath.SilenceDb, 12f, SliderDirection.Vertical) { value = bus.volume };
            _fader.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(bus, "Change bus volume");
                bus.volume = Mathf.Round(evt.newValue * 10f) / 10f;
                EditorUtility.SetDirty(bus);
            });
            body.Add(_fader);
            _meter = Ui.Box("rs-meter");
            _meter.generateVisualContent += DrawMeter;
            body.Add(_meter);
            Element.Add(body);

            _db = Ui.Styled(Ui.Decibels(bus.volume), "rs-strip-db");
            Element.Add(_db);
            var buttons = Ui.Box("rs-strip-buttons");
            _mute = Ui.IconButton(Glyphs.Mute, "Mute (this session)", () => Toggle(true));
            _solo = Ui.IconButton(Glyphs.Solo, "Solo (this session)", () => Toggle(false));
            buttons.Add(_mute);
            buttons.Add(_solo);
            buttons.Add(Ui.IconButton(Glyphs.Filter, "Edit the effects of this bus", () => select(bus)));
            buttons.Add(Ui.IconButton(Glyphs.Settings, "Edit the bus", () => StudioWindow.Open(bus)));
            Element.Add(buttons);
            _effects = Ui.Styled(string.Empty, "rs-strip-effects");
            _effects.RegisterCallback<ClickEvent>(_ => select(bus));
            Element.Add(_effects);
            _info = Ui.Styled(DuckInfo(bus), "rs-strip-info");
            Element.Add(_info);
        }

        public void Refresh(ResonanceEngine engine, AudioBus selected)
        {
            if (_bus == null)
                return;
            Element.EnableInClassList("rs-strip--selected", _bus == selected);
            _effects.text = EffectsInfo(_bus, engine);
            if (_fader.focusController?.focusedElement != _fader)
                _fader.SetValueWithoutNotify(_bus.volume);
            var state = engine != null && engine.Mixer.Buses.TryGetValue(_bus, out var found) ? found : null;
            var level = state?.Meter ?? 0f;
            var peak = state?.Peak ?? 0f;
            if (!level.Equals(_level) || !peak.Equals(_peak))
            {
                _level = level;
                _peak = peak;
                _meter.MarkDirtyRepaint();
            }
            var duck = Mathf.Round((state?.Duck ?? 0f) * 10f) / 10f;
            if (!duck.Equals(_shownDuck) || !_bus.volume.Equals(_shownVolume))
            {
                _shownDuck = duck;
                _shownVolume = _bus.volume;
                _db.text = duck < -0.1f ? $"{Ui.Decibels(_bus.volume)}  ({duck:0.0})" : Ui.Decibels(_bus.volume);
            }
            _mute.EnableInClassList("rs-button--mute-on", _bus.mute || (state?.Muted ?? false));
            _solo.EnableInClassList("rs-button--solo-on", state?.Soloed ?? false);
        }

        #endregion


        #region Private and Protected

        private readonly AudioBus _bus;
        private readonly Slider _fader;
        private readonly VisualElement _meter;
        private readonly Label _db;
        private readonly Label _info;
        private readonly Label _effects;
        private readonly Button _mute;
        private readonly Button _solo;
        private float _level = -1f;
        private float _peak = -1f;
        private float _shownDuck = float.NaN;
        private float _shownVolume = float.NaN;

        private void Toggle(bool mute)
        {
            var engine = Resonance.Engine;
            if (engine == null)
                return;
            var state = engine.Mixer.State(_bus);
            if (mute)
                state.Muted = !state.Muted;
            else
                state.Soloed = !state.Soloed;
        }

        private static string DuckInfo(AudioBus bus)
        {
            if (bus.ducking.Count == 0)
                return string.Empty;
            var names = new List<string>();
            foreach (var rule in bus.ducking)
            {
                if (rule?.target != null)
                    names.Add($"{rule.target.name} {rule.amount:0} dB");
            }
            return names.Count == 0 ? string.Empty : "ducks " + string.Join(", ", names);
        }

        private static string EffectsInfo(AudioBus bus, ResonanceEngine engine)
        {
            var text = string.Empty;
            for (var i = 0; i < bus.effects.Count && i < BusEffects.MaxEffects; i++)
            {
                var effect = bus.effects[i];
                if (effect != null)
                    text += (text.Length > 0 ? "\n" : string.Empty) + (effect.bypass ? $"({BusEffects.Label(effect.type)})" : BusEffects.Label(effect.type));
            }
            var reduction = engine != null ? engine.Mixer.GainReduction(bus) : 0f;
            return reduction > 0.05f ? $"{text}\nGR -{reduction:0.0} dB" : text;
        }

        private static Color MeterColor(float decibels)
        {
            if (decibels > -1f)
                return new Color(0.89f, 0.38f, 0.38f);
            if (decibels > -6f)
                return new Color(0.87f, 0.67f, 0.28f);
            return new Color(0.38f, 0.75f, 0.52f);
        }

        private void DrawMeter(MeshGenerationContext context)
        {
            var rect = _meter.contentRect;
            var painter = context.painter2D;
            var level = Mathf.Clamp01((AudioMath.ToDecibels(_level) + 60f) / 66f);
            var peak = Mathf.Clamp01((AudioMath.ToDecibels(_peak) + 60f) / 66f);
            painter.fillColor = MeterColor(AudioMath.ToDecibels(_level));
            painter.BeginPath();
            painter.MoveTo(new Vector2(0f, rect.height));
            painter.LineTo(new Vector2(rect.width, rect.height));
            painter.LineTo(new Vector2(rect.width, rect.height * (1f - level)));
            painter.LineTo(new Vector2(0f, rect.height * (1f - level)));
            painter.ClosePath();
            painter.Fill();
            painter.strokeColor = new Color(0.87f, 0.87f, 0.9f, 0.8f);
            painter.lineWidth = 1f;
            painter.BeginPath();
            painter.MoveTo(new Vector2(0f, rect.height * (1f - peak)));
            painter.LineTo(new Vector2(rect.width, rect.height * (1f - peak)));
            painter.Stroke();
        }

        #endregion
    }

    internal sealed class SnapshotRow
    {
        public readonly VisualElement Element;

        public SnapshotRow(MixSnapshot snapshot)
        {
            _snapshot = snapshot;
            Element = Ui.Box("rs-preview-row");
            Element.Add(Glyphs.AssetImage(nameof(MixSnapshot), "rs-asset-icon"));
            Element.Add(new Label(snapshot.name));
            _button = Ui.TextButton(null, "Apply", Toggle);
            Element.Add(_button);
            Element.Add(Ui.IconButton(Glyphs.Settings, "Edit the snapshot", () => StudioWindow.Open(snapshot), "rs-button--flat"));
        }

        public void Refresh(ResonanceEngine engine)
        {
            var active = engine != null && _snapshot != null && engine.Mixer.IsActive(_snapshot);
            _button.Q<Label>().text = active ? "Remove" : "Apply";
            _button.EnableInClassList("rs-button--on", active);
        }

        private readonly MixSnapshot _snapshot;
        private readonly Button _button;

        private void Toggle()
        {
            var engine = Resonance.Engine;
            if (engine == null || _snapshot == null)
                return;
            if (engine.Mixer.IsActive(_snapshot))
                engine.Mixer.Remove(_snapshot);
            else
                engine.Mixer.Apply(_snapshot);
        }
    }
}

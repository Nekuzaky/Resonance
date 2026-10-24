using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.Resonance.Editor
{
    internal sealed class PreviewPanel : VisualElement
    {
        #region Public

        public PreviewPanel(SoundEvent sound)
        {
            _sound = sound;
            AddToClassList("rs-preview");
            Rebuild();
        }

        public void Rebuild()
        {
            Clear();
            _sliders.Clear();
            Add(Ui.GroupTitle(Glyphs.Playing, "Preview Controls"));
            CollectSyncs();
            if (_languages.Count > 0)
                AddLanguage();
            if (_parameters.Count == 0 && _switches.Count == 0 && _languages.Count == 0)
            {
                Add(Ui.Help("This sound uses no parameter or switch. Press Play to hear it."));
                return;
            }
            foreach (var parameter in _parameters)
                AddParameter(parameter);
            foreach (var group in _switches)
                AddSwitch(group);
        }

        public void Refresh()
        {
            var syncs = PreviewEngine.Engine.Syncs;
            foreach (var pair in _sliders)
            {
                if (pair.Key != null && pair.Value.panel?.focusController?.focusedElement != pair.Value)
                    pair.Value.SetValueWithoutNotify(syncs.GetParameter(pair.Key, null));
            }
        }

        #endregion


        #region Private and Protected

        private readonly SoundEvent _sound;
        private readonly List<SoundParameter> _parameters = new();
        private readonly List<SwitchGroup> _switches = new();
        private readonly List<string> _languages = new();
        private readonly Dictionary<SoundParameter, Slider> _sliders = new();

        private void CollectSyncs()
        {
            _parameters.Clear();
            _switches.Clear();
            _languages.Clear();
            foreach (var modulation in _sound.modulations)
            {
                if (modulation?.parameter != null && !_parameters.Contains(modulation.parameter))
                    _parameters.Add(modulation.parameter);
            }
            var walker = new NodeWalker(_sound.root);
            for (var guard = 0; guard < NodeWalker.MaxNodes && walker.Next(out var node); guard++)
            {
                if (node is BlendNode { parameter: not null } blend && !_parameters.Contains(blend.parameter))
                    _parameters.Add(blend.parameter);
                if (node is SwitchNode { group: not null } switchNode && !_switches.Contains(switchNode.group))
                    _switches.Add(switchNode.group);
                if (node is DialogueNode dialogue)
                    CollectLanguages(dialogue);
            }
        }

        private void CollectLanguages(DialogueNode dialogue)
        {
            foreach (var line in dialogue.lines)
            {
                if (line != null && !string.IsNullOrEmpty(line.language) && !_languages.Contains(line.language))
                    _languages.Add(line.language);
            }
        }

        private void AddLanguage()
        {
            var row = Ui.Box("rs-preview-row");
            row.Add(Glyphs.Icon(Glyphs.Language, "rs-icon--small", "rs-icon--dim"));
            row.Add(new Label("Language"));
            var engine = PreviewEngine.Engine;
            var dropdown = new DropdownField(new List<string>(_languages), 0);
            dropdown.SetValueWithoutNotify(_languages.Contains(engine.Language) ? engine.Language : _languages[0]);
            engine.Language = dropdown.value;
            dropdown.RegisterValueChangedCallback(evt => PreviewEngine.Engine.Language = evt.newValue);
            row.Add(dropdown);
            Add(row);
        }

        private void AddParameter(SoundParameter parameter)
        {
            var row = Ui.Box("rs-preview-row");
            row.Add(Glyphs.Icon(Glyphs.Parameter, "rs-icon--small", "rs-icon--dim"));
            row.Add(new Label(parameter.name));
            var low = Mathf.Min(parameter.min, parameter.max);
            var high = Mathf.Max(parameter.min, parameter.max);
            var slider = new Slider(low, high) { showInputField = true };
            slider.SetValueWithoutNotify(PreviewEngine.Engine.Syncs.GetParameter(parameter, null));
            slider.RegisterValueChangedCallback(evt => PreviewEngine.Engine.Syncs.SetParameter(parameter, evt.newValue, null, false));
            row.Add(slider);
            _sliders[parameter] = slider;
            Add(row);
        }

        private void AddSwitch(SwitchGroup group)
        {
            var row = Ui.Box("rs-preview-row");
            row.Add(Glyphs.Icon(Glyphs.SwitchGroup, "rs-icon--small", "rs-icon--dim"));
            row.Add(new Label(group.name));
            var dropdown = new DropdownField(new List<string>(group.values), 0);
            dropdown.SetValueWithoutNotify(PreviewEngine.Engine.Syncs.GetSwitch(group, null));
            dropdown.RegisterValueChangedCallback(evt => PreviewEngine.Engine.Syncs.SetSwitch(group, evt.newValue, null));
            row.Add(dropdown);
            Add(row);
        }

        #endregion
    }

    internal sealed class WaveformView : VisualElement
    {
        #region Public

        public const int Columns = 480;

        public WaveformView()
        {
            AddToClassList("rs-waveform");
            generateVisualContent += Draw;
        }

        public void SetClip(AudioClip clip, List<ClipMarker> markers = null)
        {
            _peaks = Peaks(clip);
            _length = clip != null ? clip.length : 0f;
            _markers = markers;
            MarkDirtyRepaint();
        }

        #endregion


        #region Private and Protected

        private static readonly Dictionary<AudioClip, float[]> Cache = new();
        private static readonly Color Wave = new(47f / 255f, 169f / 255f, 184f / 255f, 1f);

        private float[] _peaks;
        private float _length;
        private List<ClipMarker> _markers;
        private static readonly Color MarkerColor = new(0.87f, 0.67f, 0.28f, 1f);

        private static float[] Peaks(AudioClip clip)
        {
            if (clip == null)
                return null;
            if (Cache.TryGetValue(clip, out var cached))
                return cached;

            var peaks = new float[Columns];
            if (!clip.preloadAudioData && clip.loadState != AudioDataLoadState.Loaded)
                clip.LoadAudioData();
            var channels = Mathf.Max(1, clip.channels);
            var framesPerColumn = Mathf.Max(1, clip.samples / Columns);
            var read = Mathf.Min(framesPerColumn, 1024);
            var buffer = new float[read * channels];
            for (var column = 0; column < Columns; column++)
            {
                var offset = column * framesPerColumn;
                if (offset + read > clip.samples || !clip.GetData(buffer, offset))
                    break;
                var peak = 0f;
                foreach (var sample in buffer)
                    peak = Mathf.Max(peak, Mathf.Abs(sample));
                peaks[column] = peak;
            }
            Cache[clip] = peaks;
            return peaks;
        }

        private void Draw(MeshGenerationContext context)
        {
            if (_peaks == null)
                return;
            var rect = contentRect;
            var painter = context.painter2D;
            var middle = rect.height * 0.5f;
            var step = rect.width / _peaks.Length;
            painter.strokeColor = Wave;
            painter.lineWidth = Mathf.Max(1f, step * 0.8f);
            painter.BeginPath();
            for (var i = 0; i < _peaks.Length; i++)
            {
                var x = i * step + step * 0.5f;
                var height = Mathf.Max(0.5f, _peaks[i] * middle * 0.95f);
                painter.MoveTo(new Vector2(x, middle - height));
                painter.LineTo(new Vector2(x, middle + height));
            }
            painter.Stroke();
            DrawMarkers(painter, rect);
        }

        private void DrawMarkers(Painter2D painter, Rect rect)
        {
            if (_markers == null || _length <= 0f)
                return;
            painter.strokeColor = MarkerColor;
            painter.lineWidth = 2f;
            painter.BeginPath();
            foreach (var marker in _markers)
            {
                if (marker == null)
                    continue;
                var x = Mathf.Clamp01(marker.time / _length) * rect.width;
                painter.MoveTo(new Vector2(x, 0f));
                painter.LineTo(new Vector2(x, rect.height));
            }
            painter.Stroke();
        }

        #endregion
    }
}

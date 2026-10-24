using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.Resonance.Editor
{
    internal sealed class ProfilerView : IStudioView
    {
        #region Public

        public VisualElement Root { get; }

        public ProfilerView()
        {
            Root = Ui.Box("rs-profiler");
            var stats = Ui.Box("rs-stats");
            _instances = Stat(stats, "Sounds playing");
            _real = Stat(stats, "Real voices");
            _virtual = Stat(stats, "Virtual voices");
            _pool = Stat(stats, "Audio sources pooled");
            _cost = Stat(stats, "Update cost (ms)");
            _plays = Stat(stats, "Played / rejected");
            _clips = Stat(stats, "Clips held by banks");
            _onDemand = Stat(stats, "Loaded on demand");
            _reverb = Stat(stats, "Reverb around listener");
            Root.Add(stats);

            var toolbar = Ui.Box("rs-preview-row");
            toolbar.Add(Ui.TextButton(Glyphs.Stop, "Stop All", () => Resonance.StopAll()));
            toolbar.Add(Ui.TextButton(Glyphs.Remove, "Clear Log", () => Resonance.Stats?.ClearLog()));
            _liveButton = Ui.TextButton(Glyphs.Playing, "Live", GoLive);
            toolbar.Add(_liveButton);
            toolbar.Add(Ui.TextButton(Glyphs.History, "Clear History", () =>
            {
                _history.Clear();
                GoLive();
            }));
            _cursorLabel = Ui.Styled("Live. Click or drag on the history to look back up to 60 seconds.", "rs-node-tag");
            toolbar.Add(_cursorLabel);
            Root.Add(toolbar);
            _timeline = new ProfilerTimeline(_history);
            _timeline.Picked += _ => _paused = true;
            Root.Add(_timeline);

            var columns = Ui.Box("rs-columns");
            columns.Add(Column("Active Sounds", Header("Event", 150, "Emitter", 110, "Bus", 70, "Voices", 50, "Distance", 60, "Occluded", 60, "Level", 80), out _active));
            columns.Add(Column("Event Log", Header("Time", 60, "Event", 160, "Emitter", 120, "Result", 90), out _log));
            Root.Add(columns);

            var syncs = Ui.Box("rs-columns");
            syncs.style.flexGrow = 0f;
            syncs.style.height = 140f;
            syncs.Add(Column("Game States", Header("Group", 160, "State", 160), out _states));
            syncs.Add(Column("Global Parameters", Header("Parameter", 160, "Value", 160), out _parameters));
            Root.Add(syncs);
        }

        public void Sample()
        {
            var engine = Resonance.Engine;
            if (engine == null || _paused)
                return;
            _history.Record(engine, UnityEditor.EditorApplication.timeSinceStartup);
            _timeline.MarkDirtyRepaint();
        }

        public void Refresh()
        {
            var engine = Resonance.Engine;
            if (engine == null)
                return;
            _liveButton.SetEnabled(_paused);
            _liveButton.EnableInClassList("rs-button--on", !_paused);
            if (_paused && _timeline.Cursor >= 0 && _timeline.Cursor < _history.Count)
            {
                ShowSample(_history[_timeline.Cursor]);
                return;
            }
            var stats = engine.Stats;
            _instances.text = stats.Instances.ToString();
            _real.text = $"{stats.RealVoices} / {engine.MaxVoices}";
            _virtual.text = stats.VirtualVoices.ToString();
            _pool.text = stats.PooledVoices.ToString();
            _cost.text = $"{stats.TickMilliseconds:0.000}";
            _plays.text = $"{stats.TotalPlays} / {stats.TotalRejected}";
            _clips.text = engine.Memory.Clips.Count.ToString();
            _onDemand.text = stats.OnDemandLoads.ToString();
            _onDemand.EnableInClassList("rs-cell--warn", stats.OnDemandLoads > 0);
            var zone = engine.Reverb.Dominant;
            _reverb.text = zone != null ? $"{zone.preset} {engine.Reverb.Wet * 100f:0}%" : "dry";
            if (_history.Count > 0)
                RenderSounds(_history[_history.Count - 1]);
            RefreshLog(stats);
            RefreshSyncs(engine);
        }

        public void ProjectChanged()
        {
        }

        #endregion


        #region Private and Protected

        private readonly Label _instances;
        private readonly Label _real;
        private readonly Label _virtual;
        private readonly Label _pool;
        private readonly Label _cost;
        private readonly Label _plays;
        private readonly Label _clips;
        private readonly Label _onDemand;
        private readonly Label _reverb;
        private readonly ScrollView _active;
        private readonly ScrollView _log;
        private readonly ScrollView _states;
        private readonly ScrollView _parameters;
        private int _logVersion = -1;
        private readonly ProfilerHistory _history = new();
        private readonly ProfilerTimeline _timeline;
        private readonly Button _liveButton;
        private readonly Label _cursorLabel;
        private bool _paused;

        private void GoLive()
        {
            _paused = false;
            _timeline.Cursor = -1;
            _timeline.MarkDirtyRepaint();
            _cursorLabel.text = "Live. Click or drag on the history to look back up to 60 seconds.";
        }

        private void ShowSample(ProfilerSample sample)
        {
            var newest = _history[_history.Count - 1];
            _cursorLabel.text = $"Paused {newest.Time - sample.Time:0.0} s before the last sample. Press Live to follow the game again.";
            _instances.text = sample.Instances.ToString();
            _real.text = sample.Real.ToString();
            _virtual.text = sample.Virtual.ToString();
            _cost.text = $"{sample.Cost:0.000}";
            RenderSounds(sample);
        }

        private static Label Stat(VisualElement parent, string label)
        {
            var box = Ui.Box("rs-stat");
            var value = Ui.Styled("0", "rs-stat-value");
            box.Add(value);
            box.Add(Ui.Styled(label, "rs-stat-label"));
            parent.Add(box);
            return value;
        }

        private static VisualElement Header(params object[] columns)
        {
            var header = Ui.Box("rs-table-header");
            for (var i = 0; i + 1 < columns.Length; i += 2)
                header.Add(Cell((string)columns[i], (int)columns[i + 1]));
            return header;
        }

        private static Label Cell(string text, float width, string className = null)
        {
            var cell = Ui.Styled(text, "rs-cell");
            cell.style.width = width;
            if (className != null)
                cell.AddToClassList(className);
            return cell;
        }

        private static VisualElement Column(string title, VisualElement header, out ScrollView rows)
        {
            var column = Ui.Box("rs-column");
            column.Add(Ui.Styled(title, "rs-column-title"));
            column.Add(header);
            rows = new ScrollView { style = { flexGrow = 1f } };
            column.Add(rows);
            return column;
        }

        private void RenderSounds(ProfilerSample sample)
        {
            _active.Clear();
            foreach (var sound in sample.Sounds)
            {
                var row = Ui.Box("rs-table-row");
                row.Add(Cell(sound.Event != null ? sound.Event.name : "(deleted)", 150));
                row.Add(Cell(sound.Emitter, 110, "rs-cell--dim"));
                row.Add(Cell(sound.Bus, 70, "rs-cell--dim"));
                row.Add(Cell(sound.Virtual ? "virtual" : sound.Voices.ToString(), 50, sound.Virtual ? "rs-cell--warn" : null));
                row.Add(Cell(sound.Spatial ? $"{sound.Distance:0.0} m" : "2D", 60, "rs-cell--dim"));
                row.Add(Cell(sound.Occlusion > 0.01f ? $"{sound.Occlusion * 100f:0}%" : "-", 60, sound.Occlusion > 0.01f ? "rs-cell--warn" : "rs-cell--dim"));
                row.Add(Level(sound.Level));
                _active.Add(row);
            }
            if (sample.Sounds.Count == 0)
                _active.Add(Ui.Help("Nothing is playing."));
        }

        private static VisualElement Level(float level)
        {
            var bar = Ui.Box("rs-level");
            bar.style.width = 80f;
            var fill = Ui.Box("rs-level-fill");
            fill.style.width = Mathf.Clamp01((AudioMath.ToDecibels(level) + 60f) / 60f) * 80f;
            bar.Add(fill);
            return bar;
        }

        private void RefreshLog(ResonanceStats stats)
        {
            if (stats.LogVersion == _logVersion)
                return;
            _logVersion = stats.LogVersion;
            _log.Clear();
            var count = Mathf.Min(stats.LogCount, 120);
            for (var i = 0; i < count; i++)
            {
                var entry = stats.LogAt(i);
                var row = Ui.Box("rs-table-row");
                row.Add(Cell($"{entry.Time:0.00}", 60, "rs-cell--dim"));
                row.Add(Cell(entry.Event, 160));
                row.Add(Cell(entry.Emitter, 120, "rs-cell--dim"));
                row.Add(Cell(Describe(entry.Result), 90, ClassOf(entry.Result)));
                _log.Add(row);
            }
        }

        private void RefreshSyncs(ResonanceEngine engine)
        {
            _states.Clear();
            foreach (var pair in engine.Syncs.States)
            {
                var row = Ui.Box("rs-table-row");
                row.Add(Cell(pair.Key != null ? pair.Key.name : "?", 160));
                row.Add(Cell(pair.Value, 160, "rs-cell--good"));
                _states.Add(row);
            }
            _parameters.Clear();
            foreach (var pair in engine.Syncs.GlobalParameters)
            {
                var row = Ui.Box("rs-table-row");
                row.Add(Cell(pair.Key != null ? pair.Key.name : "?", 160));
                row.Add(Cell($"{pair.Value.Current:0.###}", 160, "rs-cell--good"));
                _parameters.Add(row);
            }
        }

        private static string Describe(PlayResult result)
        {
            switch (result)
            {
                case PlayResult.Stolen: return "played, stole";
                case PlayResult.RejectedLimit: return "instance limit";
                case PlayResult.RejectedVoices: return "no free voice";
                case PlayResult.Cooldown: return "cooldown";
                case PlayResult.Empty: return "nothing to play";
                default: return "played";
            }
        }

        private static string ClassOf(PlayResult result)
        {
            switch (result)
            {
                case PlayResult.Played: return "rs-cell--good";
                case PlayResult.Stolen:
                case PlayResult.Cooldown: return "rs-cell--warn";
                default: return "rs-cell--bad";
            }
        }

        #endregion
    }
}

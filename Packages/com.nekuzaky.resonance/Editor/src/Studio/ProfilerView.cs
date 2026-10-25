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
            toolbar.Add(Ui.Spacer());
            _sourceButton = Ui.TextButton(Glyphs.Remote, "Show Player", ToggleSource);
            _sourceButton.tooltip = "Profile a Development build connected to this editor (Build Settings > Development Build, Autoconnect Profiler).";
            toolbar.Add(_sourceButton);
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

            _nothing = Ui.Help("Nothing is playing.");
            _active.Add(_nothing);
            _soundRows = new ProfilerTable(_active, true, 150, 110, 70, 50, 60, 60);
            _logRows = new ProfilerTable(_log, false, 60, 160, 120, 90);
            _stateRows = new ProfilerTable(_states, false, 160, 160);
            _parameterRows = new ProfilerTable(_parameters, false, 160, 160);
        }

        public void Sample()
        {
            if (_paused)
                return;
            var now = UnityEditor.EditorApplication.timeSinceStartup;
            if (_remote)
            {
                if (RemoteSession.Version == _remoteVersion)
                    return;
                _remoteVersion = RemoteSession.Version;
                _history.RecordRemote(RemoteSession.Frame, now);
            }
            else
            {
                var engine = Resonance.Engine;
                if (engine == null)
                    return;
                _history.Record(engine, now);
            }
            _timeline.MarkDirtyRepaint();
        }

        public void Refresh()
        {
            RefreshSource();
            if (++_refreshes % ProfilerHistory.Capacity == 0)
                _names.Clear();
            var engine = Resonance.Engine;
            if (engine == null && !_remote)
                return;
            _liveButton.SetEnabled(_paused);
            _liveButton.EnableInClassList("rs-button--on", !_paused);
            if (_paused && _timeline.Cursor >= 0 && _timeline.Cursor < _history.Count)
            {
                ShowSample(_history[_timeline.Cursor]);
                return;
            }
            if (_remote)
            {
                ShowRemote(RemoteSession.Frame);
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

        public void ProjectChanged() => _names.Clear();

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
        private readonly Label _nothing;
        private readonly ProfilerTable _soundRows;
        private readonly ProfilerTable _logRows;
        private readonly ProfilerTable _stateRows;
        private readonly ProfilerTable _parameterRows;
        private readonly NameCache _names = new();
        private int _refreshes;
        private readonly ProfilerHistory _history = new();
        private readonly ProfilerTimeline _timeline;
        private readonly Button _liveButton;
        private readonly Label _cursorLabel;
        private bool _paused;
        private bool _remote;
        private int _remoteVersion = -1;
        private readonly Button _sourceButton;

        private void ToggleSource()
        {
            _remote = !_remote && RemoteSession.FramesReceived > 0;
            _history.Clear();
            GoLive();
        }

        private void RefreshSource()
        {
            var live = RemoteSession.Live;
            _sourceButton.style.display = live || _remote ? DisplayStyle.Flex : DisplayStyle.None;
            _sourceButton.Q<Label>().text = _remote ? (live ? $"Player: {RemoteSession.Frame.Platform}" : "Player disconnected") : "Show Player";
            _sourceButton.EnableInClassList("rs-button--on", _remote && live);
        }

        private void ShowRemote(RemoteFrame frame)
        {
            _instances.text = frame.Instances.ToString();
            _real.text = frame.Real.ToString();
            _virtual.text = frame.Virtual.ToString();
            _pool.text = frame.Pooled.ToString();
            _cost.text = $"{frame.Cost:0.000}";
            _plays.text = $"{frame.Plays} / {frame.Rejected}";
            _clips.text = "-";
            _onDemand.text = frame.OnDemand.ToString();
            _reverb.text = string.IsNullOrEmpty(frame.Reverb) ? "dry" : $"{frame.Reverb} {frame.Wet * 100f:0}%";
            _cursorLabel.text = $"Profiling the {frame.Platform} player. Click the history to look back.";
            if (_history.Count > 0)
                RenderSounds(_history[_history.Count - 1]);
        }

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

        private static Label Cell(string text, float width)
        {
            var cell = Ui.Styled(text, "rs-cell");
            cell.style.width = width;
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
            _soundRows.Begin();
            foreach (var sound in sample.Sounds)
            {
                var row = _soundRows.Next();
                row.Set(0, sound.Event);
                row.Set(1, sound.Emitter, "rs-cell--dim");
                row.Set(2, sound.Bus, "rs-cell--dim");
                if (row.Changed(3, sound.Virtual ? -1f : sound.Voices))
                    row.Set(3, sound.Virtual ? "virtual" : sound.Voices.ToString(), sound.Virtual ? "rs-cell--warn" : null);
                if (row.Changed(4, sound.Spatial ? Mathf.Round(sound.Distance * 10f) : -1f))
                    row.Set(4, sound.Spatial ? $"{sound.Distance:0.0} m" : "2D", "rs-cell--dim");
                var occluded = sound.Occlusion > 0.01f;
                if (row.Changed(5, occluded ? Mathf.Round(sound.Occlusion * 100f) : -1f))
                    row.Set(5, occluded ? $"{sound.Occlusion * 100f:0}%" : "-", occluded ? "rs-cell--warn" : "rs-cell--dim");
                row.SetLevel(sound.Level);
            }
            _soundRows.End();
            _nothing.style.display = sample.Sounds.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void RefreshLog(ResonanceStats stats)
        {
            if (stats.LogVersion == _logVersion)
                return;
            _logVersion = stats.LogVersion;
            _logRows.Begin();
            var count = Mathf.Min(stats.LogCount, 120);
            for (var i = 0; i < count; i++)
            {
                var entry = stats.LogAt(i);
                var row = _logRows.Next();
                if (row.Changed(0, entry.Time))
                    row.Set(0, $"{entry.Time:0.00}", "rs-cell--dim");
                row.Set(1, _names.Of(entry.Sound) ?? "(deleted)");
                row.Set(2, _names.Of(entry.EmitterObject) ?? (entry.HadEmitter ? "(destroyed)" : "(world)"), "rs-cell--dim");
                row.Set(3, Describe(entry.Result), ClassOf(entry.Result));
            }
            _logRows.End();
        }

        private void RefreshSyncs(ResonanceEngine engine)
        {
            _stateRows.Begin();
            foreach (var pair in engine.Syncs.States)
            {
                var row = _stateRows.Next();
                row.Set(0, _names.Of(pair.Key) ?? "?");
                row.Set(1, pair.Value, "rs-cell--good");
            }
            _stateRows.End();
            _parameterRows.Begin();
            foreach (var pair in engine.Syncs.GlobalParameters)
            {
                var row = _parameterRows.Next();
                row.Set(0, _names.Of(pair.Key) ?? "?");
                if (row.Changed(1, pair.Value.Current))
                    row.Set(1, $"{pair.Value.Current:0.###}", "rs-cell--good");
            }
            _parameterRows.End();
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

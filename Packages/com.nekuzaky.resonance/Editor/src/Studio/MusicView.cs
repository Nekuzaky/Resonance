using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.Resonance.Editor
{
    internal sealed class MusicView : IStudioView
    {
        #region Public

        public VisualElement Root { get; }

        public MusicView()
        {
            Root = new ScrollView(ScrollViewMode.Vertical);
            Root.AddToClassList("rs-music");
            _picker = new ObjectField("Music Track") { objectType = typeof(MusicTrack), allowSceneObjects = false };
            _picker.RegisterValueChangedCallback(evt => Select(evt.newValue as MusicTrack));
            Root.Add(_picker);
            _content = new VisualElement();
            Root.Add(_content);
            var first = ResonanceAssets.FindAll<MusicTrack>();
            Select(first.Count > 0 ? first[0] : null);
        }

        public void Select(MusicTrack track)
        {
            _track = track;
            _picker.SetValueWithoutNotify(track);
            Rebuild();
        }

        public void Refresh()
        {
            if (_track == null || _position == null)
                return;
            var music = Resonance.Engine?.Music;
            var playing = music != null && music.Track == _track;
            var bar = 0;
            var beat = 0;
            var hasPosition = playing && music.Position(out bar, out beat, out _);
            _position.text = hasPosition ? $"Bar {bar}  ·  Beat {beat}" : playing ? "Starting..." : "Stopped";
            for (var i = 0; i < _beats.Count; i++)
                _beats[i].EnableInClassList("rs-beat--on", hasPosition && i == beat - 1);
            foreach (var pair in _cards)
            {
                pair.Value.EnableInClassList("rs-card--current", playing && music.Current == pair.Key);
                pair.Value.EnableInClassList("rs-card--pending", playing && music.Pending == pair.Key);
            }
            var stateGroup = _track.stateGroup;
            if (_stateLabel != null && stateGroup != null)
                _stateLabel.text = $"{stateGroup.name}: {Resonance.GetState(stateGroup)}";
        }

        public void ProjectChanged()
        {
            if (!ReferenceEquals(_track, null) && _track == null)
                Select(null);
        }

        #endregion


        #region Private and Protected

        private readonly ObjectField _picker;
        private readonly VisualElement _content;
        private readonly List<VisualElement> _beats = new();
        private readonly Dictionary<MusicSegment, VisualElement> _cards = new();
        private MusicTrack _track;
        private Label _position;
        private Label _stateLabel;

        private void Rebuild()
        {
            _content.Clear();
            _beats.Clear();
            _cards.Clear();
            _position = null;
            _stateLabel = null;
            if (_track == null)
            {
                _content.Add(Empty());
                return;
            }
            _content.Add(Transport());
            _content.Add(Ui.GroupTitle(Glyphs.Note, "Segments"));
            _content.Add(Ui.Help($"Click a segment to move to it at the {Sync(_track.transitionSync)}. Gold border: waiting for the boundary."));
            _content.Add(Segments());
            AddStates();
            AddStingers();
            _content.Add(Ui.GroupTitle(Glyphs.Settings, "Track Settings"));
            _content.Add(new InspectorElement(_track));
        }

        private VisualElement Empty()
        {
            var empty = Ui.Box("rs-empty");
            empty.Add(Glyphs.AssetImage(nameof(MusicTrack), "rs-asset-icon--large"));
            empty.Add(Ui.Styled("Interactive music", "rs-empty-title"));
            empty.Add(Ui.Styled("A Music Track holds segments (intro, explore, combat...) and moves between them on the beat " +
                                "when a game state changes.", "rs-empty-text"));
            empty.Add(Ui.TextButton(Glyphs.Add, "New Music Track", () => Select(ResonanceAssets.Create<MusicTrack>("New Music Track")), "rs-button--primary"));
            return empty;
        }

        private VisualElement Transport()
        {
            var transport = Ui.Box("rs-transport");
            transport.Add(Ui.TextButton(Glyphs.Play, "Play", () => Resonance.PlayMusic(_track), "rs-button--primary"));
            transport.Add(Ui.TextButton(Glyphs.Stop, "Stop", () => Resonance.StopMusic(_track.crossfade)));
            _position = Ui.Styled("Stopped", "rs-position");
            transport.Add(_position);
            var beats = Ui.Box("rs-beats");
            for (var i = 0; i < _track.beatsPerBar; i++)
            {
                var dot = Ui.Box("rs-beat");
                dot.EnableInClassList("rs-beat--downbeat", i == 0);
                _beats.Add(dot);
                beats.Add(dot);
            }
            transport.Add(beats);
            transport.Add(Ui.Spacer());
            transport.Add(Ui.Styled($"{_track.bpm:0.#} BPM  ·  {_track.beatsPerBar}/4", "rs-node-tag"));
            return transport;
        }

        private VisualElement Segments()
        {
            var cards = Ui.Box("rs-cards");
            foreach (var segment in _track.segments)
            {
                if (segment == null)
                    continue;
                var card = Ui.Box("rs-card");
                card.Add(Ui.Styled(segment.name, "rs-card-title"));
                var length = _track.LengthOf(segment);
                var bars = _track.BarLength > 0 ? length / _track.BarLength : 0;
                card.Add(Ui.Styled($"{bars:0.#} bars  ·  {length:0.0} s", "rs-card-detail"));
                card.Add(Ui.Styled(string.IsNullOrEmpty(segment.next) ? "then loops" : $"then {segment.next}", "rs-card-detail"));
                if (segment.clip == null)
                    card.Add(Ui.Styled("no clip", "rs-cell--bad"));
                var target = segment;
                card.RegisterCallback<ClickEvent>(_ => Jump(target));
                _cards[segment] = card;
                cards.Add(card);
            }
            if (_cards.Count == 0)
                cards.Add(Ui.Help("No segment yet. Add segments in the Track Settings below."));
            return cards;
        }

        private void Jump(MusicSegment segment)
        {
            var music = Resonance.Engine?.Music;
            if (music == null)
                return;
            if (music.Track != _track)
                Resonance.PlayMusic(_track);
            music.Request(segment, _track.transitionSync);
        }

        private void AddStates()
        {
            var group = _track.stateGroup;
            if (group == null)
                return;
            _content.Add(Ui.GroupTitle(Glyphs.StateGroup, "Game States"));
            _stateLabel = Ui.Styled(string.Empty, "rs-help");
            _content.Add(_stateLabel);
            var row = Ui.Box("rs-cards");
            foreach (var state in group.states)
                row.Add(Ui.TextButton(Glyphs.StateGroup, state, () => Resonance.SetState(group, state)));
            _content.Add(row);
        }

        private void AddStingers()
        {
            if (_track.stingers.Count == 0)
                return;
            _content.Add(Ui.GroupTitle(Glyphs.Stinger, "Stingers"));
            var row = Ui.Box("rs-cards");
            foreach (var stinger in _track.stingers)
            {
                if (stinger == null)
                    continue;
                var name = stinger.name;
                row.Add(Ui.TextButton(Glyphs.Stinger, name, () => Resonance.PlayStinger(name)));
            }
            _content.Add(row);
        }

        private static string Sync(MusicSync sync)
        {
            switch (sync)
            {
                case MusicSync.Immediate: return "same moment";
                case MusicSync.NextBeat: return "next beat";
                case MusicSync.EndOfSegment: return "end of the segment";
                default: return "next bar";
            }
        }

        #endregion
    }
}

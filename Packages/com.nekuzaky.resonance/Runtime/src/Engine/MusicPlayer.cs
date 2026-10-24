using UnityEngine;

namespace Nekuzaky.Resonance
{
    internal sealed class MusicDeck
    {
        public readonly AudioSource Source;
        public MusicSegment Segment;
        public double Start;
        public double FadeStart;
        public double FadeLength;
        public float FadeFrom;
        public float FadeTo = 1f;
        public bool Active;
        public float Volume;
        public readonly System.Collections.Generic.List<AudioSource> Layers = new();

        private readonly GameObject _host;

        public MusicDeck(Transform parent, string name)
        {
            _host = new GameObject(name) { hideFlags = parent.gameObject.hideFlags };
            _host.transform.SetParent(parent, false);
            Source = NewSource();
        }

        public AudioSource Layer(int index)
        {
            for (var guard = 0; guard < 16 && Layers.Count <= index; guard++)
                Layers.Add(NewSource());
            return Layers[index];
        }

        private AudioSource NewSource()
        {
            var source = _host.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.priority = 0;
            return source;
        }

        public float FadeAt(double time)
        {
            if (FadeLength <= 0.0 || time >= FadeStart + FadeLength)
                return time >= FadeStart ? FadeTo : FadeFrom;
            if (time <= FadeStart)
                return FadeFrom;
            return Mathf.Lerp(FadeFrom, FadeTo, (float)((time - FadeStart) / FadeLength));
        }

        public void FadeTowards(float target, double start, double length, double now)
        {
            FadeFrom = FadeAt(now);
            FadeTo = target;
            FadeStart = start;
            FadeLength = length;
        }

        public void EndAt(double time)
        {
            if (Source != null && Source.isPlaying)
                Source.SetScheduledEndTime(time);
            foreach (var layer in Layers)
            {
                if (layer != null && layer.isPlaying)
                    layer.SetScheduledEndTime(time);
            }
        }

        public void Halt()
        {
            if (Source != null)
                Source.Stop();
            foreach (var layer in Layers)
            {
                if (layer != null)
                    layer.Stop();
            }
            Active = false;
            Segment = null;
        }
    }

    internal sealed partial class MusicPlayer
    {
        #region Public

        public const double Lookahead = 0.1;
        public const double ScheduleAhead = 0.5;

        public MusicTrack Track { get; private set; }

        public bool Metering { get; set; }

        public MusicSegment Current => _decks[_current].Segment;

        public MusicSegment Pending => _pending ? _decks[1 - _current].Segment : null;

        public MusicSegment AfterBridge => _afterBridge;

        internal float LayerVolume(int index)
        {
            var deck = _decks[_current];
            return deck.Active && index < deck.Layers.Count ? deck.Layers[index].volume : -1f;
        }

        public event System.Action<int, int> BeatReached;

        public event System.Action<MusicSegment> SegmentStarted;

        public double PendingTime => _pending ? _decks[1 - _current].Start : 0.0;

        public MusicPlayer(Transform host, SyncStore syncs, BusMixer mixer)
        {
            _syncs = syncs;
            _mixer = mixer;
            _decks = new[] { new MusicDeck(host, "Music A"), new MusicDeck(host, "Music B") };
            _stingers = new[] { new MusicDeck(host, "Stinger A"), new MusicDeck(host, "Stinger B") };
            _syncs.StateChanged += OnStateChanged;
        }

        #endregion


        #region Private and Protected

        private readonly SyncStore _syncs;
        private readonly BusMixer _mixer;
        private readonly MusicDeck[] _decks;
        private readonly MusicDeck[] _stingers;
        private int _current;
        private int _nextStinger;
        private bool _pending;
        private bool _stopping;
        private MusicSegment _bridge;
        private MusicSegment _afterBridge;
        private MusicSegment _announced;
        private int _lastBar = -1;
        private int _lastBeat = -1;
        private double _stopAt;

        #endregion


        #region Main API

        public void Play(MusicTrack track, double now)
        {
            if (track == null)
                return;
            if (track == Track && !_stopping)
                return;

            Stop(track.crossfade);
            Track = track;
            _stopping = false;
            ClearBridge();
            var first = track.ForState(_syncs.GetState(track.stateGroup)) ?? track.First;
            if (first == null)
                return;
            Schedule(first, now + Lookahead, Track.crossfade, now);
        }

        public void Stop(float fade)
        {
            var now = AudioSettings.dspTime;
            if (_pending)
                _decks[1 - _current].Halt();
            foreach (var deck in _decks)
            {
                if (!deck.Active)
                    continue;
                deck.FadeTowards(0f, now, fade, now);
                deck.EndAt(now + Mathf.Max(0f, fade) + 0.05);
            }
            _pending = false;
            ClearBridge();
            _stopping = Track != null;
            _stopAt = now + fade;
            if (fade <= 0f)
                Finish();
        }

        public void RequestNamed(string segmentName, MusicSync sync)
        {
            if (Track != null)
                Request(Track.Find(segmentName), sync);
        }

        public bool Request(MusicSegment segment, MusicSync sync)
        {
            if (Track == null || segment == null || _stopping)
                return false;
            var now = AudioSettings.dspTime;
            Promote(now);
            ClearBridge();
            if (Current == segment)
            {
                if (_pending)
                    CancelPending(now);
                return true;
            }
            var boundary = Boundary(sync, now);
            if (_pending)
            {
                var planned = _decks[1 - _current].Start;
                if (planned >= now && planned < boundary)
                    boundary = planned;
                _decks[1 - _current].Halt();
            }
            _pending = false;
            Schedule(segment, boundary, Track.crossfade, now);
            return true;
        }

        public void RequestThrough(MusicSegment bridge, MusicSegment target, MusicSync sync)
        {
            if (bridge == null || bridge.clip == null)
            {
                Request(target, sync);
                return;
            }
            if (!Request(bridge, sync))
                return;
            _bridge = bridge;
            _afterBridge = target;
        }

        public void PlayStinger(string stingerName)
        {
            var stinger = Track != null ? Track.FindStinger(stingerName) : null;
            if (stinger == null || stinger.clip == null)
                return;

            var now = AudioSettings.dspTime;
            var deck = _stingers[_nextStinger];
            _nextStinger = (_nextStinger + 1) % _stingers.Length;
            deck.Halt();
            deck.Source.clip = stinger.clip;
            deck.Source.outputAudioMixerGroup = Track.bus != null ? Track.bus.ResolveOutput() : null;
            deck.Volume = stinger.volume;
            deck.Source.volume = Gain(stinger.volume);
            deck.Start = Boundary(stinger.sync, now);
            deck.Source.PlayScheduled(deck.Start);
            deck.Active = true;
        }

        public bool Position(out int bar, out int beat, out float beatPhase)
        {
            bar = beat = 0;
            beatPhase = 0f;
            var deck = _decks[_current];
            if (Track == null || !deck.Active)
                return false;

            var elapsed = AudioSettings.dspTime - deck.Start;
            if (elapsed < 0.0)
                return false;
            var beats = elapsed / Track.BeatLength;
            bar = (int)(beats / Track.beatsPerBar) + 1;
            beat = (int)(beats % Track.beatsPerBar) + 1;
            beatPhase = (float)(beats - System.Math.Floor(beats));
            return true;
        }

        #endregion
    }
}

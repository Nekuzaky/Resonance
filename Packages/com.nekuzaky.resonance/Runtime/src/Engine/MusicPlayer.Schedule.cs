namespace Nekuzaky.Resonance
{
    internal sealed partial class MusicPlayer
    {
        #region Main API

        public void Tick(double now)
        {
            if (Track == null)
                return;
            if (_stopping)
            {
                if (now >= _stopAt)
                    Finish();
                else
                    ApplyVolumes(now);
                return;
            }

            Promote(now);

            var deck = _decks[_current];
            if (!_pending && deck.Active)
            {
                var length = Track.LengthOf(deck.Segment);
                if (length > 0.0 && now >= deck.Start + length - ScheduleAhead)
                {
                    var end = deck.Start + length;
                    Schedule(NextAfter(deck.Segment), end > now ? end : now + Lookahead, 0f, now);
                }
            }
            ApplyVolumes(now);
            Announce();
        }

        public double Boundary(MusicSync sync, double now)
        {
            var time = now + Lookahead;
            var deck = _decks[_current];
            if (Track == null || !deck.Active)
                return time;

            switch (sync)
            {
                case MusicSync.NextBeat:
                    return AudioMath.NextBoundary(deck.Start, time, Track.BeatLength);
                case MusicSync.NextBar:
                    return AudioMath.NextBoundary(deck.Start, time, Track.BarLength);
                case MusicSync.EndOfSegment:
                    var length = Track.LengthOf(deck.Segment);
                    return length <= 0.0 ? time : AudioMath.NextBoundary(deck.Start, time, length);
                default:
                    return time;
            }
        }

        #endregion


        #region Private and Protected

        private void Schedule(MusicSegment segment, double time, float crossfade, double now)
        {
            var previous = _decks[_current];
            var hasPrevious = previous.Active;
            var index = hasPrevious ? 1 - _current : _current;
            var deck = _decks[index];

            deck.Halt();
            deck.Segment = segment;
            deck.Start = time;
            deck.FadeFrom = crossfade > 0f ? 0f : 1f;
            deck.FadeTo = 1f;
            deck.FadeStart = time;
            deck.FadeLength = crossfade;
            deck.Active = true;
            deck.Source.clip = segment.clip;
            deck.Source.outputAudioMixerGroup = Track.bus != null ? Track.bus.ResolveOutput() : null;
            deck.Source.volume = 0f;
            if (segment.clip != null)
                deck.Source.PlayScheduled(time);
            StartLayers(deck, segment, time);

            if (!hasPrevious)
            {
                _current = index;
                _pending = false;
                return;
            }
            previous.FadeTowards(0f, time, crossfade, now);
            previous.EndAt(time + crossfade);
            _pending = true;
        }

        private void ApplyVolumes(double now)
        {
            var chain = _mixer.Chain(Track.bus);
            for (var i = 0; i < _decks.Length; i++)
            {
                var deck = _decks[i];
                if (!deck.Active)
                    continue;
                var outgoing = i != _current && !_pending;
                if (outgoing && deck.FadeTo <= 0f && now >= deck.FadeStart + deck.FadeLength)
                {
                    deck.Halt();
                    continue;
                }
                var fade = deck.FadeAt(now);
                deck.Source.volume = UnityEngine.Mathf.Clamp01(Gain(deck.Segment.volume) * fade);
                deck.ApplyEffects(chain);
                Meter(deck.Source, chain);
                LayerVolumes(deck, fade, chain);
            }
            StingerVolumes(now, chain);
        }

        private void Promote(double now)
        {
            if (!_pending || now < _decks[1 - _current].Start)
                return;
            _current = 1 - _current;
            _pending = false;
        }

        private void CancelPending(double now)
        {
            _decks[1 - _current].Halt();
            _pending = false;
            var deck = _decks[_current];
            deck.FadeFrom = 1f;
            deck.FadeTo = 1f;
            deck.FadeLength = 0.0;
            deck.EndAt(now + 86400.0);
        }

        private void ClearBridge()
        {
            _bridge = null;
            _afterBridge = null;
        }

        private void StingerVolumes(double now, EffectChain chain)
        {
            foreach (var stinger in _stingers)
            {
                if (!stinger.Active)
                    continue;
                if (!stinger.Source.isPlaying && now > stinger.Start + 0.1)
                {
                    stinger.Active = false;
                    continue;
                }
                stinger.Source.volume = UnityEngine.Mathf.Clamp01(Gain(stinger.Volume));
                stinger.Effects.Apply(chain);
            }
        }

        private void StartLayers(MusicDeck deck, MusicSegment segment, double time)
        {
            for (var i = 0; i < segment.layers.Count; i++)
            {
                var layer = segment.layers[i];
                var source = deck.Layer(i);
                source.clip = layer?.clip;
                source.outputAudioMixerGroup = deck.Source.outputAudioMixerGroup;
                source.volume = 0f;
                if (source.clip != null)
                    source.PlayScheduled(time);
            }
        }

        private void LayerVolumes(MusicDeck deck, float fade, EffectChain chain)
        {
            var layers = deck.Segment.layers;
            for (var i = 0; i < layers.Count && i < deck.Layers.Count; i++)
            {
                var layer = layers[i];
                if (layer == null)
                    continue;
                var weight = layer.parameter == null ? 1f : AudioMath.BlendWeight(_syncs.GetParameter(layer.parameter, null), layer.range, layer.fade);
                deck.Layers[i].volume = UnityEngine.Mathf.Clamp01(Gain(deck.Segment.volume + layer.volume) * fade * weight);
                Meter(deck.Layers[i], chain);
            }
        }

        private void Meter(UnityEngine.AudioSource source, EffectChain chain)
        {
            if (!source.isPlaying)
                return;
            var envelope = Metering || chain.Compressed ? ClipEnvelopes.At(source.clip, source.timeSamples) : 1f;
            _mixer.CountVoice(Track.bus, (Metering ? envelope : 1f) * source.volume);
            if (!chain.Compressed)
                return;
            var busGain = _mixer.Gain(Track.bus);
            if (busGain > 0.0001f)
                _mixer.Detect(Track.bus, envelope * source.volume / busGain);
        }

        private void Announce()
        {
            if (!Position(out var bar, out var beat, out _))
                return;
            var segment = _decks[_current].Segment;
            if (segment != _announced)
            {
                _announced = segment;
                _lastBar = -1;
                SegmentStarted?.Invoke(segment);
            }
            if (bar == _lastBar && beat == _lastBeat)
                return;
            _lastBar = bar;
            _lastBeat = beat;
            BeatReached?.Invoke(bar, beat);
        }

        private MusicSegment NextAfter(MusicSegment segment)
        {
            if (_bridge != null && segment == _bridge && _afterBridge != null)
            {
                var target = _afterBridge;
                _bridge = null;
                _afterBridge = null;
                return target;
            }
            return Track.Find(segment.next) ?? segment;
        }

        private float Gain(float decibels) => AudioMath.ToLinear(Track.volume + decibels) * _mixer.Gain(Track.bus);

        private void Finish()
        {
            foreach (var deck in _decks)
                deck.Halt();
            foreach (var stinger in _stingers)
                stinger.Halt();
            _pending = false;
            _stopping = false;
            _announced = null;
            ClearBridge();
            Track = null;
        }

        private void OnStateChanged(StateGroup group, string state)
        {
            if (Track == null || Track.stateGroup != group)
                return;
            var link = Track.LinkFor(state);
            var segment = Track.Find(link?.segment);
            if (segment != null)
                RequestThrough(Track.Find(link.bridge), segment, Track.transitionSync);
        }

        #endregion
    }
}

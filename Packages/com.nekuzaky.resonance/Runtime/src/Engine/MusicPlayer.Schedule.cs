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

            if (_pending && now >= _decks[1 - _current].Start)
            {
                _current = 1 - _current;
                _pending = false;
            }

            var deck = _decks[_current];
            if (!_pending && deck.Active)
            {
                var length = Track.LengthOf(deck.Segment);
                if (length > 0.0 && now >= deck.Start + length - ScheduleAhead)
                {
                    var next = Track.Find(deck.Segment.next) ?? deck.Segment;
                    Schedule(next, deck.Start + length, 0f, now);
                }
            }
            ApplyVolumes(now);
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

            if (!hasPrevious)
            {
                _current = index;
                _pending = false;
                return;
            }
            previous.FadeTowards(0f, time, crossfade, now);
            if (previous.Source.isPlaying)
                previous.Source.SetScheduledEndTime(time + crossfade);
            _pending = true;
        }

        private void ApplyVolumes(double now)
        {
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
                deck.Source.volume = UnityEngine.Mathf.Clamp01(Gain(deck.Segment.volume) * deck.FadeAt(now));
                if (Metering && deck.Source.isPlaying)
                    _mixer.CountVoice(Track.bus, ClipEnvelopes.At(deck.Source.clip, deck.Source.timeSamples) * deck.Source.volume);
            }
        }

        private float Gain(float decibels) => AudioMath.ToLinear(Track.volume + decibels) * _mixer.Gain(Track.bus);

        private void Finish()
        {
            foreach (var deck in _decks)
                deck.Halt();
            _pending = false;
            _stopping = false;
            Track = null;
        }

        private void OnStateChanged(StateGroup group, string state)
        {
            if (Track == null || Track.stateGroup != group)
                return;
            var segment = Track.ForState(state);
            if (segment != null)
                Request(segment, Track.transitionSync);
        }

        #endregion
    }
}

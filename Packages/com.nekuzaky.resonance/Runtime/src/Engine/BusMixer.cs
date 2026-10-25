using System.Collections.Generic;
using UnityEngine;

namespace Nekuzaky.Resonance
{
    internal sealed class BusState
    {
        public float Duck;
        public float DuckTarget;
        public float DuckAttack;
        public float DuckRelease;
        public float DuckSpan;
        public float UserVolume;
        public int ActiveVoices;
        public float Meter;
        public float Peak;
        public bool Muted;
        public bool Soloed;
        public float FrameGain;
        public int GainFrame = -1;
    }

    internal sealed class ActiveSnapshot
    {
        public MixSnapshot Snapshot;
        public float Weight;
        public bool Removing;
    }

    internal sealed class BusMixer
    {
        #region Public

        public IReadOnlyDictionary<AudioBus, BusState> Buses => _buses;

        public IReadOnlyList<ActiveSnapshot> Snapshots => _snapshots;

        public bool AnySolo { get; private set; }

        #endregion


        #region Private and Protected

        private readonly Dictionary<AudioBus, BusState> _buses = new();
        private readonly List<ActiveSnapshot> _snapshots = new();
        private readonly List<AudioBus> _sources = new();
        private int _frame;
        private bool _frameOpen;

        #endregion


        #region Main API

        public BusState State(AudioBus bus)
        {
            if (_buses.TryGetValue(bus, out var state))
                return state;
            state = new BusState();
            _buses[bus] = state;
            return state;
        }

        public void BeginFrame()
        {
            var solo = false;
            foreach (var state in _buses.Values)
            {
                state.ActiveVoices = 0;
                state.Meter = 0f;
                solo |= state.Soloed;
            }
            AnySolo = solo;
            _frame++;
            _frameOpen = true;
        }

        public void CountVoice(AudioBus bus, float level)
        {
            for (var depth = 0; depth < AudioBus.MaxDepth && bus != null; depth++)
            {
                var state = State(bus);
                state.ActiveVoices++;
                state.Meter += level * level;
                bus = bus.parent;
            }
        }

        public void Tick(float deltaTime)
        {
            _frameOpen = false;
            TickSnapshots(deltaTime);
            foreach (var state in _buses.Values)
            {
                state.DuckTarget = 0f;
                state.Meter = Mathf.Sqrt(state.Meter);
                state.Peak = Mathf.Max(state.Meter, state.Peak - deltaTime * 0.8f);
            }
            CollectDucking();
            foreach (var state in _buses.Values)
            {
                var falling = state.DuckTarget < state.Duck;
                var time = falling ? state.DuckAttack : state.DuckRelease;
                state.Duck = AudioMath.Approach(state.Duck, state.DuckTarget, time, state.DuckSpan, deltaTime);
            }
        }

        public float Gain(AudioBus bus)
        {
            if (bus == null)
                return AnySolo ? 0f : 1f;
            if (!_frameOpen || !_buses.TryGetValue(bus, out var cached))
                return ComputeGain(bus);
            if (cached.GainFrame != _frame)
            {
                cached.FrameGain = ComputeGain(bus);
                cached.GainFrame = _frame;
            }
            return cached.FrameGain;
        }

        private float ComputeGain(AudioBus bus)
        {
            var decibels = 0f;
            var soloed = false;
            for (var depth = 0; depth < AudioBus.MaxDepth && bus != null; depth++)
            {
                if (bus.mute)
                    return 0f;
                var state = _buses.TryGetValue(bus, out var found) ? found : null;
                if (state != null && state.Muted)
                    return 0f;
                soloed |= state != null && state.Soloed;
                decibels += bus.volume + SnapshotOffset(bus) + (state == null ? 0f : state.Duck + state.UserVolume);
                bus = bus.parent;
            }
            return AnySolo && !soloed ? 0f : AudioMath.ToLinear(decibels);
        }

        public float SnapshotOffset(AudioBus bus)
        {
            var offset = 0f;
            foreach (var active in _snapshots)
            {
                foreach (var entry in active.Snapshot.offsets)
                {
                    if (entry != null && entry.bus == bus)
                        offset += entry.volume * active.Weight;
                }
            }
            return offset;
        }

        public void Apply(MixSnapshot snapshot)
        {
            if (snapshot == null)
                return;
            foreach (var active in _snapshots)
            {
                if (active.Snapshot == snapshot)
                {
                    active.Removing = false;
                    return;
                }
            }
            _snapshots.Add(new ActiveSnapshot { Snapshot = snapshot, Weight = snapshot.fadeIn <= 0f ? 1f : 0f });
        }

        public void Remove(MixSnapshot snapshot)
        {
            foreach (var active in _snapshots)
            {
                if (active.Snapshot == snapshot)
                    active.Removing = true;
            }
        }

        public bool IsActive(MixSnapshot snapshot)
        {
            foreach (var active in _snapshots)
            {
                if (active.Snapshot == snapshot && !active.Removing)
                    return true;
            }
            return false;
        }

        public void Clear()
        {
            _frameOpen = false;
            _buses.Clear();
            _snapshots.Clear();
            AnySolo = false;
        }

        #endregion


        #region Private and Protected

        private void TickSnapshots(float deltaTime)
        {
            for (var i = _snapshots.Count - 1; i >= 0; i--)
            {
                var active = _snapshots[i];
                if (active.Snapshot == null)
                {
                    _snapshots.RemoveAt(i);
                    continue;
                }
                var target = active.Removing ? 0f : 1f;
                var time = active.Removing ? active.Snapshot.fadeOut : active.Snapshot.fadeIn;
                active.Weight = AudioMath.Approach(active.Weight, target, time, 1f, deltaTime);
                if (active.Removing && active.Weight <= 0f)
                    _snapshots.RemoveAt(i);
            }
        }

        private void CollectDucking()
        {
            _sources.Clear();
            foreach (var pair in _buses)
            {
                if (pair.Value.ActiveVoices > 0 && pair.Key != null && pair.Key.ducking.Count > 0)
                    _sources.Add(pair.Key);
            }
            foreach (var source in _sources)
            {
                foreach (var rule in source.ducking)
                {
                    if (rule == null || rule.target == null)
                        continue;
                    var target = State(rule.target);
                    if (rule.amount < target.DuckTarget)
                    {
                        target.DuckTarget = rule.amount;
                        target.DuckAttack = rule.attack;
                        target.DuckRelease = rule.release;
                        target.DuckSpan = Mathf.Abs(rule.amount);
                    }
                }
            }
        }

        #endregion
    }
}

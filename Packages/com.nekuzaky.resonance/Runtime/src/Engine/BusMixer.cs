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
        public readonly List<float[]> Values = new();
        public readonly List<float> Reductions = new();
        public readonly EffectChain Chain = new();
        public int EvaluatedFrame = -1;
        public bool HasCompressor;
        public float Detector;
        public float Compression;
        public float GainReduction;
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

        public SyncStore Syncs { get; set; }

        #endregion


        #region Private and Protected

        private readonly Dictionary<AudioBus, BusState> _buses = new();
        private readonly List<ActiveSnapshot> _snapshots = new();
        private readonly List<AudioBus> _sources = new();
        private int _frame;

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
            _frame++;
            foreach (var state in _buses.Values)
            {
                state.ActiveVoices = 0;
                state.Meter = 0f;
                state.Detector = 0f;
                solo |= state.Soloed;
            }
            AnySolo = solo;
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
            foreach (var pair in _buses)
                TickCompressors(pair.Key, pair.Value, deltaTime);
        }

        public float Gain(AudioBus bus)
        {
            if (bus == null)
                return AnySolo ? 0f : 1f;

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
                decibels += OwnDecibels(bus, state);
                bus = bus.parent;
            }
            return AnySolo && !soloed ? 0f : AudioMath.ToLinear(decibels);
        }

        public EffectChain Chain(AudioBus bus)
        {
            if (bus == null)
                return EffectChain.None;
            var chain = State(bus).Chain;
            if (chain.Frame == _frame)
                return chain;
            chain.Frame = _frame;
            chain.Clear();
            var remaining = PathDecibels(bus);
            var current = bus;
            for (var depth = 0; depth < AudioBus.MaxDepth && current != null; depth++)
            {
                var state = Evaluate(current);
                chain.Level = Mathf.Max(0.0001f, AudioMath.ToLinear(remaining));
                remaining -= OwnDecibels(current, state);
                var count = Mathf.Min(current.effects.Count, BusEffects.MaxEffects);
                for (var i = 0; i < count; i++)
                {
                    var effect = current.effects[i];
                    if (effect != null && !effect.bypass)
                        chain.Add(effect, state.Values[i]);
                }
                current = current.parent;
            }
            chain.Finish();
            return chain;
        }

        public float[] EffectValues(AudioBus bus, int index)
        {
            if (bus == null || index < 0 || index >= Mathf.Min(bus.effects.Count, BusEffects.MaxEffects))
                return null;
            return Evaluate(bus).Values[index];
        }

        public float GainReduction(AudioBus bus) => bus != null && _buses.TryGetValue(bus, out var state) ? state.GainReduction : 0f;

        public void Detect(AudioBus bus, float level)
        {
            for (var depth = 0; depth < AudioBus.MaxDepth && bus != null && level > 0f; depth++)
            {
                var state = bus.effects.Count > 0 ? Evaluate(bus) : _buses.TryGetValue(bus, out var found) ? found : null;
                if (state != null && state.HasCompressor)
                    state.Detector += level * level;
                if (bus.mute || (state != null && state.Muted))
                    return;
                level *= AudioMath.ToLinear(OwnDecibels(bus, state));
                bus = bus.parent;
            }
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

        private float PathDecibels(AudioBus bus)
        {
            var decibels = 0f;
            for (var depth = 0; depth < AudioBus.MaxDepth && bus != null; depth++)
            {
                decibels += OwnDecibels(bus, _buses.TryGetValue(bus, out var state) ? state : null);
                bus = bus.parent;
            }
            return decibels;
        }

        private float OwnDecibels(AudioBus bus, BusState state) =>
            bus.volume + SnapshotOffset(bus) + (state == null ? 0f : state.Duck + state.UserVolume + state.Compression);

        private BusState Evaluate(AudioBus bus)
        {
            var state = State(bus);
            if (state.EvaluatedFrame == _frame)
                return state;
            state.EvaluatedFrame = _frame;
            state.HasCompressor = false;
            var count = Mathf.Min(bus.effects.Count, BusEffects.MaxEffects);
            for (var guard = 0; guard < BusEffects.MaxEffects && state.Values.Count < count; guard++)
            {
                state.Values.Add(new float[BusEffect.MaxParameters]);
                state.Reductions.Add(0f);
            }
            for (var i = 0; i < count; i++)
            {
                var effect = bus.effects[i];
                if (effect == null)
                    continue;
                EvaluateEffect(effect, state.Values[i]);
                state.HasCompressor |= !effect.bypass && effect.type == BusEffectType.Compressor;
            }
            ApplySnapshotEffects(bus, state, count);
            return state;
        }

        private void EvaluateEffect(BusEffect effect, float[] values)
        {
            var parameters = BusEffects.Parameters(effect.type);
            for (var p = 0; p < parameters.Count; p++)
                values[p] = effect.Value(p);
            if (effect.modulations == null)
                return;
            foreach (var modulation in effect.modulations)
            {
                if (modulation?.parameter == null || modulation.curve == null || modulation.target < 0 || modulation.target >= parameters.Count)
                    continue;
                var parameter = modulation.parameter;
                var raw = Syncs != null ? Syncs.GetParameter(parameter, null) : parameter.Clamp(parameter.defaultValue);
                values[modulation.target] = parameters[modulation.target].Clamp(modulation.curve.Evaluate(parameter.Normalize(raw)));
            }
        }

        private void ApplySnapshotEffects(AudioBus bus, BusState state, int count)
        {
            foreach (var active in _snapshots)
            {
                var entries = active.Snapshot != null ? active.Snapshot.effects : null;
                if (entries == null || active.Weight <= 0f)
                    continue;
                foreach (var entry in entries)
                {
                    if (entry == null || entry.bus != bus)
                        continue;
                    var index = bus.IndexOfEffect(entry.effect);
                    if (index < 0 || index >= count)
                        continue;
                    var parameters = BusEffects.Parameters(bus.effects[index].type);
                    if (entry.parameter < 0 || entry.parameter >= parameters.Count)
                        continue;
                    var info = parameters[entry.parameter];
                    var values = state.Values[index];
                    var target = info.Clamp(entry.value);
                    values[entry.parameter] = info.Logarithmic
                        ? AudioMath.LogLerp(values[entry.parameter], target, active.Weight)
                        : Mathf.Lerp(values[entry.parameter], target, active.Weight);
                }
            }
        }

        private void TickCompressors(AudioBus bus, BusState state, float deltaTime)
        {
            if (bus == null || (bus.effects.Count == 0 && state.Compression == 0f && state.GainReduction == 0f))
                return;
            var count = Mathf.Min(bus.effects.Count, BusEffects.MaxEffects);
            if (count > 0)
                Evaluate(bus);
            var input = AudioMath.ToDecibels(Mathf.Sqrt(state.Detector));
            var gain = 0f;
            var reduction = 0f;
            for (var i = 0; i < count; i++)
            {
                var effect = bus.effects[i];
                if (effect == null || effect.bypass || effect.type != BusEffectType.Compressor)
                {
                    state.Reductions[i] = 0f;
                    continue;
                }
                var values = state.Values[i];
                var amount = Compress(values, input + gain, state.Reductions[i], deltaTime);
                state.Reductions[i] = amount;
                gain += values[BusEffects.CompressorMakeup] - amount;
                reduction += amount;
            }
            state.Compression = gain;
            state.GainReduction = reduction;
        }

        internal static float Compress(float[] values, float input, float current, float deltaTime)
        {
            var threshold = values[BusEffects.CompressorThreshold];
            var ratio = Mathf.Max(1f, values[BusEffects.CompressorRatio]);
            var target = input > threshold ? (input - threshold) * (1f - 1f / ratio) : 0f;
            var time = target > current ? values[BusEffects.CompressorAttack] : values[BusEffects.CompressorRelease];
            if (time <= 0f || deltaTime <= 0f)
                return target;
            return current + (target - current) * (1f - Mathf.Exp(-deltaTime / time));
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

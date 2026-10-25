using System.Threading;
using UnityEngine;

namespace Nekuzaky.Resonance
{
    [AddComponentMenu("")]
    [ExecuteAlways]
    internal sealed class EffectDsp : MonoBehaviour
    {
        #region Public

        public const int MaxChannels = 8;
        public const float MaxDriveDb = 36f;

        public int Stages => _count;

        public StageKind Kind(int stage) => _kinds[stage];

        public float Value(int stage, int slot) => slot == 0 ? _a[stage] : slot == 1 ? _b[stage] : slot == 2 ? _c[stage] : _d[stage];

        public int SampleRate => _sampleRate;

        internal bool HasState
        {
            get
            {
                foreach (var value in _state)
                {
                    if (value != 0f)
                        return true;
                }
                return false;
            }
        }

        public void Configure(EffectChain chain, float applied = 1f)
        {
            var count = Mathf.Min(chain.Stages, EffectChain.MaxStages);
            var scale = applied > 0f && applied < 1f ? applied : 1f;
            if (Differs(chain, count, scale))
                Publish(chain, count, scale);
            var on = count > 0;
            if (enabled != on)
                enabled = on;
        }

        public void ResetState() => Interlocked.Exchange(ref _resetRequested, 1);

        public void Process(float[] data, int channels)
        {
            if (data == null || channels <= 0)
                return;
            Acquire();
            if (Interlocked.Exchange(ref _resetRequested, 0) != 0)
                System.Array.Clear(_state, 0, _state.Length);
            var set = _live * EffectChain.MaxStages;
            var count = _liveCount[_live];
            var rate = _sampleRate;
            for (var stage = 0; stage < count && stage < EffectChain.MaxStages; stage++)
            {
                var kind = _liveKinds[set + stage];
                Prepare(stage, set + stage, rate);
                if (kind == StageKind.Distortion)
                    Distort(data, stage);
                else if (kind != StageKind.None)
                    Filter(data, Mathf.Min(channels, MaxChannels), channels, stage);
            }
        }

        public static float SoftClip(float x)
        {
            x = Mathf.Clamp(x, -3f, 3f);
            var square = x * x;
            return x * (27f + square) / (27f + 9f * square);
        }

        #endregion


        #region Private and Protected

        private const int Sets = 2;

        private readonly StageKind[] _kinds = new StageKind[EffectChain.MaxStages];
        private readonly float[] _a = new float[EffectChain.MaxStages];
        private readonly float[] _b = new float[EffectChain.MaxStages];
        private readonly float[] _c = new float[EffectChain.MaxStages];
        private readonly float[] _d = new float[EffectChain.MaxStages];
        private readonly StageKind[] _liveKinds = new StageKind[EffectChain.MaxStages * Sets];
        private readonly float[] _liveA = new float[EffectChain.MaxStages * Sets];
        private readonly float[] _liveB = new float[EffectChain.MaxStages * Sets];
        private readonly float[] _liveC = new float[EffectChain.MaxStages * Sets];
        private readonly float[] _liveD = new float[EffectChain.MaxStages * Sets];
        private readonly int[] _liveCount = new int[Sets];
        private readonly StageKind[] _preparedKinds = new StageKind[EffectChain.MaxStages];
        private readonly float[] _preparedA = new float[EffectChain.MaxStages];
        private readonly float[] _preparedB = new float[EffectChain.MaxStages];
        private readonly float[] _preparedC = new float[EffectChain.MaxStages];
        private readonly float[] _preparedD = new float[EffectChain.MaxStages];
        private readonly int[] _preparedRates = new int[EffectChain.MaxStages];
        private readonly float[] _coefficients = new float[EffectChain.MaxStages * 5];
        private readonly float[] _state = new float[EffectChain.MaxStages * MaxChannels * 2];
        private int _count;
        private int _sequence;
        private int _acquired;
        private int _live;
        private int _resetRequested;
        private int _sampleRate = 48000;

        internal void BeginPublish() => Interlocked.Increment(ref _sequence);

        internal void EndPublish() => Interlocked.Increment(ref _sequence);

        internal void WriteStage(int stage, StageKind kind, float a)
        {
            _kinds[stage] = kind;
            _a[stage] = a;
        }

        internal StageKind PreparedKind(int stage) => _preparedKinds[stage];

        internal float PreparedValue(int stage) => _preparedA[stage];

        private bool Differs(EffectChain chain, int count, float scale)
        {
            if (count != _count)
                return true;
            for (var i = 0; i < count; i++)
            {
                if (_kinds[i] != chain.Kinds[i] || _a[i] != chain.A[i] || _b[i] != chain.B[i] || _c[i] != chain.C[i] ||
                    _d[i] != chain.D[i] * scale)
                    return true;
            }
            return false;
        }

        private void Publish(EffectChain chain, int count, float scale)
        {
            BeginPublish();
            for (var i = 0; i < count; i++)
            {
                _kinds[i] = chain.Kinds[i];
                _a[i] = chain.A[i];
                _b[i] = chain.B[i];
                _c[i] = chain.C[i];
                _d[i] = chain.D[i] * scale;
            }
            _count = count;
            _sampleRate = Mathf.Max(8000, AudioSettings.outputSampleRate);
            EndPublish();
        }

        private void Acquire()
        {
            var sequence = Volatile.Read(ref _sequence);
            if ((sequence & 1) != 0 || sequence == _acquired)
                return;
            var next = 1 - _live;
            var set = next * EffectChain.MaxStages;
            var count = Mathf.Clamp(_count, 0, EffectChain.MaxStages);
            for (var i = 0; i < count; i++)
            {
                _liveKinds[set + i] = _kinds[i];
                _liveA[set + i] = _a[i];
                _liveB[set + i] = _b[i];
                _liveC[set + i] = _c[i];
                _liveD[set + i] = _d[i];
            }
            Interlocked.MemoryBarrier();
            if (Volatile.Read(ref _sequence) != sequence)
                return;
            _liveCount[next] = count;
            _live = next;
            _acquired = sequence;
        }

        private void Prepare(int stage, int live, int rate)
        {
            var kind = _liveKinds[live];
            var a = _liveA[live];
            var b = _liveB[live];
            var c = _liveC[live];
            var d = _liveD[live];
            if (_preparedKinds[stage] == kind && _preparedA[stage] == a && _preparedB[stage] == b && _preparedC[stage] == c &&
                _preparedD[stage] == d && _preparedRates[stage] == rate)
                return;
            if (_preparedKinds[stage] != kind)
                System.Array.Clear(_state, stage * MaxChannels * 2, MaxChannels * 2);
            _preparedKinds[stage] = kind;
            _preparedA[stage] = a;
            _preparedB[stage] = b;
            _preparedC[stage] = c;
            _preparedD[stage] = d;
            _preparedRates[stage] = rate;
            var offset = stage * 5;
            if (kind == StageKind.Distortion)
            {
                _coefficients[offset] = AudioMath.ToLinear(Mathf.Clamp01(a) * MaxDriveDb);
                _coefficients[offset + 1] = AudioMath.ToLinear(b);
                _coefficients[offset + 2] = Mathf.Clamp01(c);
                _coefficients[offset + 3] = d > 0f ? d : 1f;
                return;
            }
            Biquad(kind == StageKind.HighPass, a, b, rate, _coefficients, offset);
        }

        internal static void Biquad(bool highPass, float cutoff, float resonance, int rate, float[] coefficients, int offset)
        {
            var frequency = Mathf.Clamp(cutoff, 10f, rate * 0.45f);
            var q = 0.7071f * Mathf.Clamp(resonance, 1f, 10f);
            var w0 = 2.0 * System.Math.PI * frequency / rate;
            var cos = System.Math.Cos(w0);
            var alpha = System.Math.Sin(w0) / (2.0 * q);
            var a0 = 1.0 + alpha;
            var side = highPass ? (1.0 + cos) / 2.0 : (1.0 - cos) / 2.0;
            coefficients[offset] = (float)(side / a0);
            coefficients[offset + 1] = (float)((highPass ? -(1.0 + cos) : 1.0 - cos) / a0);
            coefficients[offset + 2] = (float)(side / a0);
            coefficients[offset + 3] = (float)(-2.0 * cos / a0);
            coefficients[offset + 4] = (float)((1.0 - alpha) / a0);
        }

        private void Filter(float[] data, int processed, int channels, int stage)
        {
            var offset = stage * 5;
            var b0 = _coefficients[offset];
            var b1 = _coefficients[offset + 1];
            var b2 = _coefficients[offset + 2];
            var a1 = _coefficients[offset + 3];
            var a2 = _coefficients[offset + 4];
            for (var channel = 0; channel < processed; channel++)
            {
                var index = (stage * MaxChannels + channel) * 2;
                var z1 = _state[index];
                var z2 = _state[index + 1];
                for (var i = channel; i < data.Length; i += channels)
                {
                    var x = data[i];
                    var y = b0 * x + z1;
                    z1 = b1 * x - a1 * y + z2;
                    z2 = b2 * x - a2 * y;
                    data[i] = y;
                }
                if (!float.IsFinite(z1) || !float.IsFinite(z2))
                {
                    z1 = 0f;
                    z2 = 0f;
                }
                _state[index] = Mathf.Abs(z1) < 1e-20f ? 0f : z1;
                _state[index + 1] = Mathf.Abs(z2) < 1e-20f ? 0f : z2;
            }
        }

        private void Distort(float[] data, int stage)
        {
            var offset = stage * 5;
            var drive = _coefficients[offset];
            var output = _coefficients[offset + 1];
            var mix = _coefficients[offset + 2];
            var level = _coefficients[offset + 3];
            var input = drive / level;
            output *= level;
            for (var i = 0; i < data.Length; i++)
            {
                var x = data[i];
                data[i] = x + (SoftClip(x * input) * output - x) * mix;
            }
        }

        #endregion


        #region Unity Callbacks

        private void OnAudioFilterRead(float[] data, int channels) => Process(data, channels);

        #endregion
    }
}

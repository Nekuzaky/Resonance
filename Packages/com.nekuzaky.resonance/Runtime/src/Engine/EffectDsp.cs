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

        public float Value(int stage, int slot) => slot == 0 ? _a[stage] : slot == 1 ? _b[stage] : _c[stage];

        public int SampleRate => _sampleRate;

        public void Configure(EffectChain chain)
        {
            var count = Mathf.Min(chain.Stages, EffectChain.MaxStages);
            var changed = count != _count;
            for (var i = 0; i < count; i++)
            {
                changed |= _kinds[i] != chain.Kinds[i] || _a[i] != chain.A[i] || _b[i] != chain.B[i] || _c[i] != chain.C[i] || _d[i] != chain.D[i];
                _kinds[i] = chain.Kinds[i];
                _a[i] = chain.A[i];
                _b[i] = chain.B[i];
                _c[i] = chain.C[i];
                _d[i] = chain.D[i];
            }
            if (changed)
                _sampleRate = Mathf.Max(8000, AudioSettings.outputSampleRate);
            _count = count;
            var on = count > 0;
            if (enabled != on)
                enabled = on;
        }

        public void Process(float[] data, int channels)
        {
            var count = _count;
            if (data == null || channels <= 0)
                return;
            var rate = _sampleRate;
            for (var stage = 0; stage < count && stage < EffectChain.MaxStages; stage++)
            {
                var kind = _kinds[stage];
                Prepare(stage, kind, rate);
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

        private readonly StageKind[] _kinds = new StageKind[EffectChain.MaxStages];
        private readonly float[] _a = new float[EffectChain.MaxStages];
        private readonly float[] _b = new float[EffectChain.MaxStages];
        private readonly float[] _c = new float[EffectChain.MaxStages];
        private readonly float[] _d = new float[EffectChain.MaxStages];
        private readonly float[] _preparedD = new float[EffectChain.MaxStages];
        private readonly StageKind[] _preparedKinds = new StageKind[EffectChain.MaxStages];
        private readonly float[] _preparedA = new float[EffectChain.MaxStages];
        private readonly float[] _preparedB = new float[EffectChain.MaxStages];
        private readonly float[] _preparedC = new float[EffectChain.MaxStages];
        private readonly int[] _preparedRates = new int[EffectChain.MaxStages];
        private readonly float[] _coefficients = new float[EffectChain.MaxStages * 5];
        private readonly float[] _state = new float[EffectChain.MaxStages * MaxChannels * 2];
        private volatile int _count;
        private int _sampleRate = 48000;

        private void Prepare(int stage, StageKind kind, int rate)
        {
            var a = _a[stage];
            var b = _b[stage];
            var c = _c[stage];
            var d = _d[stage];
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

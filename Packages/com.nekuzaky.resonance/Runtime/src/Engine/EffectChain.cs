using UnityEngine;

namespace Nekuzaky.Resonance
{
    internal enum StageKind
    {
        None,
        LowPass,
        HighPass,
        Distortion
    }

    internal sealed class EffectChain
    {
        #region Public

        public const int MaxStages = 8;

        public static readonly EffectChain None = new();

        public readonly StageKind[] Kinds = new StageKind[MaxStages];
        public readonly float[] A = new float[MaxStages];
        public readonly float[] B = new float[MaxStages];
        public readonly float[] C = new float[MaxStages];
        public readonly float[] D = new float[MaxStages];
        public float Level = 1f;
        public int Stages;
        public bool Any;
        public bool Compressed;
        public bool Delay;
        public float DelayTime;
        public float Feedback;
        public float Wet;
        public float Dry;
        public bool Reverb;
        public bool ReverbInsert;
        public float ReverbMix;
        public float Decay;
        public float Damping;
        public float PreDelay;
        public float Tail;
        public int Frame = -1;

        public void Clear()
        {
            Stages = 0;
            Level = 1f;
            Any = false;
            Compressed = false;
            Delay = false;
            Reverb = false;
            ReverbInsert = false;
            Tail = 0f;
        }

        public void Add(BusEffect effect, float[] values)
        {
            switch (effect.type)
            {
                case BusEffectType.LowPass:
                    if (values[BusEffects.FilterCutoff] < AudioMath.OpenLowPass - 1f)
                        AddStage(StageKind.LowPass, values[BusEffects.FilterCutoff], values[BusEffects.FilterResonance], 1f);
                    break;
                case BusEffectType.HighPass:
                    if (values[BusEffects.FilterCutoff] > AudioMath.OpenHighPass + 1f)
                        AddStage(StageKind.HighPass, values[BusEffects.FilterCutoff], values[BusEffects.FilterResonance], 1f);
                    break;
                case BusEffectType.Distortion:
                    if (values[BusEffects.DistortionDrive] > 0f && values[BusEffects.DistortionMix] > 0f)
                        AddStage(StageKind.Distortion, values[BusEffects.DistortionDrive], values[BusEffects.DistortionOutput], values[BusEffects.DistortionMix]);
                    break;
                case BusEffectType.Delay:
                    AddDelay(values);
                    break;
                case BusEffectType.Reverb:
                    AddReverb(effect.routing, values);
                    break;
                case BusEffectType.Compressor:
                    Compressed = true;
                    break;
            }
        }

        public void Finish()
        {
            Any = Stages > 0 || Delay || Reverb;
            var tail = 0f;
            if (Delay && Wet > 0f)
                tail = DelayTail(DelayTime * 0.001f, Feedback);
            if (Reverb && ReverbMix > 0f)
                tail = Mathf.Max(tail, Decay + PreDelay + 0.1f);
            Tail = Mathf.Min(tail, BusEffects.MaxTail);
        }

        public static float DelayTail(float seconds, float feedback)
        {
            if (feedback <= 0.001f)
                return seconds + 0.05f;
            var repeats = Mathf.Log(0.001f) / Mathf.Log(Mathf.Min(feedback, 0.999f));
            return seconds * (Mathf.Min(repeats, 1000f) + 1f) + 0.05f;
        }

        #endregion


        #region Private and Protected

        private void AddStage(StageKind kind, float a, float b, float c)
        {
            if (Stages >= MaxStages)
                return;
            Kinds[Stages] = kind;
            A[Stages] = a;
            B[Stages] = b;
            C[Stages] = c;
            D[Stages] = Level;
            Stages++;
        }

        private void AddDelay(float[] values)
        {
            if (Delay)
                return;
            Delay = true;
            DelayTime = values[BusEffects.DelayTime];
            Feedback = values[BusEffects.DelayFeedback];
            Wet = values[BusEffects.DelayWet];
            Dry = values[BusEffects.DelayDry];
        }

        private void AddReverb(ReverbRouting routing, float[] values)
        {
            if (Reverb)
                return;
            Reverb = true;
            ReverbInsert = routing == ReverbRouting.Insert;
            ReverbMix = values[BusEffects.ReverbMix];
            Decay = values[BusEffects.ReverbDecay];
            Damping = values[BusEffects.ReverbDamping];
            PreDelay = values[BusEffects.ReverbPreDelay];
        }

        #endregion
    }
}

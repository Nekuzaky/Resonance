using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekuzaky.Resonance
{
    public enum BusEffectType
    {
        LowPass,
        HighPass,
        Reverb,
        Delay,
        Distortion,
        Compressor
    }

    public enum ReverbRouting
    {
        Send,
        Insert
    }

    public readonly struct EffectParameterInfo
    {
        public readonly string Name;
        public readonly string Unit;
        public readonly float Min;
        public readonly float Max;
        public readonly float Default;
        public readonly bool Logarithmic;

        public EffectParameterInfo(string name, string unit, float min, float max, float defaultValue, bool logarithmic = false)
        {
            Name = name;
            Unit = unit;
            Min = min;
            Max = max;
            Default = defaultValue;
            Logarithmic = logarithmic;
        }

        public float Clamp(float value) => float.IsNaN(value) ? Default : Mathf.Clamp(value, Min, Max);
    }

    [Serializable]
    public sealed class EffectModulation
    {
        public SoundParameter parameter;
        public int target;
        public AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 0f);
    }

    [Serializable]
    public sealed class EffectOffset
    {
        public AudioBus bus;
        public int effect;
        public int parameter;
        public float value;
    }

    [Serializable]
    public sealed class BusEffect
    {
        #region Public

        public const int MaxParameters = 5;

        public int id;
        public BusEffectType type;
        public bool bypass;
        public ReverbRouting routing = ReverbRouting.Send;
        public float[] values = Defaults(BusEffectType.LowPass);
        public List<EffectModulation> modulations = new();

        public int ParameterCount => BusEffects.Parameters(type).Count;

        public BusEffect()
        {
        }

        public BusEffect(BusEffectType effectType)
        {
            type = effectType;
            values = Defaults(effectType);
        }

        public float Value(int index)
        {
            var parameters = BusEffects.Parameters(type);
            if (index < 0 || index >= parameters.Count)
                return 0f;
            return values != null && index < values.Length ? parameters[index].Clamp(values[index]) : parameters[index].Default;
        }

        public void SetValue(int index, float value)
        {
            var parameters = BusEffects.Parameters(type);
            if (index < 0 || index >= parameters.Count)
                return;
            if (values == null || values.Length < parameters.Count)
            {
                var grown = Defaults(type);
                for (var i = 0; values != null && i < values.Length && i < grown.Length; i++)
                    grown[i] = values[i];
                values = grown;
            }
            values[index] = parameters[index].Clamp(value);
        }

        public static float[] Defaults(BusEffectType effectType)
        {
            var parameters = BusEffects.Parameters(effectType);
            var defaults = new float[parameters.Count];
            for (var i = 0; i < parameters.Count; i++)
                defaults[i] = parameters[i].Default;
            return defaults;
        }

        #endregion
    }

    public static class BusEffects
    {
        #region Public

        public const int MaxEffects = 16;
        public const float MaxTail = 10f;

        public const int FilterCutoff = 0;
        public const int FilterResonance = 1;
        public const int ReverbMix = 0;
        public const int ReverbDecay = 1;
        public const int ReverbDamping = 2;
        public const int ReverbPreDelay = 3;
        public const int DelayTime = 0;
        public const int DelayFeedback = 1;
        public const int DelayWet = 2;
        public const int DelayDry = 3;
        public const int DistortionDrive = 0;
        public const int DistortionOutput = 1;
        public const int DistortionMix = 2;
        public const int CompressorThreshold = 0;
        public const int CompressorRatio = 1;
        public const int CompressorAttack = 2;
        public const int CompressorRelease = 3;
        public const int CompressorMakeup = 4;

        public static IReadOnlyList<EffectParameterInfo> Parameters(BusEffectType type)
        {
            switch (type)
            {
                case BusEffectType.LowPass: return LowPass;
                case BusEffectType.HighPass: return HighPass;
                case BusEffectType.Reverb: return Reverb;
                case BusEffectType.Delay: return Delay;
                case BusEffectType.Distortion: return Distortion;
                case BusEffectType.Compressor: return Compressor;
                default: return LowPass;
            }
        }

        public static string Label(BusEffectType type)
        {
            switch (type)
            {
                case BusEffectType.LowPass: return "Low Pass";
                case BusEffectType.HighPass: return "High Pass";
                case BusEffectType.Delay: return "Delay";
                default: return type.ToString();
            }
        }

        #endregion


        #region Private and Protected

        private static readonly EffectParameterInfo[] LowPass =
        {
            new("Cutoff", "Hz", 10f, 22000f, 5000f, true),
            new("Resonance", "Q", 1f, 10f, 1f)
        };

        private static readonly EffectParameterInfo[] HighPass =
        {
            new("Cutoff", "Hz", 10f, 22000f, 200f, true),
            new("Resonance", "Q", 1f, 10f, 1f)
        };

        private static readonly EffectParameterInfo[] Reverb =
        {
            new("Mix", "", 0f, 1f, 0.3f),
            new("Decay", "s", 0.1f, 20f, 1.5f),
            new("HF Ratio", "", 0.1f, 2f, 0.83f),
            new("Pre-delay", "s", 0f, 0.1f, 0.011f)
        };

        private static readonly EffectParameterInfo[] Delay =
        {
            new("Time", "ms", 10f, 5000f, 350f),
            new("Feedback", "", 0f, 0.95f, 0.4f),
            new("Wet", "", 0f, 1f, 0.5f),
            new("Dry", "", 0f, 1f, 1f)
        };

        private static readonly EffectParameterInfo[] Distortion =
        {
            new("Drive", "", 0f, 1f, 0.5f),
            new("Output", "dB", -24f, 6f, -6f),
            new("Mix", "", 0f, 1f, 1f)
        };

        private static readonly EffectParameterInfo[] Compressor =
        {
            new("Threshold", "dB", -60f, 0f, -18f),
            new("Ratio", ":1", 1f, 20f, 4f),
            new("Attack", "s", 0f, 1f, 0.02f),
            new("Release", "s", 0.01f, 3f, 0.3f),
            new("Makeup", "dB", 0f, 24f, 0f)
        };

        #endregion
    }
}

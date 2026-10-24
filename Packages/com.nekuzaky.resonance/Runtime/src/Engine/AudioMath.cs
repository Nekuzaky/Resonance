using UnityEngine;

namespace Nekuzaky.Resonance
{
    public static class AudioMath
    {
        public const float SilenceDb = -80f;
        public const float OpenLowPass = 22000f;
        public const float OpenHighPass = 10f;

        public static float ToLinear(float decibels) => decibels <= SilenceDb ? 0f : Mathf.Pow(10f, decibels / 20f);

        public static float ToDecibels(float linear) => linear <= 0.0001f ? SilenceDb : 20f * Mathf.Log10(linear);

        public static float ToPitch(float semitones) => Mathf.Pow(2f, semitones / 12f);

        public static float Approach(float current, float target, float duration, float span, float deltaTime)
        {
            if (duration <= 0f || span <= 0f)
                return target;
            return Mathf.MoveTowards(current, target, span / duration * deltaTime);
        }

        public static float BlendWeight(float value, Vector2 range, float fade)
        {
            var low = Mathf.Min(range.x, range.y);
            var high = Mathf.Max(range.x, range.y);
            if (value >= low && value <= high)
                return 1f;
            if (fade <= 0f)
                return 0f;
            var distance = value < low ? low - value : value - high;
            return Mathf.Clamp01(1f - distance / fade);
        }

        public static double NextBoundary(double origin, double time, double unit)
        {
            if (unit <= 0.0 || time <= origin)
                return origin;
            var steps = System.Math.Ceiling((time - origin) / unit - 1e-9);
            return origin + steps * unit;
        }
    }
}

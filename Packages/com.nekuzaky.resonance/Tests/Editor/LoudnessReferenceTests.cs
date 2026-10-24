using NUnit.Framework;
using Nekuzaky.Resonance.Editor;
using UnityEngine;

namespace Nekuzaky.Resonance.Tests
{
    public sealed class LoudnessReferenceTests : Fixture
    {
        private const int Rate = 48000;

        [TestCase(new[] { -23f }, new[] { 20f }, -23f, TestName = "Ebu3341_Case1_SteadyMinus23")]
        [TestCase(new[] { -33f }, new[] { 20f }, -33f, TestName = "Ebu3341_Case2_SteadyMinus33")]
        [TestCase(new[] { -36f, -23f, -36f }, new[] { 10f, 60f, 10f }, -23f, TestName = "Ebu3341_Case3_RelativeGate")]
        [TestCase(new[] { -72f, -36f, -23f, -36f, -72f }, new[] { 10f, 10f, 60f, 10f, 10f }, -23f, TestName = "Ebu3341_Case4_AbsoluteGate")]
        [TestCase(new[] { -26f, -20f, -26f }, new[] { 20f, 20.1f, 20f }, -23f, TestName = "Ebu3341_Case5_LoudMiddle")]
        public void Measure_MatchesTheEbuReferenceSignals(float[] levels, float[] seconds, float expected)
        {
            var clip = Track(StereoSine(levels, seconds, 1000f));

            var result = Loudness.Measure(clip);

            Assert.IsTrue(result.Valid);
            Assert.AreEqual(expected, result.Loudness, 0.1f);
        }

        [TestCase(100f, -1.8f)]
        [TestCase(10000f, 3.4f)]
        public void Weighting_FollowsTheKCurve(float frequency, float relativeToOneKilohertz)
        {
            var reference = Loudness.Measure(Track(StereoSine(new[] { -20f }, new[] { 2f }, 1000f))).Loudness;
            var measured = Loudness.Measure(Track(StereoSine(new[] { -20f }, new[] { 2f }, frequency))).Loudness;

            Assert.AreEqual(relativeToOneKilohertz, measured - reference, 0.15f);
        }

        private static AudioClip StereoSine(float[] levels, float[] seconds, float frequency)
        {
            var frames = 0;
            foreach (var duration in seconds)
                frames += Mathf.RoundToInt(duration * Rate);
            var data = new float[frames * 2];
            var frame = 0;
            for (var part = 0; part < levels.Length; part++)
            {
                var amplitude = Mathf.Pow(10f, levels[part] / 20f);
                var end = frame + Mathf.RoundToInt(seconds[part] * Rate);
                for (; frame < end; frame++)
                {
                    var value = amplitude * (float)System.Math.Sin(2.0 * System.Math.PI * frequency * frame / Rate);
                    data[frame * 2] = value;
                    data[frame * 2 + 1] = value;
                }
            }
            var clip = AudioClip.Create("reference", frames, 2, Rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}

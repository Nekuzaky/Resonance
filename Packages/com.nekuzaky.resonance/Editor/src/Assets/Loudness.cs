using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Nekuzaky.Resonance.Editor
{
    internal readonly struct ClipLoudness
    {
        public readonly float Loudness;
        public readonly float Peak;
        public readonly bool Valid;

        public ClipLoudness(float loudness, float peak)
        {
            Loudness = loudness;
            Peak = peak;
            Valid = true;
        }
    }

    internal sealed class LoudnessImbalance
    {
        public SoundEvent Event;
        public float Spread;
        public readonly List<(ClipNode Node, float Loudness)> Clips = new();
    }

    internal static class Loudness
    {
        #region Public

        public const float AbsoluteGate = -70f;
        public const float RelativeGate = -10f;
        public const float AllowedSpread = 6f;
        public const float MaxBoost = 12f;

        public static ClipLoudness Measure(AudioClip clip)
        {
            if (clip == null)
                return default;
            if (Cache.TryGetValue(clip, out var cached))
                return cached;
            var blocks = Blocks(clip, out var peak);
            var result = blocks == null ? default : new ClipLoudness(Gated(blocks), AudioMath.ToDecibels(peak));
            Cache[clip] = result;
            return result;
        }

        public static float Gated(IReadOnlyList<float> blockMeanSquares)
        {
            var sum = 0.0;
            var count = 0;
            foreach (var block in blockMeanSquares)
            {
                if (ToDb(block) > AbsoluteGate)
                {
                    sum += block;
                    count++;
                }
            }
            if (count == 0)
                return AudioMath.SilenceDb;
            var threshold = ToDb((float)(sum / count)) + RelativeGate;
            sum = 0.0;
            count = 0;
            foreach (var block in blockMeanSquares)
            {
                if (ToDb(block) > threshold)
                {
                    sum += block;
                    count++;
                }
            }
            return count == 0 ? AudioMath.SilenceDb : ToDb((float)(sum / count));
        }

        public static List<LoudnessImbalance> FindImbalances()
        {
            var found = new List<LoudnessImbalance>();
            foreach (var sound in ResonanceAssets.FindAll<SoundEvent>())
            {
                var imbalance = Analyze(sound);
                if (imbalance != null)
                    found.Add(imbalance);
            }
            return found;
        }

        public static LoudnessImbalance Analyze(SoundEvent sound)
        {
            var imbalance = new LoudnessImbalance { Event = sound };
            var walker = new NodeWalker(sound.root);
            for (var guard = 0; guard < NodeWalker.MaxNodes && walker.Next(out var node); guard++)
            {
                if (node is not ClipNode { clip: not null } clip)
                    continue;
                var measure = Measure(clip.clip);
                if (measure.Valid && measure.Loudness > AudioMath.SilenceDb)
                    imbalance.Clips.Add((clip, measure.Loudness + clip.volume));
            }
            if (imbalance.Clips.Count < 2)
                return null;
            var low = float.MaxValue;
            var high = float.MinValue;
            foreach (var (_, loudness) in imbalance.Clips)
            {
                low = Mathf.Min(low, loudness);
                high = Mathf.Max(high, loudness);
            }
            imbalance.Spread = high - low;
            return imbalance.Spread > AllowedSpread ? imbalance : null;
        }

        public static void Balance(LoudnessImbalance imbalance)
        {
            var values = new List<float>();
            foreach (var (node, _) in imbalance.Clips)
                values.Add(Measure(node.clip).Loudness);
            values.Sort();
            var median = values[values.Count / 2];
            var highest = float.MinValue;
            foreach (var value in values)
                highest = Mathf.Max(highest, median - value);
            var shift = Mathf.Max(0f, highest - MaxBoost);
            Undo.RecordObject(imbalance.Event, "Balance loudness");
            foreach (var (node, _) in imbalance.Clips)
                node.volume = Mathf.Clamp(Mathf.Round((median - Measure(node.clip).Loudness - shift) * 2f) / 2f, -48f, MaxBoost);
            EditorUtility.SetDirty(imbalance.Event);
        }

        public static void Forget(AudioClip clip) => Cache.Remove(clip);

        #endregion


        #region Private and Protected

        private static readonly Dictionary<AudioClip, ClipLoudness> Cache = new();

        private const float Offset = -0.691f;
        private const float HopSeconds = 0.1f;
        private const int HopsPerBlock = 4;

        private static float ToDb(float meanSquare) => meanSquare <= 1e-10f ? AudioMath.SilenceDb : Offset + 10f * Mathf.Log10(meanSquare);

        private static float Weight(int channel, int channels) => channels != 6 ? 1f : channel == 3 ? 0f : channel >= 4 ? 1.41f : 1f;

        private static List<float> Blocks(AudioClip clip, out float peak)
        {
            peak = 0f;
            if (clip.loadType == AudioClipLoadType.Streaming || clip.samples <= 0)
                return null;
            if (clip.loadState != AudioDataLoadState.Loaded && !clip.LoadAudioData())
                return null;
            var channels = Mathf.Max(1, clip.channels);
            var hop = Mathf.Max(1, Mathf.RoundToInt(clip.frequency * HopSeconds));
            var filters = new KWeighting[channels];
            for (var c = 0; c < channels; c++)
                filters[c] = new KWeighting(clip.frequency);
            var buffer = new float[hop * channels];
            var hops = new List<float>();
            var total = 0.0;
            for (var offset = 0; offset < clip.samples; offset += hop)
            {
                var read = Mathf.Min(hop, clip.samples - offset);
                var data = read == hop ? buffer : new float[read * channels];
                if (!clip.GetData(data, offset))
                    return null;
                var sum = 0.0;
                for (var i = 0; i < data.Length; i++)
                {
                    var channel = i % channels;
                    var weighted = filters[channel].Process(data[i]);
                    sum += Weight(channel, channels) * weighted * weighted;
                    peak = Mathf.Max(peak, Mathf.Abs(data[i]));
                }
                total += sum;
                if (read == hop)
                    hops.Add((float)(sum / hop));
            }
            return Overlap(hops, (float)(total / clip.samples));
        }

        private static List<float> Overlap(List<float> hops, float whole)
        {
            var blocks = new List<float>();
            if (hops.Count < HopsPerBlock)
            {
                blocks.Add(whole);
                return blocks;
            }
            for (var i = HopsPerBlock - 1; i < hops.Count; i++)
                blocks.Add((hops[i] + hops[i - 1] + hops[i - 2] + hops[i - 3]) / HopsPerBlock);
            return blocks;
        }

        #endregion
    }

    internal sealed class KWeighting
    {
        #region Public

        public KWeighting(int sampleRate)
        {
            var k = System.Math.Tan(System.Math.PI * ShelfFrequency / sampleRate);
            var gain = System.Math.Pow(10.0, ShelfGain / 20.0);
            var boost = System.Math.Pow(gain, 0.4996667741545416);
            var norm = 1.0 + k / ShelfQ + k * k;
            _s0 = (gain + boost * k / ShelfQ + k * k) / norm;
            _s1 = 2.0 * (k * k - gain) / norm;
            _s2 = (gain - boost * k / ShelfQ + k * k) / norm;
            _sa1 = 2.0 * (k * k - 1.0) / norm;
            _sa2 = (1.0 - k / ShelfQ + k * k) / norm;
            k = System.Math.Tan(System.Math.PI * HighPassFrequency / sampleRate);
            norm = 1.0 + k / HighPassQ + k * k;
            _ha1 = 2.0 * (k * k - 1.0) / norm;
            _ha2 = (1.0 - k / HighPassQ + k * k) / norm;
        }

        public double Process(double input)
        {
            var shelved = _s0 * input + _sz1;
            _sz1 = _s1 * input - _sa1 * shelved + _sz2;
            _sz2 = _s2 * input - _sa2 * shelved;
            var output = shelved + _hz1;
            _hz1 = -2.0 * shelved - _ha1 * output + _hz2;
            _hz2 = shelved - _ha2 * output;
            return output;
        }

        #endregion


        #region Private and Protected

        private const double ShelfFrequency = 1681.974450955533;
        private const double ShelfGain = 3.999843853973347;
        private const double ShelfQ = 0.7071752369554196;
        private const double HighPassFrequency = 38.13547087602444;
        private const double HighPassQ = 0.5003270373238773;

        private readonly double _s0, _s1, _s2, _sa1, _sa2, _ha1, _ha2;
        private double _sz1, _sz2, _hz1, _hz2;

        #endregion
    }
}

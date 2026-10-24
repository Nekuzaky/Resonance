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

        public const float BlockSeconds = 0.4f;
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

        private static float ToDb(float meanSquare) => meanSquare <= 1e-10f ? AudioMath.SilenceDb : 10f * Mathf.Log10(meanSquare);

        private static List<float> Blocks(AudioClip clip, out float peak)
        {
            peak = 0f;
            if (clip.loadType == AudioClipLoadType.Streaming || clip.samples <= 0)
                return null;
            if (clip.loadState != AudioDataLoadState.Loaded && !clip.LoadAudioData())
                return null;
            var channels = Mathf.Max(1, clip.channels);
            var frames = Mathf.Max(1, Mathf.RoundToInt(clip.frequency * BlockSeconds));
            var buffer = new float[frames * channels];
            var blocks = new List<float>();
            for (var offset = 0; offset < clip.samples; offset += frames)
            {
                var read = Mathf.Min(frames, clip.samples - offset);
                var data = read == frames ? buffer : new float[read * channels];
                if (!clip.GetData(data, offset))
                    return null;
                var sum = 0.0;
                foreach (var sample in data)
                {
                    sum += sample * sample;
                    peak = Mathf.Max(peak, Mathf.Abs(sample));
                }
                blocks.Add((float)(sum / data.Length));
                if (read < frames)
                    break;
            }
            return blocks;
        }

        #endregion
    }
}

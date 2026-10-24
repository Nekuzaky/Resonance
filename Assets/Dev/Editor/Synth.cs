using System;
using System.IO;
using UnityEngine;

namespace Nekuzaky.Resonance.Dev
{
    internal static class Synth
    {
        #region Public

        public const int Rate = 44100;

        public static float[] Buffer(float seconds) => new float[Mathf.RoundToInt(seconds * Rate)];

        public static void Footstep(float[] data, int seed, float brightness, float thump)
        {
            var random = new System.Random(seed);
            var filtered = 0f;
            for (var i = 0; i < data.Length; i++)
            {
                var t = i / (float)Rate;
                var envelope = Mathf.Exp(-t * (18f + brightness * 20f));
                filtered += (Noise(random) - filtered) * brightness;
                var body = Mathf.Sin(2f * Mathf.PI * (70f + seed % 5 * 6f) * t) * Mathf.Exp(-t * 30f) * thump;
                data[i] = (filtered * 0.9f + body) * envelope;
            }
        }

        public static float[] Wind(float[] data, int seed)
        {
            var random = new System.Random(seed);
            var low = 0f;
            for (var i = 0; i < data.Length; i++)
            {
                var t = i / (float)Rate;
                low += (Noise(random) - low) * 0.02f;
                var gust = 0.55f + 0.45f * Mathf.Sin(2f * Mathf.PI * t / (data.Length / (float)Rate));
                data[i] = low * 2.2f * gust;
            }
            return LoopCrossfade(data, Rate / 2);
        }

        public static void Engine(float[] data, float frequency, float roughness, int seed)
        {
            var random = new System.Random(seed);
            for (var i = 0; i < data.Length; i++)
            {
                var t = i / (float)Rate;
                var phase = frequency * t % 1f;
                var saw = phase * 2f - 1f;
                var pulse = Mathf.Sin(2f * Mathf.PI * frequency * 0.5f * t);
                data[i] = (saw * 0.35f + pulse * 0.35f + Noise(random) * roughness) * 0.8f;
            }
        }

        public static void Tones(float[] data, float[] notes, float noteLength, float decay, bool square)
        {
            for (var i = 0; i < data.Length; i++)
            {
                var t = i / (float)Rate;
                var index = Mathf.Min(notes.Length - 1, (int)(t / noteLength));
                var local = t - index * noteLength;
                var phase = notes[index] * t % 1f;
                var wave = square ? (phase < 0.5f ? 0.5f : -0.5f) : Mathf.Sin(2f * Mathf.PI * phase);
                data[i] = wave * Mathf.Exp(-local * decay) * Mathf.Min(1f, local * 400f);
            }
        }

        public static void Voice(float[] data, int seed)
        {
            var random = new System.Random(seed);
            var syllable = 0.17f;
            var pitch = 150f;
            var phase = 0f;
            for (var i = 0; i < data.Length; i++)
            {
                var t = i / (float)Rate;
                var index = (int)(t / syllable);
                var local = t / syllable - index;
                if (local < 1f / (syllable * Rate))
                    pitch = 130f + (float)random.NextDouble() * 50f;
                phase = (phase + pitch / Rate) % 1f;
                var formant = Mathf.Sin(2f * Mathf.PI * phase) + 0.5f * Mathf.Sin(2f * Mathf.PI * phase * (3f + index % 3));
                var gap = index % 7 == 6 ? 0f : 1f;
                var tail = Mathf.Clamp01((data.Length - i) / (0.03f * Rate));
                data[i] = formant * Mathf.Sin(Mathf.PI * local) * gap * tail * 0.45f;
            }
        }

        public static void Normalize(float[] data, float peak)
        {
            var max = 0.0001f;
            foreach (var sample in data)
                max = Mathf.Max(max, Mathf.Abs(sample));
            for (var i = 0; i < data.Length; i++)
                data[i] = data[i] / max * peak;
        }

        public static void WriteWav(string path, float[] data)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            using var stream = new FileStream(path, FileMode.Create);
            using var writer = new BinaryWriter(stream);
            writer.Write(new[] { 'R', 'I', 'F', 'F' });
            writer.Write(36 + data.Length * 2);
            writer.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write(Rate);
            writer.Write(Rate * 2);
            writer.Write((short)2);
            writer.Write((short)16);
            writer.Write(new[] { 'd', 'a', 't', 'a' });
            writer.Write(data.Length * 2);
            foreach (var sample in data)
                writer.Write((short)Math.Round(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue));
        }

        #endregion


        #region Private and Protected

        private static float Noise(System.Random random) => (float)random.NextDouble() * 2f - 1f;

        private static float[] LoopCrossfade(float[] data, int length)
        {
            length = Mathf.Min(length, data.Length / 2);
            var tail = data.Length - length;
            for (var i = 0; i < length; i++)
            {
                var mix = i / (float)length;
                data[i] = data[i] * mix + data[tail + i] * (1f - mix);
            }
            Array.Resize(ref data, tail);
            return data;
        }

        #endregion
    }
}

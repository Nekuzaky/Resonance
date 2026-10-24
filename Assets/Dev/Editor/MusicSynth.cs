using UnityEngine;

namespace Nekuzaky.Resonance.Dev
{
    internal static class MusicSynth
    {
        #region Public

        public const float Bpm = 100f;
        public const float Beat = 60f / Bpm;
        public const float Bar = Beat * 4f;

        private static readonly float[][] Chords =
        {
            new[] { 220f, 261.63f, 329.63f },
            new[] { 174.61f, 220f, 261.63f },
            new[] { 261.63f, 329.63f, 392f },
            new[] { 196f, 246.94f, 293.66f }
        };

        private static readonly float[] Roots = { 110f, 87.31f, 130.81f, 98f };

        public static float[] Explore()
        {
            var data = Synth.Buffer(Bar * 4f);
            for (var bar = 0; bar < 4; bar++)
            {
                var start = bar * Bar;
                foreach (var note in Chords[bar])
                    Pad(data, start, Bar, note, 0.16f);
                Tone(data, start, Bar * 0.9f, Roots[bar] * 0.5f, 0.25f, 1.2f, false);
                for (var beat = 0; beat < 4; beat++)
                {
                    if (beat % 2 == 0)
                        Kick(data, start + beat * Beat, 0.5f);
                    Hat(data, start + beat * Beat + Beat * 0.5f, 0.08f, bar * 4 + beat);
                }
            }
            Synth.Normalize(data, 0.8f);
            return data;
        }

        public static float[] Combat()
        {
            var data = Synth.Buffer(Bar * 4f);
            for (var bar = 0; bar < 4; bar++)
            {
                var start = bar * Bar;
                Pad(data, start, Bar, Chords[bar][0] * 2f, 0.07f);
                for (var eighth = 0; eighth < 8; eighth++)
                {
                    var time = start + eighth * Beat * 0.5f;
                    Tone(data, time, Beat * 0.45f, Roots[bar], 0.3f, 9f, true);
                    Hat(data, time, 0.06f, bar * 8 + eighth);
                }
                for (var beat = 0; beat < 4; beat++)
                {
                    Kick(data, start + beat * Beat, 0.8f);
                    if (beat % 2 == 1)
                        Snare(data, start + beat * Beat, 0.45f, bar * 4 + beat);
                }
            }
            Synth.Normalize(data, 0.85f);
            return data;
        }

        public static float[] Stinger()
        {
            var data = Synth.Buffer(Bar);
            var notes = new[] { 440f, 523.25f, 659.25f, 880f };
            for (var i = 0; i < notes.Length; i++)
                Tone(data, i * Beat * 0.25f, Bar - i * Beat * 0.25f, notes[i], 0.3f, 2.5f, true);
            Snare(data, 0f, 0.3f, 99);
            Synth.Normalize(data, 0.8f);
            return data;
        }

        #endregion


        #region Private and Protected

        private static void Tone(float[] data, float start, float length, float frequency, float amplitude, float decay, bool square)
        {
            var first = Mathf.RoundToInt(start * Synth.Rate);
            var count = Mathf.RoundToInt(length * Synth.Rate);
            for (var i = 0; i < count && first + i < data.Length; i++)
            {
                var t = i / (float)Synth.Rate;
                var phase = frequency * t % 1f;
                var wave = square ? (phase < 0.5f ? 0.6f : -0.6f) : Mathf.Sin(2f * Mathf.PI * phase);
                var release = Mathf.Clamp01((count - i) / (Synth.Rate * 0.02f));
                data[first + i] += wave * amplitude * Mathf.Exp(-t * decay) * Mathf.Min(1f, t * 300f) * release;
            }
        }

        private static void Pad(float[] data, float start, float length, float frequency, float amplitude)
        {
            var first = Mathf.RoundToInt(start * Synth.Rate);
            var count = Mathf.RoundToInt(length * Synth.Rate);
            for (var i = 0; i < count && first + i < data.Length; i++)
            {
                var t = i / (float)Synth.Rate;
                var envelope = Mathf.Min(1f, t / 0.25f) * Mathf.Clamp01((length - t) / 0.3f);
                var wave = Mathf.Sin(2f * Mathf.PI * frequency * t) + 0.3f * Mathf.Sin(2f * Mathf.PI * frequency * 2.003f * t);
                data[first + i] += wave * amplitude * envelope;
            }
        }

        private static void Kick(float[] data, float start, float amplitude)
        {
            var first = Mathf.RoundToInt(start * Synth.Rate);
            var phase = 0f;
            for (var i = 0; i < Synth.Rate / 3 && first + i < data.Length; i++)
            {
                var t = i / (float)Synth.Rate;
                phase += (50f + 90f * Mathf.Exp(-t * 25f)) / Synth.Rate;
                data[first + i] += Mathf.Sin(2f * Mathf.PI * phase) * amplitude * Mathf.Exp(-t * 9f);
            }
        }

        private static void Snare(float[] data, float start, float amplitude, int seed)
        {
            var random = new System.Random(seed);
            var first = Mathf.RoundToInt(start * Synth.Rate);
            for (var i = 0; i < Synth.Rate / 4 && first + i < data.Length; i++)
            {
                var t = i / (float)Synth.Rate;
                var noise = (float)random.NextDouble() * 2f - 1f;
                var body = Mathf.Sin(2f * Mathf.PI * 185f * t) * Mathf.Exp(-t * 30f);
                data[first + i] += (noise * Mathf.Exp(-t * 18f) + body * 0.6f) * amplitude;
            }
        }

        private static void Hat(float[] data, float start, float amplitude, int seed)
        {
            var random = new System.Random(seed + 1000);
            var first = Mathf.RoundToInt(start * Synth.Rate);
            var previous = 0f;
            for (var i = 0; i < Synth.Rate / 20 && first + i < data.Length; i++)
            {
                var t = i / (float)Synth.Rate;
                var noise = (float)random.NextDouble() * 2f - 1f;
                data[first + i] += (noise - previous) * amplitude * Mathf.Exp(-t * 60f);
                previous = noise;
            }
        }

        #endregion
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Nekuzaky.Resonance
{
    internal static class ClipEnvelopes
    {
        #region Public

        public const int Window = 1024;
        public const int WindowsPerCall = 8;
        public const float Unknown = 0.25f;

        public static float At(AudioClip clip, int sample)
        {
            var entry = Entry(clip);
            if (entry == null || entry.Values.Length == 0)
                return Unknown;
            Advance(entry, clip, WindowsPerCall);
            var index = Mathf.Clamp(sample / Window, 0, entry.Values.Length - 1);
            return index < entry.Done ? entry.Values[index] : Unknown;
        }

        public static float[] Get(AudioClip clip)
        {
            var entry = Entry(clip);
            if (entry == null)
                return null;
            Advance(entry, clip, entry.Values.Length);
            return entry.Values;
        }

        public static void Clear() => Cache.Clear();

        #endregion


        #region Private and Protected

        private sealed class Envelope
        {
            public float[] Values;
            public float[] Buffer;
            public int Done;
            public bool Failed;
        }

        private static readonly Dictionary<AudioClip, Envelope> Cache = new();
        private static bool Readable => Application.platform != RuntimePlatform.WebGLPlayer;

        private static Envelope Entry(AudioClip clip)
        {
            if (clip == null)
                return null;
            if (Cache.TryGetValue(clip, out var cached))
                return cached.Failed ? null : cached;
            var entry = new Envelope();
            Cache[clip] = entry;
            if (!Readable || clip.loadType == AudioClipLoadType.Streaming || clip.samples <= 0 ||
                (clip.loadState != AudioDataLoadState.Loaded && !clip.LoadAudioData()))
            {
                entry.Failed = true;
                return null;
            }
            entry.Values = new float[Mathf.Max(1, (clip.samples + Window - 1) / Window)];
            entry.Buffer = new float[Window * Mathf.Max(1, clip.channels)];
            return entry;
        }

        private static void Advance(Envelope entry, AudioClip clip, int windows)
        {
            for (var step = 0; step < windows && entry.Done < entry.Values.Length; step++)
            {
                var index = entry.Done;
                var offset = index * Window;
                if (offset + Window > clip.samples || !clip.GetData(entry.Buffer, offset))
                {
                    entry.Values[index] = index > 0 ? entry.Values[index - 1] : Unknown;
                }
                else
                {
                    var sum = 0f;
                    foreach (var sample in entry.Buffer)
                        sum += sample * sample;
                    entry.Values[index] = Mathf.Sqrt(sum / entry.Buffer.Length);
                }
                entry.Done++;
            }
        }

        #endregion
    }
}

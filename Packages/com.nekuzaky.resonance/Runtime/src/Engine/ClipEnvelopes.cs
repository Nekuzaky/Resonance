using System.Collections.Generic;
using UnityEngine;

namespace Nekuzaky.Resonance
{
    internal static class ClipEnvelopes
    {
        #region Public

        public const int Window = 1024;
        public const float Unknown = 0.25f;

        public static float At(AudioClip clip, int sample)
        {
            var envelope = Get(clip);
            if (envelope == null || envelope.Length == 0)
                return Unknown;
            return envelope[Mathf.Clamp(sample / Window, 0, envelope.Length - 1)];
        }

        public static float[] Get(AudioClip clip)
        {
            if (clip == null)
                return null;
            if (Cache.TryGetValue(clip, out var cached))
                return cached;
            var envelope = Compute(clip);
            Cache[clip] = envelope;
            return envelope;
        }

        public static void Clear() => Cache.Clear();

        #endregion


        #region Private and Protected

        private static readonly Dictionary<AudioClip, float[]> Cache = new();

        private static float[] Compute(AudioClip clip)
        {
            if (clip.loadType == AudioClipLoadType.Streaming || clip.samples <= 0)
                return null;
            if (clip.loadState != AudioDataLoadState.Loaded && !clip.LoadAudioData())
                return null;

            var channels = Mathf.Max(1, clip.channels);
            var windows = Mathf.Max(1, (clip.samples + Window - 1) / Window);
            var envelope = new float[windows];
            var buffer = new float[Window * channels];
            for (var index = 0; index < windows; index++)
            {
                var offset = index * Window;
                if (offset + Window > clip.samples || !clip.GetData(buffer, offset))
                {
                    envelope[index] = index > 0 ? envelope[index - 1] : Unknown;
                    continue;
                }
                var sum = 0f;
                foreach (var sample in buffer)
                    sum += sample * sample;
                envelope[index] = Mathf.Sqrt(sum / buffer.Length);
            }
            return envelope;
        }

        #endregion
    }
}

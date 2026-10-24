using UnityEngine;

namespace Nekuzaky.Resonance.Dev
{
    internal static class Smoke
    {
        #region Public

        public static int Failures { get; private set; }

        public static int Pending { get; set; }

        public static void Check(bool condition, string message)
        {
            Debug.Log($"[Smoke] {(condition ? "PASS" : "FAIL")} {message}");
            if (!condition)
                Failures++;
        }

        public static AudioClip Tone(string name, float frequency, float seconds)
        {
            const int rate = 48000;
            var data = new float[Mathf.RoundToInt(seconds * rate)];
            for (var i = 0; i < data.Length; i++)
                data[i] = 0.5f * Mathf.Sin(2f * Mathf.PI * frequency * i / rate);
            var clip = AudioClip.Create(name, data.Length, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static SoundEvent Event(AudioClip clip, float volume, bool loop)
        {
            var sound = ScriptableObject.CreateInstance<SoundEvent>();
            sound.name = clip.name;
            sound.volume = volume;
            sound.root = new ClipNode { name = clip.name, clip = clip, loop = loop, randomStart = loop };
            return sound;
        }

        #endregion
    }
}

using System;
using UnityEngine;
using UnityEngine.Events;

namespace Nekuzaky.Resonance
{
    [Serializable]
    public sealed class VolumeChangedEvent : UnityEvent<float>
    {
    }

    [AddComponentMenu("Resonance/Volume Setting")]
    [HelpURL("https://github.com/Nekuzaky/Resonance/blob/main/Packages/com.nekuzaky.resonance/Documentation~/components.md")]
    public sealed class VolumeSetting : MonoBehaviour
    {
        #region Public

        public const string KeyPrefix = "Resonance.Volume.";

        public AudioBus bus;
        [Range(0f, 1f)] public float defaultVolume = 0.8f;
        public bool saveBetweenSessions = true;
        public VolumeChangedEvent onLoaded = new();

        public float Volume { get; private set; } = 1f;

        public static float ToDecibels(float volume) => volume <= 0.0001f ? AudioMath.SilenceDb : Mathf.Max(AudioMath.SilenceDb, 40f * Mathf.Log10(volume));

        public void SetVolume(float volume)
        {
            Volume = Mathf.Clamp01(volume);
            Resonance.SetBusVolume(bus, ToDecibels(Volume));
            if (saveBetweenSessions && bus != null)
                PlayerPrefs.SetFloat(Key, Volume);
        }

        public void SetMuted(bool muted) => Resonance.SetBusVolume(bus, muted ? AudioMath.SilenceDb : ToDecibels(Volume));

        public void ResetToDefault() => SetVolume(defaultVolume);

        #endregion


        #region Private and Protected

        private string Key => KeyPrefix + (bus != null ? bus.name : string.Empty);

        #endregion


        #region Unity Callbacks

        private void Start()
        {
            var saved = saveBetweenSessions && bus != null ? PlayerPrefs.GetFloat(Key, defaultVolume) : defaultVolume;
            SetVolume(saved);
            onLoaded.Invoke(Volume);
        }

        #endregion
    }
}

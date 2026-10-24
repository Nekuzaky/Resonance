using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekuzaky.Resonance
{
    [AddComponentMenu("Resonance/Music Source")]
    [HelpURL("https://github.com/Nekuzaky/Resonance/blob/main/Packages/com.nekuzaky.resonance/Documentation~/components.md")]
    public sealed class MusicSource : MonoBehaviour
    {
        public MusicTrack track;
        public bool playOnStart = true;
        public bool stopOnDisable = true;
        [Min(0f)] public float stopFade = 1f;

        public void Play() => Resonance.PlayMusic(track);

        public void Stop() => Resonance.StopMusic(stopFade);

        public void PlaySegment(string segment) => Resonance.PlayMusicSegment(segment, track != null ? track.transitionSync : MusicSync.NextBar);

        public void PlayStinger(string stinger) => Resonance.PlayStinger(stinger);

        private void Start()
        {
            if (playOnStart)
                Play();
        }

        private void OnDisable()
        {
            if (stopOnDisable && gameObject.scene.isLoaded && Resonance.CurrentMusic == track)
                Stop();
        }
    }
}

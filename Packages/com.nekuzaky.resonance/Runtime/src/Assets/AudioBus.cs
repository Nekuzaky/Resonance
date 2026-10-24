using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Nekuzaky.Resonance
{
    [CreateAssetMenu(menuName = "Resonance/Bus", fileName = "New Bus", order = 40)]
    public sealed class AudioBus : ScriptableObject
    {
        public const int MaxDepth = 16;

        public AudioBus parent;
        [Range(-80f, 12f)] public float volume;
        public bool mute;
        public AudioMixerGroup output;
        public List<DuckRule> ducking = new();

        public AudioMixerGroup ResolveOutput()
        {
            var bus = this;
            for (var depth = 0; depth < MaxDepth && bus != null; depth++)
            {
                if (bus.output != null)
                    return bus.output;
                bus = bus.parent;
            }
            return null;
        }

        public bool IsDescendantOf(AudioBus ancestor)
        {
            var bus = this;
            for (var depth = 0; depth < MaxDepth && bus != null; depth++)
            {
                if (bus == ancestor)
                    return true;
                bus = bus.parent;
            }
            return false;
        }
    }
}

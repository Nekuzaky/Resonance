using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Nekuzaky.Resonance
{
    [Serializable]
    public sealed class DuckRule
    {
        public AudioBus target;
        [Range(-48f, 0f)] public float amount = -9f;
        [Min(0f)] public float attack = 0.1f;
        [Min(0f)] public float release = 0.6f;
    }

    [Serializable]
    public sealed class BusOffset
    {
        public AudioBus bus;
        [Range(-80f, 12f)] public float volume;
    }
}

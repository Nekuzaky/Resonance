using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Nekuzaky.Resonance
{
    [CreateAssetMenu(menuName = "Resonance/Mix Snapshot", fileName = "New Mix Snapshot", order = 41)]
    public sealed class MixSnapshot : ScriptableObject
    {
        [Min(0f)] public float fadeIn = 0.5f;
        [Min(0f)] public float fadeOut = 0.5f;
        public List<BusOffset> offsets = new();
    }
}

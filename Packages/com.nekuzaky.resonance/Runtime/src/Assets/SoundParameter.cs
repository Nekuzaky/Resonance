using System.Collections.Generic;
using UnityEngine;

namespace Nekuzaky.Resonance
{
    [CreateAssetMenu(menuName = "Resonance/Parameter", fileName = "New Parameter", order = 20)]
    public sealed class SoundParameter : ScriptableObject
    {
        public float min;
        public float max = 1f;
        public float defaultValue;
        public ParameterScope scope = ParameterScope.Global;
        [Min(0f)] public float seekSpeed;

        public float Clamp(float value) => Mathf.Clamp(value, Mathf.Min(min, max), Mathf.Max(min, max));

        public float Normalize(float value) => Mathf.Approximately(min, max) ? 0f : Mathf.Clamp01((value - min) / (max - min));
    }
}

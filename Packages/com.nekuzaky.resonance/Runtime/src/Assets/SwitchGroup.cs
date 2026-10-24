using System.Collections.Generic;
using UnityEngine;

namespace Nekuzaky.Resonance
{
    [CreateAssetMenu(menuName = "Resonance/Switch Group", fileName = "New Switch Group", order = 21)]
    public sealed class SwitchGroup : ScriptableObject
    {
        public List<string> values = new() { "Default" };
        [Min(0)] public int defaultIndex;

        public string DefaultValue => values.Count == 0 ? string.Empty : values[Mathf.Clamp(defaultIndex, 0, values.Count - 1)];
    }
}

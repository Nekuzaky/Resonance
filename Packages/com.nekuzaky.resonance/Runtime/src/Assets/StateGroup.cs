using System.Collections.Generic;
using UnityEngine;

namespace Nekuzaky.Resonance
{
    [CreateAssetMenu(menuName = "Resonance/State Group", fileName = "New State Group", order = 22)]
    public sealed class StateGroup : ScriptableObject
    {
        public List<string> states = new() { "None" };
        [Min(0)] public int defaultIndex;

        public string DefaultState => states.Count == 0 ? string.Empty : states[Mathf.Clamp(defaultIndex, 0, states.Count - 1)];
    }
}

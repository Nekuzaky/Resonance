using UnityEngine;

namespace Nekuzaky.Resonance
{
    [CreateAssetMenu(menuName = "Resonance/Attenuation Preset", fileName = "New Attenuation", order = 23)]
    public sealed class AttenuationPreset : ScriptableObject
    {
        public Spatial spatial = new();
    }
}

using UnityEngine;

namespace Nekuzaky.Resonance
{
    [AddComponentMenu("Resonance/Switch Setter")]
    [HelpURL("https://github.com/Nekuzaky/Resonance/blob/main/Packages/com.nekuzaky.resonance/Documentation~/components.md")]
    public sealed class SwitchSetter : MonoBehaviour
    {
        public SwitchGroup group;
        public string value = string.Empty;
        public bool global;

        public void SetValue(string newValue)
        {
            value = newValue;
            Apply();
        }

        public void Apply()
        {
            if (group != null)
                Resonance.SetSwitch(group, value, global ? null : gameObject);
        }

        private void Start() => Apply();
    }
}

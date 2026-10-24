using UnityEngine;

namespace Nekuzaky.Resonance
{
    [AddComponentMenu("Resonance/Sound Listener")]
    [DisallowMultipleComponent]
    [HelpURL("https://github.com/Nekuzaky/Resonance/blob/main/Packages/com.nekuzaky.resonance/Documentation~/components.md")]
    public sealed class SoundListener : MonoBehaviour
    {
        public static SoundListener Current { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Current = null;

        private void OnEnable() => Current = this;

        private void OnDisable()
        {
            if (Current == this)
                Current = null;
        }
    }
}

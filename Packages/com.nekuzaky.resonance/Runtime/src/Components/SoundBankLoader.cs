using UnityEngine;

namespace Nekuzaky.Resonance
{
    [AddComponentMenu("Resonance/Sound Bank Loader")]
    [HelpURL("https://github.com/Nekuzaky/Resonance/blob/main/Packages/com.nekuzaky.resonance/Documentation~/memory.md")]
    public sealed class SoundBankLoader : MonoBehaviour
    {
        public SoundBank bank;
        public bool unloadOnDisable = true;

        public bool IsReady => Resonance.IsBankReady(bank);

        private void OnEnable() => Resonance.LoadBank(bank);

        private void OnDisable()
        {
            if (unloadOnDisable)
                Resonance.UnloadBank(bank);
        }
    }
}

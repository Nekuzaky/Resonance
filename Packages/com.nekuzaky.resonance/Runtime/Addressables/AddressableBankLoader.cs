using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Nekuzaky.Resonance
{
    [Serializable]
    public sealed class SoundBankReference : AssetReferenceT<SoundBank>
    {
        public SoundBankReference(string guid) : base(guid)
        {
        }
    }

    [AddComponentMenu("Resonance/Addressable Bank Loader")]
    [HelpURL("https://github.com/Nekuzaky/Resonance/blob/main/Packages/com.nekuzaky.resonance/Documentation~/memory.md")]
    public sealed class AddressableBankLoader : MonoBehaviour
    {
        #region Public

        public SoundBankReference bank;
        public bool unloadOnDisable = true;

        public SoundBank Loaded { get; private set; }

        public bool IsReady => Loaded != null && Resonance.IsBankReady(Loaded);

        #endregion


        #region Private and Protected

        private AsyncOperationHandle<SoundBank> _handle;

        private void OnLoaded(AsyncOperationHandle<SoundBank> handle)
        {
            if (handle.Status != AsyncOperationStatus.Succeeded || !isActiveAndEnabled)
                return;
            Loaded = handle.Result;
            Resonance.LoadBank(Loaded);
        }

        #endregion


        #region Unity Callbacks

        private void OnEnable()
        {
            if (bank == null || !bank.RuntimeKeyIsValid())
                return;
            _handle = bank.LoadAssetAsync<SoundBank>();
            _handle.Completed += OnLoaded;
        }

        private void OnDisable()
        {
            if (!unloadOnDisable)
                return;
            if (Loaded != null)
                Resonance.UnloadBank(Loaded);
            Loaded = null;
            if (_handle.IsValid())
                bank.ReleaseAsset();
        }

        #endregion
    }
}

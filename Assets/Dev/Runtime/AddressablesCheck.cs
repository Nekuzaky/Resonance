using System.Collections.Generic;
using UnityEngine;

namespace Nekuzaky.Resonance.Dev
{
    [AddComponentMenu("")]
    internal sealed class AddressablesCheck : MonoBehaviour
    {
        #region Private and Protected

        private const string DemoBank = "591cdff1a8691df4c9b3bc9ff1a1fa3a";
        private const float Timeout = 8f;

        private AddressableBankLoader _loader;
        private float _start;
        private int _step;
        private int _banksBefore;

        private void Loaded()
        {
            var engine = Resonance.Engine;
            var bank = _loader.Loaded;
            Smoke.Check(engine.Memory.Banks.Count == _banksBefore + 1, "the Addressable Bank Loader loaded the demo bank from its bundle");
            var loaded = 0;
            var clips = new HashSet<AudioClip>();
            bank.CollectClips(clips);
            foreach (var clip in clips)
            {
                if (clip.loadState == AudioDataLoadState.Loaded)
                    loaded++;
            }
            Smoke.Check(clips.Count > 10 && loaded == clips.Count, $"{loaded} of {clips.Count} bundled clips are in memory");
            Destroy(_loader.gameObject);
        }

        #endregion


        #region Unity Callbacks

        private void Start()
        {
            Smoke.Pending++;
            _start = Time.time;
            var host = new GameObject("Addressable Demo Bank");
            host.SetActive(false);
            _loader = host.AddComponent<AddressableBankLoader>();
            _loader.bank = new SoundBankReference(DemoBank);
        }

        private void Update()
        {
            var elapsed = Time.time - _start;
            if (_step == 0 && elapsed >= 2f)
            {
                _banksBefore = Resonance.Engine.Memory.Banks.Count;
                _loader.gameObject.SetActive(true);
                _step = 1;
            }
            else if (_step == 1 && _loader.IsReady)
            {
                Loaded();
                _step = 2;
            }
            else if (_step == 1 && elapsed >= Timeout)
            {
                Smoke.Check(false, "the Addressable Bank Loader did not finish loading");
                Smoke.Pending--;
                _step = 3;
            }
            else if (_step == 2)
            {
                Smoke.Check(Resonance.Engine.Memory.Banks.Count == _banksBefore, "destroying the loader unloaded the bank");
                Smoke.Pending--;
                _step = 3;
            }
        }

        #endregion
    }
}

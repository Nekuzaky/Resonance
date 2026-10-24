using System.Collections.Generic;
using UnityEngine;

namespace Nekuzaky.Resonance
{
    internal sealed class AudioMemory
    {
        #region Public

        public IReadOnlyDictionary<AudioClip, int> Clips => _references;

        public IReadOnlyDictionary<SoundBank, int> Banks => _banks;

        public int OnDemandLoads { get; private set; }

        public void Load(SoundBank bank)
        {
            if (bank == null)
                return;
            _banks.TryGetValue(bank, out var count);
            _banks[bank] = count + 1;
            if (count > 0)
                return;
            _scratch.Clear();
            bank.CollectClips(_scratch);
            foreach (var clip in _scratch)
                Retain(clip);
        }

        public void Unload(SoundBank bank)
        {
            if (bank == null || !_banks.TryGetValue(bank, out var count))
                return;
            if (count > 1)
            {
                _banks[bank] = count - 1;
                return;
            }
            _banks.Remove(bank);
            _scratch.Clear();
            bank.CollectClips(_scratch);
            foreach (var clip in _scratch)
                Release(clip);
        }

        public bool IsLoaded(SoundBank bank) => bank != null && _banks.ContainsKey(bank);

        public bool IsReady(SoundBank bank)
        {
            if (!IsLoaded(bank))
                return false;
            _scratch.Clear();
            bank.CollectClips(_scratch);
            foreach (var clip in _scratch)
            {
                if (clip.loadState != AudioDataLoadState.Loaded && clip.loadState != AudioDataLoadState.Failed)
                    return false;
            }
            return true;
        }

        public void NoteOnDemand(AudioClip clip)
        {
            if (clip != null && clip.loadState == AudioDataLoadState.Unloaded && clip.loadType != AudioClipLoadType.Streaming)
                OnDemandLoads++;
        }

        public void Clear()
        {
            foreach (var pair in _references)
            {
                if (pair.Key != null)
                    pair.Key.UnloadAudioData();
            }
            _references.Clear();
            _banks.Clear();
        }

        #endregion


        #region Private and Protected

        private readonly Dictionary<AudioClip, int> _references = new();
        private readonly Dictionary<SoundBank, int> _banks = new();
        private readonly HashSet<AudioClip> _scratch = new();

        private void Retain(AudioClip clip)
        {
            _references.TryGetValue(clip, out var count);
            _references[clip] = count + 1;
            if (count == 0 && clip.loadState == AudioDataLoadState.Unloaded)
                clip.LoadAudioData();
        }

        private void Release(AudioClip clip)
        {
            if (!_references.TryGetValue(clip, out var count))
                return;
            if (count > 1)
            {
                _references[clip] = count - 1;
                return;
            }
            _references.Remove(clip);
            clip.UnloadAudioData();
        }

        #endregion
    }
}

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

        public int PendingUnloads => _deferred.Count;

        internal System.Action<AudioClip> UnloadData = clip => clip.UnloadAudioData();

        internal System.Func<AudioClip, bool> InUse;

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
            var held = new List<AudioClip>(_scratch);
            _held[bank] = held;
            foreach (var clip in held)
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
            if (!_held.Remove(bank, out var held))
                return;
            foreach (var clip in held)
                Release(clip);
        }

        public bool IsLoaded(SoundBank bank) => bank != null && _banks.ContainsKey(bank);

        public bool IsReady(SoundBank bank)
        {
            if (!IsLoaded(bank) || !_held.TryGetValue(bank, out var held))
                return false;
            foreach (var clip in held)
            {
                if (clip != null && clip.loadState != AudioDataLoadState.Loaded && clip.loadState != AudioDataLoadState.Failed)
                    return false;
            }
            return true;
        }

        public void NoteOnDemand(AudioClip clip)
        {
            if (clip != null && clip.loadState == AudioDataLoadState.Unloaded && clip.loadType != AudioClipLoadType.Streaming)
                OnDemandLoads++;
        }

        public void Tick()
        {
            if (_deferred.Count == 0)
                return;
            _released.Clear();
            foreach (var clip in _deferred)
            {
                if (clip == null || InUse == null || !InUse(clip))
                    _released.Add(clip);
            }
            foreach (var clip in _released)
            {
                _deferred.Remove(clip);
                if (clip != null)
                    UnloadData(clip);
            }
        }

        public void Clear()
        {
            foreach (var pair in _references)
            {
                if (pair.Key != null)
                    UnloadData(pair.Key);
            }
            foreach (var clip in _deferred)
            {
                if (clip != null)
                    UnloadData(clip);
            }
            _references.Clear();
            _deferred.Clear();
            _banks.Clear();
            _held.Clear();
        }

        #endregion


        #region Private and Protected

        private readonly Dictionary<AudioClip, int> _references = new();
        private readonly Dictionary<SoundBank, int> _banks = new();
        private readonly Dictionary<SoundBank, List<AudioClip>> _held = new();
        private readonly HashSet<AudioClip> _deferred = new();
        private readonly List<AudioClip> _released = new();
        private readonly HashSet<AudioClip> _scratch = new();

        private void Retain(AudioClip clip)
        {
            if (clip == null)
                return;
            _references.TryGetValue(clip, out var count);
            _references[clip] = count + 1;
            if (count > 0 || _deferred.Remove(clip))
                return;
            if (clip.loadState == AudioDataLoadState.Unloaded)
                clip.LoadAudioData();
        }

        private void Release(AudioClip clip)
        {
            if (clip == null || !_references.TryGetValue(clip, out var count))
                return;
            if (count > 1)
            {
                _references[clip] = count - 1;
                return;
            }
            _references.Remove(clip);
            if (InUse != null && InUse(clip))
                _deferred.Add(clip);
            else
                UnloadData(clip);
        }

        #endregion
    }
}

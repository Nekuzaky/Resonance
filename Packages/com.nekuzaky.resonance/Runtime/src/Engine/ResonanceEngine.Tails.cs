using System.Collections.Generic;
using UnityEngine;

namespace Nekuzaky.Resonance
{
    public sealed partial class ResonanceEngine
    {
        #region Public

        public const int MaxRingingVoices = 32;

        #endregion


        #region Private and Protected

        internal int RingingVoices => _ringing.Count;

        private readonly List<Voice> _ringing = new();
        private static AudioClip _silence;

        private static AudioClip Silence
        {
            get
            {
                if (_silence == null)
                {
                    _silence = AudioClip.Create("Resonance Silence", 4410, 1, 44100, false);
                    _silence.hideFlags = HideFlags.HideAndDontSave;
                }
                return _silence;
            }
        }

        private bool Ring(Voice voice)
        {
            var source = voice.Source;
            var tail = voice.Effects.Tail;
            if (voice.Virtual || tail <= 0f || source == null || source.clip == null || _ringing.Count >= MaxRingingVoices)
                return false;
            voice.Owner = null;
            voice.Layer = null;
            voice.Finished = true;
            voice.Level = 0f;
            source.clip = Silence;
            source.loop = true;
            source.priority = 256;
            source.Play();
            voice.RingUntil = _time + tail;
            _ringing.Add(voice);
            return true;
        }

        private void TickRinging()
        {
            for (var i = _ringing.Count - 1; i >= 0; i--)
            {
                var voice = _ringing[i];
                if (voice.Source != null && _time < voice.RingUntil)
                    continue;
                _ringing.RemoveAt(i);
                voice.Reset();
                _free.Push(voice);
            }
        }

        private Voice TakeRinging()
        {
            if (_ringing.Count == 0 || _voices.Count < MaxVoices)
                return null;
            var voice = _ringing[0];
            _ringing.RemoveAt(0);
            voice.Reset();
            return voice;
        }

        private void CutTails()
        {
            for (var i = _ringing.Count - 1; i >= 0; i--)
            {
                _ringing[i].Reset();
                _free.Push(_ringing[i]);
            }
            _ringing.Clear();
        }

        #endregion
    }
}

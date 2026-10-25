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
        private static bool TailsAudible => Application.platform != RuntimePlatform.WebGLPlayer;

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
            if (!TailsAudible || voice.Virtual || tail <= 0f || source == null || source.clip == null || _ringing.Count >= MaxRingingVoices)
                return false;
            var owner = voice.Owner;
            voice.RingBus = owner != null ? owner.Event.bus : null;
            voice.RingEmitter = owner != null ? owner.Emitter : null;
            voice.RingGain = Mixer.Gain(voice.RingBus);
            voice.RingFade = 1f;
            voice.RingFadeTime = -1f;
            voice.Owner = null;
            voice.Layer = null;
            voice.Finished = true;
            voice.Level = 0f;
            source.clip = Silence;
            source.loop = true;
            source.priority = 256;
            source.Play();
            voice.Effects.Follow(1f);
            voice.RingUntil = _time + tail;
            _ringing.Add(voice);
            return true;
        }

        private void TickRinging(float deltaTime)
        {
            for (var i = _ringing.Count - 1; i >= 0; i--)
            {
                var voice = _ringing[i];
                if (voice.RingFadeTime >= 0f)
                    voice.RingFade = AudioMath.Approach(voice.RingFade, 0f, voice.RingFadeTime, 1f, deltaTime);
                if (voice.Source != null && _time < voice.RingUntil && voice.RingFade > 0f)
                {
                    voice.Effects.Follow(TailGain(voice) * voice.RingFade);
                    continue;
                }
                _ringing.RemoveAt(i);
                Recycle(voice);
                _free.Push(voice);
            }
        }

        private float TailGain(Voice voice)
        {
            var gain = Mixer.Gain(voice.RingBus);
            if (voice.RingGain > 0.000001f)
                return gain / voice.RingGain;
            return gain > 0f ? 1f : 0f;
        }

        private void StopTails(GameObject emitter, float fade)
        {
            for (var i = _ringing.Count - 1; i >= 0; i--)
            {
                var voice = _ringing[i];
                if (!ReferenceEquals(emitter, null) && !ReferenceEquals(voice.RingEmitter, emitter))
                    continue;
                if (fade > 0f)
                {
                    voice.RingFadeTime = voice.RingFadeTime < 0f ? fade : Mathf.Min(voice.RingFadeTime, fade);
                    continue;
                }
                _ringing.RemoveAt(i);
                Recycle(voice);
                _free.Push(voice);
            }
        }

        private Voice TakeRinging()
        {
            if (_ringing.Count == 0 || _voices.Count < MaxVoices)
                return null;
            var voice = _ringing[0];
            _ringing.RemoveAt(0);
            Recycle(voice);
            return voice;
        }

        private static void Recycle(Voice voice)
        {
            voice.Reset();
            voice.Effects.Clear();
        }

        private void CutTails()
        {
            for (var i = _ringing.Count - 1; i >= 0; i--)
            {
                Recycle(_ringing[i]);
                _free.Push(_ringing[i]);
            }
            _ringing.Clear();
        }

        #endregion
    }
}

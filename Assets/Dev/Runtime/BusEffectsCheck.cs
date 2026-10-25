using UnityEngine;

namespace Nekuzaky.Resonance.Dev
{
    [AddComponentMenu("")]
    internal sealed class BusEffectsCheck : MonoBehaviour
    {
        #region Private and Protected

        private AudioBus _bus;
        private SoundEvent _sound;
        private SoundHandle _handle;
        private float _start;
        private int _step;
        private bool _active;
        private float _reduction;
        private float _tail;

        private void Begin()
        {
            _bus = ScriptableObject.CreateInstance<AudioBus>();
            _bus.name = "Smoke Effects";
            _bus.AddEffect(BusEffectType.HighPass).SetValue(BusEffects.FilterCutoff, 150f);
            _bus.AddEffect(BusEffectType.Distortion).SetValue(BusEffects.DistortionDrive, 0.3f);
            _bus.AddEffect(BusEffectType.LowPass).SetValue(BusEffects.FilterCutoff, 3000f);
            _bus.AddEffect(BusEffectType.Delay).SetValue(BusEffects.DelayTime, 200f);
            _bus.AddEffect(BusEffectType.Reverb).SetValue(BusEffects.ReverbDecay, 1f);
            _bus.AddEffect(BusEffectType.Compressor).SetValue(BusEffects.CompressorThreshold, -40f);
            _sound = Smoke.Event(Smoke.Tone("Effects Tone", 330f, 0.3f), -12f, false);
            _sound.spatialize = false;
            _sound.bus = _bus;
            _handle = Resonance.Play(_sound);
            _start = Time.time;
        }

        private void Watch(ResonanceEngine engine)
        {
            _reduction = Mathf.Max(_reduction, engine.Mixer.GainReduction(_bus));
            foreach (var instance in engine.Instances)
            {
                if (instance.Event != _sound)
                    continue;
                foreach (var voice in instance.Voices)
                {
                    _active |= voice.Effects.Active && voice.Effects.Dsp.enabled && voice.Effects.Dsp.Stages == 3 &&
                               voice.Effects.Echo.enabled && voice.Effects.Reverb.enabled;
                    _tail = Mathf.Max(_tail, voice.Effects.Tail);
                }
            }
        }

        private void Finish(ResonanceEngine engine)
        {
            Smoke.Check(engine.RingingVoices == 0, "the effect tail ended and its voice went back to the pool");
            Smoke.Pending--;
        }

        #endregion


        #region Unity Callbacks

        private void Start()
        {
            Smoke.Pending++;
            Begin();
        }

        private void Update()
        {
            var engine = Resonance.Engine;
            var elapsed = Time.time - _start;
            Watch(engine);
            if (_step == 0 && elapsed > 0.6f)
            {
                Smoke.Check(_active, "bus effects run on the voice: 3 DSP stages, delay and reverb");
                Smoke.Check(!Resonance.IsPlaying(_handle) && engine.RingingVoices > 0, "the voice keeps ringing for the delay and reverb tail after the sound ends");
                Smoke.Check(_reduction > 1f, $"the bus compressor reduced the gain by {_reduction:0.0} dB");
                _step = 1;
            }
            else if (_step == 1 && elapsed > 0.8f + _tail)
            {
                Finish(engine);
                _step = 2;
            }
        }

        #endregion
    }
}

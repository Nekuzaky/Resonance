using UnityEngine;

namespace Nekuzaky.Resonance
{
    internal sealed class EffectRack
    {
        #region Public

        public readonly EffectDsp Dsp;
        public readonly AudioEchoFilter Echo;
        public readonly AudioReverbFilter Reverb;

        public float Tail { get; private set; }

        public bool Active { get; private set; }

        public EffectRack(GameObject host)
        {
            Dsp = Add<EffectDsp>(host);
            Echo = Add<AudioEchoFilter>(host);
            Reverb = Add<AudioReverbFilter>(host);
            if (Reverb != null)
                SetupReverb(Reverb);
        }

        public void Apply(EffectChain chain)
        {
            if (!chain.Any && !Active)
                return;
            Active = chain.Any;
            Tail = chain.Tail;
            if (Dsp != null)
                Dsp.Configure(chain);
            ApplyEcho(chain);
            ApplyReverb(chain);
        }

        internal static float Millibels(float linear) => linear <= 0.00001f ? -10000f : Mathf.Max(-10000f, 2000f * Mathf.Log10(linear));

        #endregion


        #region Private and Protected

        private bool _echo;
        private float _echoTime = -1f;
        private float _feedback = -1f;
        private float _wet = -1f;
        private float _dry = -1f;
        private bool _reverb;
        private float _dryLevel = 1f;
        private float _room = 1f;
        private float _decay = -1f;
        private float _damping = -1f;
        private float _preDelay = -1f;

        private static T Add<T>(GameObject host) where T : Behaviour
        {
            var filter = host.AddComponent<T>();
            if (filter != null)
                filter.enabled = false;
            return filter;
        }

        private static void SetupReverb(AudioReverbFilter reverb)
        {
            reverb.reverbPreset = AudioReverbPreset.User;
            reverb.roomHF = -100f;
            reverb.roomLF = 0f;
            reverb.reflectionsLevel = -2602f;
            reverb.reflectionsDelay = 0.007f;
            reverb.reverbLevel = 200f;
            reverb.hfReference = 5000f;
            reverb.lfReference = 250f;
            reverb.diffusion = 100f;
            reverb.density = 100f;
        }

        private static bool Toggle(Behaviour filter, bool on, ref bool current)
        {
            if (filter == null)
                return false;
            if (current != on)
            {
                filter.enabled = on;
                current = on;
            }
            return on;
        }

        private void ApplyEcho(EffectChain chain)
        {
            if (!Toggle(Echo, chain.Delay, ref _echo))
                return;
            if (_echoTime != chain.DelayTime)
                Echo.delay = _echoTime = chain.DelayTime;
            if (_feedback != chain.Feedback)
                Echo.decayRatio = _feedback = chain.Feedback;
            if (_wet != chain.Wet)
                Echo.wetMix = _wet = chain.Wet;
            if (_dry != chain.Dry)
                Echo.dryMix = _dry = chain.Dry;
        }

        private void ApplyReverb(EffectChain chain)
        {
            if (!Toggle(Reverb, chain.Reverb, ref _reverb))
                return;
            var room = Millibels(chain.ReverbMix);
            var dry = chain.ReverbInsert ? Millibels(1f - chain.ReverbMix) : 0f;
            if (_room != room)
                Reverb.room = _room = room;
            if (_dryLevel != dry)
                Reverb.dryLevel = _dryLevel = dry;
            if (_decay != chain.Decay)
                Reverb.decayTime = _decay = chain.Decay;
            if (_damping != chain.Damping)
                Reverb.decayHFRatio = _damping = chain.Damping;
            if (_preDelay != chain.PreDelay)
                Reverb.reverbDelay = _preDelay = chain.PreDelay;
        }

        #endregion
    }
}

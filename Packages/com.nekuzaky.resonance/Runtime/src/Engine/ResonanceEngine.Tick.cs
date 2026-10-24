using System.Diagnostics;
using UnityEngine;

namespace Nekuzaky.Resonance
{
    public sealed partial class ResonanceEngine
    {
        #region Main API

        public void Tick(float deltaTime)
        {
            var started = Stopwatch.GetTimestamp();
            deltaTime = Mathf.Max(0f, deltaTime);
            _time += deltaTime;

            Syncs.Tick(deltaTime);
            Mixer.BeginFrame();
            var listener = ListenerPosition();
            foreach (var instance in _instances)
                TickInstance(instance, deltaTime, listener);

            for (var i = _instances.Count - 1; i >= 0; i--)
            {
                if (_instances[i].State != InstanceState.Done)
                    continue;
                Release(_instances[i]);
                _instances.RemoveAt(i);
            }

            Music.Tick(AudioSettings.dspTime);
            Mixer.Tick(deltaTime);
            Measure(started);
        }

        #endregion


        #region Private and Protected

        private SoundInstance Begin(SoundEvent sound, GameObject emitter, Vector3 position)
        {
            var instance = new SoundInstance
            {
                Id = _nextId++,
                Event = sound,
                Emitter = emitter,
                HadEmitter = emitter != null,
                Position = emitter != null ? emitter.transform.position : position,
                State = InstanceState.Playing,
                Fade = sound.fadeIn > 0f ? 0f : 1f
            };
            Modulate(instance);
            instance.Distance = Vector3.Distance(instance.Position, ListenerPosition());
            var busGain = Mixer.Gain(sound.bus);

            foreach (var layer in _plan.Layers)
            {
                var voice = Take();
                voice.Owner = instance;
                voice.Layer = layer;
                Configure(voice, sound, instance.Position);
                instance.Voices.Add(voice);
                voice.BlendWeight = BlendOf(voice);
                voice.Virtual = Simulate || IsFar(instance);
                StartClip(voice, 0f, busGain);
            }
            _plan.Layers.Clear();
            _instances.Add(instance);
            return instance;
        }

        private Voice Take()
        {
            if (_free.Count > 0)
                return _free.Pop();
            var voice = new Voice(_host, _voices.Count);
            _voices.Add(voice);
            return voice;
        }

        private void Configure(Voice voice, SoundEvent sound, Vector3 position)
        {
            var source = voice.Source;
            var spatial = sound.spatial;
            voice.Transform.position = position;
            source.outputAudioMixerGroup = sound.bus != null ? sound.bus.ResolveOutput() : null;
            source.spatialBlend = sound.spatialize && !Preview ? spatial.blend : 0f;
            source.minDistance = spatial.minDistance;
            source.maxDistance = Mathf.Max(spatial.minDistance + 0.01f, spatial.maxDistance);
            source.rolloffMode = spatial.rolloff == AudioRolloffMode.Custom ? AudioRolloffMode.Logarithmic : spatial.rolloff;
            source.spread = spatial.spread;
            source.dopplerLevel = spatial.doppler;
            source.priority = 255 - Mathf.Clamp(sound.priority, 0, 255);
        }

        private void StartClip(Voice voice, float time, float busGain)
        {
            if (!voice.HasClip)
            {
                voice.Finished = true;
                return;
            }

            var clip = voice.Clip;
            voice.Source.clip = clip.Clip;
            voice.Source.loop = clip.Loop;
            voice.Source.time = Mathf.Clamp(time, 0f, Mathf.Max(0f, clip.Clip.length - 0.01f));
            voice.VirtualTime = time;
            Mix(voice, busGain);
            if (!voice.Virtual)
                voice.Source.Play();
        }

        private void TickInstance(SoundInstance instance, float deltaTime, Vector3 listener)
        {
            var sound = instance.Event;
            instance.Age += deltaTime;
            if (instance.Emitter != null)
            {
                if (sound.followEmitter)
                    instance.Position = instance.Emitter.transform.position;
            }
            else if (instance.HadEmitter && sound.stopWhenEmitterDestroyed)
            {
                BeginStop(instance, sound.fadeOut);
            }

            Fade(instance, deltaTime);
            Modulate(instance);
            instance.Distance = Vector3.Distance(instance.Position, listener);

            var busGain = Mixer.Gain(sound.bus);
            var finished = true;
            foreach (var voice in instance.Voices)
            {
                TickVoice(voice, deltaTime, busGain);
                finished &= voice.Finished;
            }
            if (finished || (instance.State == InstanceState.Stopping && instance.Fade <= 0f))
                instance.State = InstanceState.Done;
        }

        private static void Fade(SoundInstance instance, float deltaTime)
        {
            if (instance.State == InstanceState.Playing)
                instance.Fade = AudioMath.Approach(instance.Fade, 1f, instance.Event.fadeIn, 1f, deltaTime);
            else if (instance.State == InstanceState.Stopping)
                instance.Fade = AudioMath.Approach(instance.Fade, 0f, instance.FadeOutTime, 1f, deltaTime);
        }

        private void Modulate(SoundInstance instance)
        {
            var sound = instance.Event;
            var decibels = sound.volume;
            var semitones = sound.pitch;
            var lowPass = AudioMath.OpenLowPass;
            var highPass = AudioMath.OpenHighPass;
            foreach (var modulation in sound.modulations)
            {
                if (modulation?.parameter == null || modulation.curve == null)
                    continue;
                var x = modulation.parameter.Normalize(Syncs.GetParameter(modulation.parameter, instance.Emitter));
                var y = modulation.curve.Evaluate(x);
                switch (modulation.target)
                {
                    case ModulationTarget.Volume: decibels += y; break;
                    case ModulationTarget.Pitch: semitones += y; break;
                    case ModulationTarget.LowPass: lowPass = Mathf.Min(lowPass, Mathf.Max(10f, y)); break;
                    case ModulationTarget.HighPass: highPass = Mathf.Max(highPass, Mathf.Min(22000f, y)); break;
                }
            }
            instance.Gain = AudioMath.ToLinear(decibels);
            instance.Pitch = AudioMath.ToPitch(semitones);
            instance.LowPass = lowPass;
            instance.HighPass = highPass;
        }

        private void Measure(long started)
        {
            var milliseconds = (Stopwatch.GetTimestamp() - started) * 1000.0 / Stopwatch.Frequency;
            Stats.TickMilliseconds = milliseconds;
            Stats.PeakTickMilliseconds = Mathf.Max((float)(Stats.PeakTickMilliseconds * 0.995), (float)milliseconds);
            Stats.Instances = _instances.Count;
            var real = 0;
            var virtualVoices = 0;
            foreach (var instance in _instances)
            {
                foreach (var voice in instance.Voices)
                {
                    if (voice.Finished)
                        continue;
                    if (voice.Virtual)
                        virtualVoices++;
                    else
                        real++;
                }
            }
            Stats.RealVoices = real;
            Stats.VirtualVoices = virtualVoices;
            Stats.PooledVoices = _voices.Count;
        }

        private Vector3 ListenerPosition()
        {
            if (Listener != null)
                return Listener.position;
            var registered = SoundListener.Current;
            if (registered != null)
                return registered.transform.position;
            if (_audioListener == null && _time >= _nextListenerSearch)
            {
                _audioListener = Object.FindAnyObjectByType<AudioListener>();
                _nextListenerSearch = _time + 1f;
            }
            return _audioListener != null ? _audioListener.transform.position : Vector3.zero;
        }

        #endregion
    }
}

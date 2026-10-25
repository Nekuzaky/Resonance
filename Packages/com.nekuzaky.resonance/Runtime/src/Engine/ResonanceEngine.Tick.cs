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
            var listener = ListenerTransform();
            foreach (var instance in _instances)
                TickInstance(instance, deltaTime, listener);

            for (var i = _instances.Count - 1; i >= 0; i--)
            {
                if (_instances[i].State != InstanceState.Done)
                    continue;
                QueueEnded(_instances[i]);
                Release(_instances[i]);
                _instances.RemoveAt(i);
            }
            Memory.Tick();
            TickRinging(deltaTime);

            if (!Preview)
                Reverb.Tick(listener, ReverbZone.Active);
            Music.Metering = Metering;
            Music.Tick(AudioSettings.dspTime);
            Mixer.Tick(deltaTime);
            Measure(started);
            Dispatch();
        }

        #endregion


        #region Private and Protected

        private SoundInstance Begin(SoundEvent sound, GameObject emitter, Vector3 position)
        {
            var instance = _instancePool.Count > 0 ? _instancePool.Pop() : new SoundInstance();
            instance.Reset();
            instance.Id = _nextId++;
            instance.Event = sound;
            instance.Emitter = emitter;
            instance.HadEmitter = emitter != null;
            instance.Position = emitter != null ? emitter.transform.position : position;
            instance.Fade = sound.fadeIn > 0f ? 0f : 1f;
            instance.Delay = sound.delay + (float)_random.NextDouble() * sound.delayRandom;
            var listener = ListenerTransform();
            instance.Distance = listener != null ? Vector3.Distance(instance.Position, listener.position) : 0f;
            UpdateOcclusion(instance, 0f, listener, true);
            Modulate(instance);
            var busGain = Mixer.Gain(sound.bus);

            foreach (var layer in _plan.Layers)
            {
                var voice = Take();
                voice.Owner = instance;
                voice.Layer = layer;
                if (layer.Clips.Count > 0)
                    Memory.NoteOnDemand(layer.Clips[0].Clip);
                Configure(voice, sound, instance.Position);
                instance.Voices.Add(voice);
                voice.BlendWeight = BlendOf(voice);
                voice.Virtual = Simulate || IsFar(instance);
                if (instance.Delay <= 0f)
                    StartFirst(voice, busGain);
            }
            _plan.Layers.Clear();
            _instances.Add(instance);
            return instance;
        }

        private Voice Take()
        {
            if (_free.Count > 0)
                return _free.Pop();
            var ringing = TakeRinging();
            if (ringing != null)
                return ringing;
            var voice = new Voice(_host, _voices.Count);
            _voices.Add(voice);
            return voice;
        }

        private void Configure(Voice voice, SoundEvent sound, Vector3 position)
        {
            var source = voice.Source;
            var spatial = sound.Spatial;
            voice.Place(position);
            source.outputAudioMixerGroup = sound.bus != null ? sound.bus.ResolveOutput() : null;
            source.spatialBlend = sound.spatialize && !Preview ? spatial.blend : 0f;
            source.minDistance = spatial.minDistance;
            source.maxDistance = Mathf.Max(spatial.minDistance + 0.01f, spatial.maxDistance);
            var custom = spatial.rolloff == AudioRolloffMode.Custom && spatial.customCurve != null;
            source.rolloffMode = custom ? AudioRolloffMode.Custom : spatial.rolloff == AudioRolloffMode.Custom ? AudioRolloffMode.Logarithmic : spatial.rolloff;
            if (custom)
                source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, spatial.customCurve);
            source.reverbZoneMix = Preview ? 0f : sound.reverbSend;
            source.spread = spatial.spread;
            source.spatialize = spatial.binaural && sound.spatialize && !Preview && _hasSpatializer;
            source.dopplerLevel = spatial.doppler;
            source.priority = 255 - Mathf.Clamp(sound.priority, 0, 255);
        }

        private void StartFirst(Voice voice, float busGain)
        {
            if (!voice.HasClip)
            {
                voice.Finished = true;
                return;
            }
            var clip = voice.Clip;
            var offset = clip.RandomStart ? (float)_random.NextDouble() * clip.Clip.length : 0f;
            StartClip(voice, offset, busGain);
        }

        private void StartClip(Voice voice, float time, float busGain)
        {
            if (!voice.HasClip)
            {
                voice.Finished = true;
                return;
            }
            var clip = voice.Clip;
            if (!ReferenceEquals(voice.ShownLine, clip.Line))
            {
                EndSubtitle(voice);
                BeginSubtitle(voice, clip);
            }
            voice.Source.clip = clip.Clip;
            voice.Source.loop = clip.Loop;
            voice.Source.time = Mathf.Clamp(time, 0f, Mathf.Max(0f, clip.Clip.length - 0.01f));
            voice.VirtualTime = time;
            voice.LastTime = time - 0.0001f;
            Mix(voice, busGain);
            if (!voice.Virtual)
                voice.Source.Play();
        }

        private void TickInstance(SoundInstance instance, float deltaTime, Transform listener)
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
            instance.Distance = listener != null ? Vector3.Distance(instance.Position, listener.position) : 0f;
            UpdateOcclusion(instance, deltaTime, listener, false);
            Modulate(instance);

            var busGain = Mixer.Gain(sound.bus);
            if (instance.Delay > 0f && !TickDelay(instance, deltaTime, busGain))
                return;
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
            if (instance.Delay > 0f)
                return;
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
            ApplySpace(instance, ref decibels, ref lowPass);
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
            Stats.OnDemandLoads = Memory.OnDemandLoads;
        }

        internal Transform ListenerTransform()
        {
            if (Listener != null)
                return Listener;
            var registered = SoundListener.Current;
            if (registered != null)
                return registered.transform;
            if (_audioListener == null && _time >= _nextListenerSearch)
            {
                _audioListener = Object.FindAnyObjectByType<AudioListener>();
                _nextListenerSearch = _time + 1f;
            }
            return _audioListener != null ? _audioListener.transform : null;
        }

        #endregion
    }
}

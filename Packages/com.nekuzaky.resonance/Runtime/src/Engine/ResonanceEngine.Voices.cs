using UnityEngine;

namespace Nekuzaky.Resonance
{
    public sealed partial class ResonanceEngine
    {
        #region Private and Protected

        private void TickVoice(Voice voice, float deltaTime, float busGain)
        {
            if (voice.Finished)
                return;

            var instance = voice.Owner;
            voice.Transform.position = instance.Position;
            voice.BlendWeight = BlendOf(voice);

            var far = Simulate || IsFar(instance);
            if (far && !voice.Virtual)
                Virtualize(voice);
            else if (!far && voice.Virtual)
                Realize(voice, busGain);

            if (voice.Virtual)
                AdvanceVirtual(voice, deltaTime, busGain);
            else if (!voice.Source.isPlaying && !AudioListener.pause)
                AdvanceClip(voice, busGain);

            if (voice.Finished)
            {
                EndSubtitle(voice);
                return;
            }
            Mix(voice, busGain);
            Mixer.CountVoice(instance.Event.bus, voice.Virtual ? 0f : voice.Level);
        }

        private bool IsFar(SoundInstance instance)
        {
            var sound = instance.Event;
            var spatial = sound.Spatial;
            return !Preview && sound.spatialize && sound.virtualizeWhenFar && spatial.blend > 0f &&
                   instance.Distance > spatial.maxDistance * 1.05f;
        }

        private static void Virtualize(Voice voice)
        {
            voice.VirtualTime = voice.Source.time;
            voice.Source.Stop();
            voice.Virtual = true;
        }

        private void Realize(Voice voice, float busGain)
        {
            voice.Virtual = false;
            StartClip(voice, voice.VirtualTime, busGain);
        }

        private void AdvanceVirtual(Voice voice, float deltaTime, float busGain)
        {
            var clip = voice.Clip;
            var length = clip.Clip.length;
            voice.VirtualTime += deltaTime * clip.Pitch * voice.Owner.Pitch;
            if (voice.VirtualTime < length || length <= 0f)
                return;
            if (clip.Loop)
            {
                voice.VirtualTime %= length;
                return;
            }
            if (NextClip(voice))
                StartClip(voice, 0f, busGain);
        }

        private void AdvanceClip(Voice voice, float busGain)
        {
            if (NextClip(voice))
                StartClip(voice, 0f, busGain);
        }

        private static bool NextClip(Voice voice)
        {
            voice.ClipIndex++;
            if (voice.ClipIndex >= voice.Layer.Clips.Count)
            {
                if (!voice.Layer.Loop || voice.Owner.State != InstanceState.Playing)
                {
                    voice.Finished = true;
                    return false;
                }
                voice.ClipIndex = 0;
            }
            return true;
        }

        private void BeginSubtitle(Voice voice)
        {
            var layer = voice.Layer;
            if (layer?.Line == null || voice.SubtitleShown)
                return;
            voice.SubtitleShown = true;
            SubtitleStarted?.Invoke(SubtitleOf(voice));
        }

        private void EndSubtitle(Voice voice)
        {
            if (!voice.SubtitleShown || voice.Layer?.Line == null)
                return;
            voice.SubtitleShown = false;
            SubtitleEnded?.Invoke(SubtitleOf(voice));
        }

        private Subtitle SubtitleOf(Voice voice)
        {
            var layer = voice.Layer;
            var owner = voice.Owner;
            var length = layer.Line.clip != null ? layer.Line.clip.length : 0f;
            return new Subtitle(new SoundHandle(owner.Id), layer.Line.subtitle, layer.Dialogue.speaker, layer.Line.language, owner.Emitter, length);
        }

        private float BlendOf(Voice voice)
        {
            var layer = voice.Layer;
            if (layer == null || layer.BlendParameter == null)
                return 1f;
            var value = Syncs.GetParameter(layer.BlendParameter, voice.Owner.Emitter);
            return AudioMath.BlendWeight(value, layer.BlendRange, layer.BlendFade);
        }

        private void Mix(Voice voice, float busGain)
        {
            var instance = voice.Owner;
            var clip = voice.Clip;
            var volume = clip.Gain * instance.Gain * instance.Fade * voice.BlendWeight * busGain;
            var source = voice.Source;
            source.volume = Mathf.Clamp01(volume);
            source.pitch = Mathf.Clamp(clip.Pitch * instance.Pitch, 0.01f, 4f);

            var lowPass = instance.LowPass < AudioMath.OpenLowPass - 1f;
            voice.LowPass.enabled = lowPass;
            if (lowPass)
                voice.LowPass.cutoffFrequency = instance.LowPass;
            var highPass = instance.HighPass > AudioMath.OpenHighPass + 1f;
            voice.HighPass.enabled = highPass;
            if (highPass)
                voice.HighPass.cutoffFrequency = instance.HighPass;

            voice.Level = Metering && !voice.Virtual ? MeteredLevel(voice, source.volume) : 0f;
        }

        private float MeteredLevel(Voice voice, float volume)
        {
            var instance = voice.Owner;
            var sound = instance.Event;
            var level = ClipEnvelopes.At(voice.Source.clip, voice.Source.timeSamples) * volume;
            if (!sound.spatialize || Preview)
                return level;
            var attenuation = sound.Spatial.Attenuation(instance.Distance);
            return level * Mathf.Lerp(1f, attenuation, voice.Source.spatialBlend);
        }

        private void Release(SoundInstance instance)
        {
            foreach (var voice in instance.Voices)
            {
                EndSubtitle(voice);
                if (voice.Layer != null)
                    _layerPool.Push(voice.Layer);
                voice.Reset();
                _free.Push(voice);
            }
            instance.Voices.Clear();
            instance.State = InstanceState.Done;
            instance.Emitter = null;
            _instancePool.Push(instance);
        }

        #endregion
    }
}

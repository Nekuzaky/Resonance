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
            voice.Place(instance.Position);
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
            CheckMarkers(voice);
            Mix(voice, busGain);
            if (voice.Virtual)
                return;
            var bus = instance.Event.bus;
            Mixer.CountVoice(bus, voice.Level);
            if (Mixer.Chain(bus).Compressed)
                Mixer.Detect(bus, LevelOf(voice, DryVolume(voice), true));
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
            voice.VirtualTime += deltaTime * PitchOf(clip, voice.Owner);
            if (voice.VirtualTime < length || length <= 0f)
                return;
            if (clip.Loop)
            {
                voice.VirtualTime %= length;
                return;
            }
            var overshoot = voice.VirtualTime - length;
            if (NextClip(voice))
                StartClip(voice, Mathf.Min(overshoot, voice.Clip.Clip.length), busGain);
        }

        private static float PitchOf(PlanClip clip, SoundInstance instance) => Mathf.Clamp(clip.Pitch * instance.Pitch, 0.01f, 4f);

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

        private void BeginSubtitle(Voice voice, PlanClip clip)
        {
            if (clip.Line == null || voice.Owner == null)
                return;
            voice.ShownLine = clip.Line;
            voice.ShownDialogue = clip.Dialogue;
            QueueSubtitle(PendingKind.SubtitleStarted, SubtitleOf(voice));
        }

        private void EndSubtitle(Voice voice)
        {
            if (voice.ShownLine == null || voice.Owner == null)
                return;
            QueueSubtitle(PendingKind.SubtitleEnded, SubtitleOf(voice));
            voice.ShownLine = null;
            voice.ShownDialogue = null;
        }

        private static Subtitle SubtitleOf(Voice voice)
        {
            var line = voice.ShownLine;
            var owner = voice.Owner;
            var length = line.clip != null ? line.clip.length : 0f;
            var speaker = voice.ShownDialogue != null ? voice.ShownDialogue.speaker : string.Empty;
            return new Subtitle(new SoundHandle(owner.Id), line.subtitle, speaker, line.language, owner.Emitter, length);
        }

        private void CheckMarkers(Voice voice)
        {
            var markers = voice.Clip.Markers;
            if (markers == null || MarkerReached == null)
                return;
            var now = voice.Virtual ? voice.VirtualTime : voice.Source.time;
            var wrapped = now < voice.LastTime;
            foreach (var marker in markers)
            {
                var passed = wrapped ? marker.time > voice.LastTime || marker.time <= now : marker.time > voice.LastTime && marker.time <= now;
                if (passed)
                    QueueMarker(new SoundMarker(new SoundHandle(voice.Owner.Id), marker.name, marker.time, voice.Owner.Emitter));
            }
            voice.LastTime = now;
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
            var wanted = DryVolume(voice) * busGain;
            var volume = Mathf.Clamp01(wanted);
            voice.Apply(volume, PitchOf(clip, instance), instance.LowPass, instance.HighPass);
            voice.Effects.Apply(Mixer.Chain(instance.Event.bus), wanted > 1f ? volume / wanted : 1f);
            voice.Level = voice.Virtual ? 0f : LevelOf(voice, volume, Metering);
        }

        private static float DryVolume(Voice voice)
        {
            var instance = voice.Owner;
            return voice.Clip.Gain * instance.Gain * instance.Fade * voice.BlendWeight;
        }

        private float LevelOf(Voice voice, float volume, bool envelope)
        {
            var instance = voice.Owner;
            var sound = instance.Event;
            var level = envelope ? ClipEnvelopes.At(voice.Source.clip, voice.Source.timeSamples) * volume : volume;
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
                if (!Ring(voice))
                {
                    if (voice.Effects.Tail > 0f)
                        voice.Effects.Clear();
                    voice.Reset();
                    _free.Push(voice);
                }
            }
            instance.Voices.Clear();
            instance.State = InstanceState.Done;
            instance.Emitter = null;
            _instancePool.Push(instance);
        }

        #endregion
    }
}

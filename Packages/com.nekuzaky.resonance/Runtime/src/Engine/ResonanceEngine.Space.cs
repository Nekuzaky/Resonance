using UnityEngine;

namespace Nekuzaky.Resonance
{
    public sealed partial class ResonanceEngine
    {
        #region Private and Protected

        private bool TickDelay(SoundInstance instance, float deltaTime, float busGain)
        {
            if (instance.State != InstanceState.Playing)
            {
                instance.State = InstanceState.Done;
                return false;
            }
            instance.Delay -= deltaTime;
            if (instance.Delay > 0f)
                return false;
            foreach (var voice in instance.Voices)
                StartFirst(voice, busGain);
            return true;
        }

        private void ApplySpace(SoundInstance instance, ref float decibels, ref float lowPass)
        {
            var sound = instance.Event;
            if (!sound.spatialize || Preview)
                return;
            lowPass = Mathf.Min(lowPass, sound.Spatial.DistanceLowPass(instance.Distance));
            if (instance.Occlusion <= 0f)
                return;
            var occlusion = sound.occlusion;
            decibels += occlusion.volume * instance.Occlusion;
            lowPass = Mathf.Min(lowPass, AudioMath.LogLerp(AudioMath.OpenLowPass, occlusion.lowPass, instance.Occlusion));
        }

        private void UpdateOcclusion(SoundInstance instance, float deltaTime, Transform listener, bool snap)
        {
            var settings = instance.Event.occlusion;
            if (settings == null || !settings.enabled || !instance.Event.spatialize || Preview || listener == null)
            {
                instance.Occlusion = 0f;
                return;
            }
            if (!snap && IsFar(instance))
                return;
            if (snap || _time >= instance.NextOcclusionCheck)
            {
                instance.NextOcclusionCheck = _time + settings.interval;
                instance.OcclusionTarget = Occluded(instance, settings, listener);
            }
            instance.Occlusion = snap
                ? instance.OcclusionTarget
                : AudioMath.Approach(instance.Occlusion, instance.OcclusionTarget, settings.smoothing, 1f, deltaTime);
        }

        private float Occluded(SoundInstance instance, Occlusion settings, Transform listener)
        {
            var target = listener.position;
            var direction = target - instance.Position;
            var distance = direction.magnitude;
            if (distance < 0.01f)
                return 0f;
            var side = Vector3.Cross(direction / distance, Vector3.up);
            side = side.sqrMagnitude < 0.0001f ? Vector3.right : side.normalized;
            side *= settings.width * 0.5f;
            var blocked = 0;
            for (var ray = -1; ray <= 1; ray++)
            {
                if (Blocked(instance.Position + side * ray, target + side * ray, instance.Emitter, listener, settings.layers))
                    blocked++;
            }
            return blocked / 3f;
        }

        private bool Blocked(Vector3 from, Vector3 to, GameObject emitter, Transform listener, LayerMask layers)
        {
            var direction = to - from;
            var distance = direction.magnitude;
            if (distance < 0.01f)
                return false;
            var count = Physics.RaycastNonAlloc(from, direction / distance, _hits, distance, layers, QueryTriggerInteraction.Ignore);
            for (var i = 0; i < count; i++)
            {
                var hit = _hits[i].transform;
                if ((emitter == null || !hit.IsChildOf(emitter.transform)) && !hit.IsChildOf(listener.root))
                    return true;
            }
            return false;
        }

        #endregion
    }
}

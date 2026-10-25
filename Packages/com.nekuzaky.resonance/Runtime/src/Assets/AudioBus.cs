using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Nekuzaky.Resonance
{
    [CreateAssetMenu(menuName = "Resonance/Bus", fileName = "New Bus", order = 40)]
    public sealed class AudioBus : ScriptableObject
    {
        public const int MaxDepth = 16;

        public AudioBus parent;
        [Range(-80f, 12f)] public float volume;
        public bool mute;
        public AudioMixerGroup output;
        public List<DuckRule> ducking = new();
        public List<BusEffect> effects = new();

        public AudioMixerGroup ResolveOutput()
        {
            var bus = this;
            for (var depth = 0; depth < MaxDepth && bus != null; depth++)
            {
                if (bus.output != null)
                    return bus.output;
                bus = bus.parent;
            }
            return null;
        }

        public BusEffect AddEffect(BusEffectType type)
        {
            var effect = new BusEffect(type) { id = NextEffectId() };
            effects.Add(effect);
            return effect;
        }

        public BusEffect FindEffect(int id)
        {
            for (var i = 0; i < effects.Count; i++)
            {
                if (effects[i] != null && effects[i].id == id)
                    return effects[i];
            }
            return null;
        }

        public int IndexOfEffect(int id)
        {
            for (var i = 0; i < effects.Count && i < BusEffects.MaxEffects; i++)
            {
                if (effects[i] != null && effects[i].id == id)
                    return i;
            }
            return -1;
        }

        public bool AssignEffectIds()
        {
            var changed = false;
            for (var i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                if (effect == null || (effect.id != 0 && IndexOfEffect(effect.id) == i))
                    continue;
                effect.id = NextEffectId();
                changed = true;
            }
            return changed;
        }

        public bool IsDescendantOf(AudioBus ancestor)
        {
            var bus = this;
            for (var depth = 0; depth < MaxDepth && bus != null; depth++)
            {
                if (bus == ancestor)
                    return true;
                bus = bus.parent;
            }
            return false;
        }

        private int NextEffectId()
        {
            var next = 1;
            foreach (var effect in effects)
            {
                if (effect != null && effect.id >= next)
                    next = effect.id + 1;
            }
            return next;
        }

        private void OnValidate() => AssignEffectIds();
    }
}

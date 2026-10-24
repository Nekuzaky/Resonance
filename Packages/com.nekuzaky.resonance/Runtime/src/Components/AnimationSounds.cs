using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekuzaky.Resonance
{
    [AddComponentMenu("Resonance/Animation Sounds")]
    [HelpURL("https://github.com/Nekuzaky/Resonance/blob/main/Packages/com.nekuzaky.resonance/Documentation~/components.md")]
    public sealed class AnimationSounds : MonoBehaviour
    {
        public List<NamedSound> sounds = new();
        public Transform emitter;

        public void PlaySound(UnityEngine.Object sound)
        {
            if (sound is SoundEvent soundEvent)
                Resonance.Play(soundEvent, Target);
        }

        public void PlaySoundNamed(string soundName)
        {
            foreach (var entry in sounds)
            {
                if (entry != null && entry.name == soundName)
                {
                    Resonance.Play(entry.sound, Target);
                    return;
                }
            }
        }

        public void StopAll() => Resonance.StopAll(Target);

        private GameObject Target => emitter != null ? emitter.gameObject : gameObject;
    }
}

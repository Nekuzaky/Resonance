using UnityEngine;

namespace Nekuzaky.Resonance
{
    public readonly struct SoundStartInfo
    {
        public readonly SoundHandle Handle;
        public readonly SoundEvent Event;
        public readonly GameObject Emitter;
        public readonly Vector3 Position;
        public readonly float Volume;
        public readonly float MaxDistance;

        public SoundStartInfo(SoundHandle handle, SoundEvent sound, GameObject emitter, Vector3 position, float volume, float maxDistance)
        {
            Handle = handle;
            Event = sound;
            Emitter = emitter;
            Position = position;
            Volume = volume;
            MaxDistance = maxDistance;
        }
    }
}

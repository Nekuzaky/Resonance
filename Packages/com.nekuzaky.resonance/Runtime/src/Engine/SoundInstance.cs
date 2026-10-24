using System.Collections.Generic;
using UnityEngine;

namespace Nekuzaky.Resonance
{
    internal enum InstanceState
    {
        Playing,
        Stopping,
        Done
    }

    internal sealed class SoundInstance
    {
        public int Id;
        public SoundEvent Event;
        public GameObject Emitter;
        public bool HadEmitter;
        public Vector3 Position;
        public InstanceState State;
        public float Fade;
        public float FadeOutTime;
        public float Age;
        public float Gain = 1f;
        public float Pitch = 1f;
        public float LowPass = AudioMath.OpenLowPass;
        public float HighPass = AudioMath.OpenHighPass;
        public float Distance;
        public readonly List<Voice> Voices = new();

        public bool IsVirtual
        {
            get
            {
                foreach (var voice in Voices)
                {
                    if (!voice.Virtual)
                        return false;
                }
                return Voices.Count > 0;
            }
        }

        public float Level
        {
            get
            {
                var level = 0f;
                foreach (var voice in Voices)
                    level = Mathf.Max(level, voice.Level);
                return level;
            }
        }

        public string EmitterName => Emitter != null ? Emitter.name : HadEmitter ? "(destroyed)" : "(world)";
    }

    internal sealed class Voice
    {
        #region Public

        public readonly AudioSource Source;
        public readonly AudioLowPassFilter LowPass;
        public readonly AudioHighPassFilter HighPass;
        public readonly Transform Transform;
        public SoundInstance Owner;
        public PlanLayer Layer;
        public int ClipIndex;
        public bool Virtual;
        public float VirtualTime;
        public bool Finished;
        public float Level;
        public float BlendWeight = 1f;

        public Voice(Transform parent, int index)
        {
            var host = new GameObject($"Voice {index}");
            host.hideFlags = parent.gameObject.hideFlags;
            Transform = host.transform;
            Transform.SetParent(parent, false);
            Source = host.AddComponent<AudioSource>();
            Source.playOnAwake = false;
            LowPass = host.AddComponent<AudioLowPassFilter>();
            HighPass = host.AddComponent<AudioHighPassFilter>();
            LowPass.enabled = false;
            HighPass.enabled = false;
        }

        public bool HasClip => Layer != null && ClipIndex >= 0 && ClipIndex < Layer.Clips.Count;

        public PlanClip Clip => Layer.Clips[ClipIndex];

        public void Reset()
        {
            if (Source != null)
            {
                Source.Stop();
                Source.clip = null;
            }
            Owner = null;
            Layer = null;
            ClipIndex = 0;
            Virtual = false;
            VirtualTime = 0f;
            Finished = false;
            Level = 0f;
            BlendWeight = 1f;
        }

        #endregion
    }
}

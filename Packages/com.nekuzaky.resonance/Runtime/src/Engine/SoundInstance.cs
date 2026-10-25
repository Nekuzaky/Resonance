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
        public float Delay;
        public float Occlusion;
        public float OcclusionTarget;
        public float NextOcclusionCheck;
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

        public void Reset()
        {
            Id = 0;
            Event = null;
            Emitter = null;
            HadEmitter = false;
            Position = Vector3.zero;
            State = InstanceState.Playing;
            Fade = 1f;
            FadeOutTime = 0f;
            Age = 0f;
            Gain = 1f;
            Pitch = 1f;
            LowPass = AudioMath.OpenLowPass;
            HighPass = AudioMath.OpenHighPass;
            Distance = 0f;
            Delay = 0f;
            Occlusion = 0f;
            OcclusionTarget = 0f;
            NextOcclusionCheck = 0f;
            Voices.Clear();
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
        public DialogueLine ShownLine;
        public DialogueNode ShownDialogue;
        public float LastTime = -1f;
        public Vector3 Placed;
        public bool IsPlaced;
        public float Volume = -1f;
        public float Pitch = -1f;
        public bool LowPassOn;
        public float LowPassCutoff = -1f;
        public bool HighPassOn;
        public float HighPassCutoff = -1f;

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

        public void Place(Vector3 position)
        {
            if (IsPlaced && Placed.Equals(position))
                return;
            Transform.position = position;
            Placed = position;
            IsPlaced = true;
        }

        public void Apply(float volume, float pitch, float lowPass, float highPass)
        {
            if (!Volume.Equals(volume))
            {
                Source.volume = volume;
                Volume = volume;
            }
            if (!Pitch.Equals(pitch))
            {
                Source.pitch = pitch;
                Pitch = pitch;
            }
            var lowPassOn = lowPass < AudioMath.OpenLowPass - 1f;
            if (lowPassOn != LowPassOn)
            {
                LowPass.enabled = lowPassOn;
                LowPassOn = lowPassOn;
            }
            if (lowPassOn && !LowPassCutoff.Equals(lowPass))
            {
                LowPass.cutoffFrequency = lowPass;
                LowPassCutoff = lowPass;
            }
            var highPassOn = highPass > AudioMath.OpenHighPass + 1f;
            if (highPassOn != HighPassOn)
            {
                HighPass.enabled = highPassOn;
                HighPassOn = highPassOn;
            }
            if (highPassOn && !HighPassCutoff.Equals(highPass))
            {
                HighPass.cutoffFrequency = highPass;
                HighPassCutoff = highPass;
            }
        }

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
            ShownLine = null;
            ShownDialogue = null;
            LastTime = -1f;
            IsPlaced = false;
        }

        #endregion
    }
}

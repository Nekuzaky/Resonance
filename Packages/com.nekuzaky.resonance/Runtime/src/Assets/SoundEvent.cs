using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekuzaky.Resonance
{
    public enum LimitBehavior
    {
        StealOldest,
        StealQuietest,
        RejectNew
    }

    public enum LimitScope
    {
        Global,
        PerEmitter
    }

    public enum ModulationTarget
    {
        Volume,
        Pitch,
        LowPass,
        HighPass
    }

    [Serializable]
    public sealed class Modulation
    {
        public SoundParameter parameter;
        public ModulationTarget target = ModulationTarget.Volume;
        public AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 0f);
    }

    [Serializable]
    public sealed class Spatial
    {
        [Range(0f, 1f)] public float blend = 1f;
        [Min(0f)] public float minDistance = 1f;
        [Min(0.01f)] public float maxDistance = 30f;
        public AudioRolloffMode rolloff = AudioRolloffMode.Logarithmic;
        [Range(0f, 360f)] public float spread;
        [Range(0f, 5f)] public float doppler = 1f;
        public bool binaural;
        public AnimationCurve customCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        [Range(10f, 22000f)] public float lowPassAtMax = AudioMath.OpenLowPass;

        public float Attenuation(float distance)
        {
            if (rolloff != AudioRolloffMode.Custom)
                return AudioMath.Attenuation(rolloff, minDistance, maxDistance, distance);
            return customCurve == null ? 1f : Mathf.Clamp01(customCurve.Evaluate(distance / Mathf.Max(0.01f, maxDistance)));
        }

        public float DistanceLowPass(float distance)
        {
            if (lowPassAtMax >= AudioMath.OpenLowPass - 1f)
                return AudioMath.OpenLowPass;
            var t = Mathf.InverseLerp(minDistance, Mathf.Max(minDistance + 0.01f, maxDistance), distance);
            return AudioMath.LogLerp(AudioMath.OpenLowPass, lowPassAtMax, t);
        }
    }

    [Serializable]
    public sealed class Occlusion
    {
        public bool enabled;
        public LayerMask layers = ~0;
        [Range(-48f, 0f)] public float volume = -9f;
        [Range(10f, 22000f)] public float lowPass = 1500f;
        [Min(0f)] public float smoothing = 0.15f;
        [Range(0.02f, 1f)] public float interval = 0.1f;
        [Range(0f, 4f)] public float width = 0.6f;
    }

    [CreateAssetMenu(menuName = "Resonance/Sound Event", fileName = "New Sound Event", order = 0)]
    public sealed class SoundEvent : ScriptableObject
    {
        #region Public

        [SerializeReference] public SoundNode root = SoundNodes.Create(NodeKind.Random);
        public AudioBus bus;
        [Range(-48f, 12f)] public float volume;
        [Range(-24f, 24f)] public float pitch;
        public bool spatialize = true;
        public Spatial spatial = new();
        public AttenuationPreset attenuation;
        public Occlusion occlusion = new();
        [Range(0f, 1.1f)] public float reverbSend = 1f;
        [Min(0f)] public float delay;
        [Min(0f)] public float delayRandom;
        [Min(0f)] public float fadeIn;
        [Min(0f)] public float fadeOut = 0.05f;
        [Min(0)] public int maxInstances;
        public LimitScope limitScope = LimitScope.Global;
        public LimitBehavior limitBehavior = LimitBehavior.StealOldest;
        [Range(0, 255)] public int priority = 128;
        [Min(0f)] public float cooldown;
        public bool virtualizeWhenFar = true;
        public bool followEmitter = true;
        public bool stopWhenEmitterDestroyed = true;
        public List<Modulation> modulations = new();
        public string caption = string.Empty;
        public bool indicator;

        public Spatial Spatial => attenuation != null && attenuation.spatial != null ? attenuation.spatial : spatial;

        public bool IsLooping
        {
            get
            {
                var visitor = new NodeWalker(root);
                for (var guard = 0; guard < NodeWalker.MaxNodes && visitor.Next(out var node); guard++)
                {
                    if (node is ClipNode { loop: true } || node is SequenceNode { mode: SequenceMode.Continuous, loop: true })
                        return true;
                }
                return false;
            }
        }

        public void ResetRuntimeState()
        {
            var visitor = new NodeWalker(root);
            for (var guard = 0; guard < NodeWalker.MaxNodes && visitor.Next(out var node); guard++)
            {
                if (node is RandomNode random)
                    random.History.Clear();
                else if (node is SequenceNode sequence)
                    sequence.Next = 0;
            }
        }

        #endregion


    }

    public struct NodeWalker
    {
        public const int MaxNodes = 1024;

        private readonly Stack<SoundNode> _pending;

        public NodeWalker(SoundNode root)
        {
            _pending = new Stack<SoundNode>();
            if (root != null)
                _pending.Push(root);
        }

        public bool Next(out SoundNode node)
        {
            if (_pending == null || _pending.Count == 0)
            {
                node = null;
                return false;
            }

            node = _pending.Pop();
            var children = node.Children;
            for (var i = children.Count - 1; i >= 0; i--)
            {
                if (children[i] != null)
                    _pending.Push(children[i]);
            }
            return true;
        }
    }
}

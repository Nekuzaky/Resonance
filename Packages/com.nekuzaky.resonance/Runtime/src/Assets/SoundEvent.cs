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

        [NonSerialized] internal float LastPlayTime = float.NegativeInfinity;

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
            LastPlayTime = float.NegativeInfinity;
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


        #region Unity Callbacks

        private void OnEnable() => LastPlayTime = float.NegativeInfinity;

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

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekuzaky.Resonance
{
    public enum NodeKind
    {
        Clip,
        Random,
        Sequence,
        Switch,
        Blend,
        Layer,
        Dialogue
    }

    public enum SequenceMode
    {
        Step,
        Continuous
    }

    [Serializable]
    public abstract class SoundNode
    {
        #region Public

        public string name = "Node";
        [Range(-48f, 12f)] public float volume;
        [Range(0f, 24f)] public float volumeRandom;
        [Range(-24f, 24f)] public float pitch;
        [Range(0f, 12f)] public float pitchRandom;
        [Min(0f)] public float weight = 1f;
        public string switchValue = string.Empty;
        public Vector2 blendRange = new(0f, 1f);
        [Min(0f)] public float blendFade;

        public abstract NodeKind Kind { get; }

        public virtual IReadOnlyList<SoundNode> Children => Array.Empty<SoundNode>();

        public virtual bool IsContainer => false;

        public SoundNode Clone()
        {
            var copy = Copy();
            var pending = new Stack<ContainerNode>();
            if (copy is ContainerNode container)
                pending.Push(container);
            for (var guard = 0; guard < NodeWalker.MaxNodes && pending.Count > 0; guard++)
            {
                var current = pending.Pop();
                var children = new List<SoundNode>(current.children.Count);
                foreach (var child in current.children)
                {
                    var childCopy = child?.Copy();
                    children.Add(childCopy);
                    if (childCopy is ContainerNode nested)
                        pending.Push(nested);
                }
                current.children = children;
            }
            return copy;
        }

        #endregion


        #region Private and Protected

        protected virtual SoundNode Copy() => (SoundNode)MemberwiseClone();

        #endregion
    }

    [Serializable]
    public sealed class ClipNode : SoundNode
    {
        public AudioClip clip;
        public bool loop;
        public bool randomStart;

        public override NodeKind Kind => NodeKind.Clip;
    }

    [Serializable]
    public abstract class ContainerNode : SoundNode
    {
        [SerializeReference] public List<SoundNode> children = new();

        public override IReadOnlyList<SoundNode> Children => children;

        public override bool IsContainer => true;
    }

    [Serializable]
    public sealed class RandomNode : ContainerNode
    {
        [Min(0)] public int avoidRepeat = 1;
        public bool shuffle;

        [NonSerialized] internal List<int> History = new();

        public override NodeKind Kind => NodeKind.Random;

        protected override SoundNode Copy()
        {
            var copy = (RandomNode)base.Copy();
            copy.History = new List<int>();
            return copy;
        }
    }

    [Serializable]
    public sealed class SequenceNode : ContainerNode
    {
        public SequenceMode mode = SequenceMode.Step;
        public bool loop;

        [NonSerialized] internal int Next;

        public override NodeKind Kind => NodeKind.Sequence;
    }

    [Serializable]
    public sealed class SwitchNode : ContainerNode
    {
        public SwitchGroup group;
        [Min(0)] public int fallback;

        public override NodeKind Kind => NodeKind.Switch;
    }

    [Serializable]
    public sealed class BlendNode : ContainerNode
    {
        public SoundParameter parameter;

        public override NodeKind Kind => NodeKind.Blend;
    }

    [Serializable]
    public sealed class LayerNode : ContainerNode
    {
        public override NodeKind Kind => NodeKind.Layer;
    }

    public static class SoundNodes
    {
        public static SoundNode Create(NodeKind kind)
        {
            SoundNode node = kind switch
            {
                NodeKind.Random => new RandomNode(),
                NodeKind.Sequence => new SequenceNode(),
                NodeKind.Switch => new SwitchNode(),
                NodeKind.Blend => new BlendNode(),
                NodeKind.Layer => new LayerNode(),
                NodeKind.Dialogue => new DialogueNode(),
                _ => new ClipNode()
            };
            node.name = kind.ToString();
            return node;
        }
    }
}

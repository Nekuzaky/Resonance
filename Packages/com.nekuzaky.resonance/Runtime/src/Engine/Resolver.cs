using System.Collections.Generic;
using UnityEngine;

namespace Nekuzaky.Resonance
{
    internal struct PlanClip
    {
        public AudioClip Clip;
        public float Gain;
        public float Pitch;
        public bool Loop;
    }

    internal sealed class PlanLayer
    {
        public readonly List<PlanClip> Clips = new();
        public bool Loop;
        public SoundParameter BlendParameter;
        public Vector2 BlendRange;
        public float BlendFade;

        public void Clear()
        {
            Clips.Clear();
            Loop = false;
            BlendParameter = null;
            BlendRange = Vector2.zero;
            BlendFade = 0f;
        }
    }

    internal sealed class Plan
    {
        public readonly List<PlanLayer> Layers = new();
        private readonly Stack<PlanLayer> _pool;

        public Plan(Stack<PlanLayer> pool) => _pool = pool;

        public PlanLayer AddLayer()
        {
            var layer = _pool.Count > 0 ? _pool.Pop() : new PlanLayer();
            layer.Clear();
            Layers.Add(layer);
            return layer;
        }

        public void Release()
        {
            foreach (var layer in Layers)
                _pool.Push(layer);
            Layers.Clear();
        }
    }

    internal interface ISyncReader
    {
        string GetSwitch(SwitchGroup group);
    }

    internal sealed class Resolver
    {
        #region Private and Protected

        private struct Frame
        {
            public SoundNode Node;
            public float Decibels;
            public float Semitones;
            public int Layer;
            public SoundParameter BlendParameter;
            public SoundNode BlendChild;
        }

        private readonly Stack<Frame> _stack = new();
        private readonly List<int> _candidates = new();
        private readonly System.Random _random;

        #endregion


        #region Main API

        public Resolver(int seed) => _random = new System.Random(seed);

        public void Resolve(SoundNode root, ISyncReader syncs, Plan plan)
        {
            _stack.Clear();
            if (root == null)
                return;

            _stack.Push(new Frame { Node = root, Layer = -1 });
            for (var guard = 0; guard < NodeWalker.MaxNodes && _stack.Count > 0; guard++)
                Visit(_stack.Pop(), syncs, plan);
            _stack.Clear();
        }

        internal int PickRandom(RandomNode node)
        {
            var count = node.children.Count;
            if (count == 0)
                return -1;

            var avoid = Mathf.Clamp(node.avoidRepeat, 0, count - 1);
            while (node.History.Count > avoid)
                node.History.RemoveAt(0);

            var total = Candidates(node, true);
            if (total <= 0f)
                total = Candidates(node, false);
            if (_candidates.Count == 0)
                return -1;

            var pick = PickWeighted(node, total);
            if (avoid > 0)
                node.History.Add(pick);
            return pick;
        }

        #endregion


        #region Private and Protected

        private void Visit(Frame frame, ISyncReader syncs, Plan plan)
        {
            var node = frame.Node;
            if (node == null)
                return;

            frame.Decibels += node.volume + Spread(node.volumeRandom);
            frame.Semitones += node.pitch + Spread(node.pitchRandom);

            switch (node)
            {
                case ClipNode clip:
                    AddClip(frame, clip, plan);
                    break;
                case RandomNode random:
                    PushChild(frame, random, PickRandom(random));
                    break;
                case SequenceNode { mode: SequenceMode.Step } step:
                    PushChild(frame, step, NextStep(step));
                    break;
                case SequenceNode continuous:
                    PushContinuous(frame, continuous, plan);
                    break;
                case SwitchNode switchNode:
                    PushChild(frame, switchNode, PickSwitch(switchNode, syncs));
                    break;
                case BlendNode blend:
                    PushAll(frame, blend, blend.parameter);
                    break;
                case ContainerNode container:
                    PushAll(frame, container, null);
                    break;
            }
        }

        private void AddClip(Frame frame, ClipNode node, Plan plan)
        {
            if (node.clip == null)
                return;

            var layer = frame.Layer >= 0 ? plan.Layers[frame.Layer] : NewLayer(frame, plan);
            layer.Clips.Add(new PlanClip
            {
                Clip = node.clip,
                Gain = AudioMath.ToLinear(frame.Decibels),
                Pitch = AudioMath.ToPitch(frame.Semitones),
                Loop = node.loop
            });
        }

        private static PlanLayer NewLayer(Frame frame, Plan plan)
        {
            var layer = plan.AddLayer();
            if (frame.BlendParameter != null && frame.BlendChild != null)
            {
                layer.BlendParameter = frame.BlendParameter;
                layer.BlendRange = frame.BlendChild.blendRange;
                layer.BlendFade = frame.BlendChild.blendFade;
            }
            return layer;
        }

        private void PushChild(Frame frame, ContainerNode container, int index)
        {
            if (index < 0 || index >= container.children.Count)
                return;
            frame.Node = container.children[index];
            _stack.Push(frame);
        }

        private void PushAll(Frame frame, ContainerNode container, SoundParameter blend)
        {
            for (var i = container.children.Count - 1; i >= 0; i--)
            {
                var child = frame;
                child.Node = container.children[i];
                if (blend != null)
                {
                    child.BlendParameter = blend;
                    child.BlendChild = child.Node;
                }
                _stack.Push(child);
            }
        }

        private void PushContinuous(Frame frame, SequenceNode sequence, Plan plan)
        {
            if (frame.Layer < 0)
            {
                var layer = NewLayer(frame, plan);
                layer.Loop = sequence.loop;
                frame.Layer = plan.Layers.Count - 1;
            }
            PushAll(frame, sequence, null);
        }

        private static int NextStep(SequenceNode node)
        {
            var count = node.children.Count;
            if (count == 0)
                return -1;
            var index = ((node.Next % count) + count) % count;
            node.Next = index + 1;
            return index;
        }

        private static int PickSwitch(SwitchNode node, ISyncReader syncs)
        {
            var value = node.group == null || syncs == null ? string.Empty : syncs.GetSwitch(node.group);
            for (var i = 0; i < node.children.Count; i++)
            {
                if (node.children[i] != null && node.children[i].switchValue == value)
                    return i;
            }
            return node.children.Count == 0 ? -1 : Mathf.Clamp(node.fallback, 0, node.children.Count - 1);
        }

        private float Candidates(RandomNode node, bool avoidHistory)
        {
            _candidates.Clear();
            var total = 0f;
            for (var i = 0; i < node.children.Count; i++)
            {
                var child = node.children[i];
                if (child == null || child.weight <= 0f || (avoidHistory && node.History.Contains(i)))
                    continue;
                _candidates.Add(i);
                total += child.weight;
            }
            return total;
        }

        private int PickWeighted(RandomNode node, float total)
        {
            var roll = (float)_random.NextDouble() * total;
            foreach (var index in _candidates)
            {
                roll -= node.children[index].weight;
                if (roll <= 0f)
                    return index;
            }
            return _candidates[_candidates.Count - 1];
        }

        private float Spread(float amount) => amount <= 0f ? 0f : ((float)_random.NextDouble() * 2f - 1f) * amount;

        #endregion
    }
}

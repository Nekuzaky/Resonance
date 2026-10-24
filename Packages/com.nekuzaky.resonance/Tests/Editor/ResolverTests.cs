using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Nekuzaky.Resonance.Tests
{
    public sealed class ResolverTests : Fixture
    {
        private sealed class FixedSwitch : ISyncReader
        {
            public string Value;

            public string GetSwitch(SwitchGroup group) => Value;
        }

        [Test]
        public void Clip_ProducesOneLayerWithOneClip()
        {
            var plan = Resolve(ClipNode("a"));

            Assert.AreEqual(1, plan.Layers.Count);
            Assert.AreEqual("a", plan.Layers[0].Clips[0].Clip.name);
        }

        [Test]
        public void Random_NeverRepeatsTheLastPick()
        {
            var random = Container<RandomNode>(ClipNode("a"), ClipNode("b"), ClipNode("c"));
            random.avoidRepeat = 1;
            var resolver = new Resolver(99);
            string previous = null;

            for (var i = 0; i < 300; i++)
            {
                var name = Resolve(resolver, random).Layers[0].Clips[0].Clip.name;
                Assert.AreNotEqual(previous, name, $"Pick {i} repeated {name}");
                previous = name;
            }
        }

        [Test]
        public void Random_SkipsChildrenWithZeroWeight()
        {
            var silent = ClipNode("never");
            silent.weight = 0f;
            var random = Container<RandomNode>(ClipNode("a"), silent);
            random.avoidRepeat = 0;
            var resolver = new Resolver(3);

            for (var i = 0; i < 200; i++)
                Assert.AreEqual("a", Resolve(resolver, random).Layers[0].Clips[0].Clip.name);
        }

        [Test]
        public void Random_FollowsWeights()
        {
            var heavy = ClipNode("heavy");
            heavy.weight = 3f;
            var random = Container<RandomNode>(heavy, ClipNode("light"));
            random.avoidRepeat = 0;
            var resolver = new Resolver(11);
            var heavyCount = 0;

            for (var i = 0; i < 4000; i++)
            {
                if (Resolve(resolver, random).Layers[0].Clips[0].Clip.name == "heavy")
                    heavyCount++;
            }
            Assert.That(heavyCount / 4000f, Is.InRange(0.71f, 0.79f));
        }

        [Test]
        public void StepSequence_CyclesThroughChildren()
        {
            var sequence = Container<SequenceNode>(ClipNode("a"), ClipNode("b"), ClipNode("c"));
            var names = new List<string>();

            for (var i = 0; i < 5; i++)
                names.Add(Resolve(sequence).Layers[0].Clips[0].Clip.name);

            CollectionAssert.AreEqual(new[] { "a", "b", "c", "a", "b" }, names);
        }

        [Test]
        public void ContinuousSequence_QueuesEveryClipInOrderOnOneLayer()
        {
            var sequence = Container<SequenceNode>(ClipNode("a"), Container<RandomNode>(ClipNode("b")), ClipNode("c"));
            sequence.mode = SequenceMode.Continuous;
            sequence.loop = true;

            var plan = Resolve(sequence);

            Assert.AreEqual(1, plan.Layers.Count);
            Assert.IsTrue(plan.Layers[0].Loop);
            var names = plan.Layers[0].Clips.ConvertAll(clip => clip.Clip.name);
            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, names);
        }

        [Test]
        public void Layer_PlaysEveryChildOnItsOwnLayer()
        {
            var plan = Resolve(Container<LayerNode>(ClipNode("a"), ClipNode("b"), ClipNode("c")));

            Assert.AreEqual(3, plan.Layers.Count);
        }

        [Test]
        public void Switch_PicksTheMatchingChildOrTheFallback()
        {
            var grass = ClipNode("grass");
            grass.switchValue = "Grass";
            var stone = ClipNode("stone");
            stone.switchValue = "Stone";
            var node = Container<SwitchNode>(grass, stone);
            node.group = Asset<SwitchGroup>("Surface");
            node.fallback = 1;

            Assert.AreEqual("grass", Resolve(node, new FixedSwitch { Value = "Grass" }).Layers[0].Clips[0].Clip.name);
            Assert.AreEqual("stone", Resolve(node, new FixedSwitch { Value = "Stone" }).Layers[0].Clips[0].Clip.name);
            Assert.AreEqual("stone", Resolve(node, new FixedSwitch { Value = "Water" }).Layers[0].Clips[0].Clip.name);
        }

        [Test]
        public void Blend_TagsEachLayerWithItsRange()
        {
            var slow = ClipNode("slow");
            slow.blendRange = new Vector2(0f, 0.5f);
            var fast = ClipNode("fast");
            fast.blendRange = new Vector2(0.5f, 1f);
            fast.blendFade = 0.2f;
            var blend = Container<BlendNode>(slow, fast);
            blend.parameter = Asset<SoundParameter>("Speed");

            var plan = Resolve(blend);

            Assert.AreEqual(2, plan.Layers.Count);
            foreach (var layer in plan.Layers)
                Assert.AreEqual(blend.parameter, layer.BlendParameter);
            Assert.That(plan.Layers.Exists(layer => layer.BlendRange == new Vector2(0.5f, 1f) && Mathf.Approximately(layer.BlendFade, 0.2f)));
        }

        [Test]
        public void Volumes_AddUpThroughTheTree()
        {
            var clip = ClipNode("a");
            clip.volume = -6f;
            var root = Container<RandomNode>(clip);
            root.volume = -6f;

            var gain = Resolve(root).Layers[0].Clips[0].Gain;

            Assert.AreEqual(AudioMath.ToLinear(-12f), gain, 0.0001f);
        }

        [Test]
        public void Clone_CopiesTheWholeTreeWithoutSharingChildren()
        {
            var inner = Container<RandomNode>(ClipNode("a"));
            var root = Container<LayerNode>(inner, ClipNode("b"));

            var copy = (LayerNode)root.Clone();
            ((RandomNode)copy.children[0]).children.Add(ClipNode("c"));

            Assert.AreEqual(1, inner.children.Count);
            Assert.AreEqual(2, ((RandomNode)copy.children[0]).children.Count);
            Assert.AreNotSame(root.children[1], copy.children[1]);
        }
    }
}

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Nekuzaky.Resonance.Tests
{
    public abstract class Fixture
    {
        private readonly List<Object> _created = new();
        private Stack<PlanLayer> _pool;

        protected ResonanceEngine Engine { get; private set; }

        [SetUp]
        public void CreateEngine()
        {
            var host = Track(new GameObject("Resonance Test Host") { hideFlags = HideFlags.HideAndDontSave });
            Engine = new ResonanceEngine(host.transform, 12345) { Simulate = true, Preview = true };
            _pool = new Stack<PlanLayer>();
        }

        [TearDown]
        public void Destroy()
        {
            Engine.Shutdown();
            foreach (var item in _created)
            {
                if (item != null)
                    Object.DestroyImmediate(item);
            }
            _created.Clear();
        }

        protected T Track<T>(T item) where T : Object
        {
            _created.Add(item);
            return item;
        }

        protected AudioClip Clip(string name, float seconds = 0.5f)
        {
            var clip = AudioClip.Create(name, Mathf.Max(1, (int)(seconds * 8000)), 1, 8000, false);
            return Track(clip);
        }

        protected T Asset<T>(string name) where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            asset.name = name;
            return Track(asset);
        }

        protected SoundEvent Event(SoundNode root, string name = "Test Event")
        {
            var sound = Asset<SoundEvent>(name);
            sound.root = root;
            return sound;
        }

        protected ClipNode ClipNode(string name, float seconds = 0.5f, bool loop = false) =>
            new() { name = name, clip = Clip(name, seconds), loop = loop };

        protected static T Container<T>(params SoundNode[] children) where T : ContainerNode, new()
        {
            var node = new T();
            node.children.AddRange(children);
            return node;
        }

        private protected Plan Resolve(SoundNode root, ISyncReader syncs = null, int seed = 7)
        {
            var plan = new Plan(_pool);
            new Resolver(seed).Resolve(root, syncs, plan);
            return plan;
        }

        private protected Plan Resolve(Resolver resolver, SoundNode root, ISyncReader syncs = null)
        {
            var plan = new Plan(_pool);
            resolver.Resolve(root, syncs, plan);
            return plan;
        }

        protected void Run(float seconds, float step = 0.02f)
        {
            for (var time = 0f; time < seconds; time += step)
                Engine.Tick(step);
        }
    }
}

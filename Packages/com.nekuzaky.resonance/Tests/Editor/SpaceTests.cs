using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Nekuzaky.Resonance.Tests
{
    public sealed class PlaybackOptionTests : Fixture
    {
        [Test]
        public void Shuffle_PlaysEveryChildOncePerCycleWithoutRepeatingAtTheSeam()
        {
            var random = Container<RandomNode>(ClipNode("a"), ClipNode("b"), ClipNode("c"), ClipNode("d"));
            random.shuffle = true;
            var resolver = new Resolver(5);
            string previous = null;

            for (var cycle = 0; cycle < 50; cycle++)
            {
                var seen = new HashSet<string>();
                for (var i = 0; i < 4; i++)
                {
                    var name = Resolve(resolver, random).Layers[0].Clips[0].Clip.name;
                    Assert.AreNotEqual(previous, name, $"Repeated {name} in cycle {cycle}");
                    seen.Add(name);
                    previous = name;
                }
                Assert.AreEqual(4, seen.Count, $"Cycle {cycle} missed a child");
            }
        }

        [Test]
        public void Delay_HoldsTheSoundBeforeItStarts()
        {
            var sound = Event(ClipNode("a", 0.3f));
            sound.delay = 0.5f;
            var handle = Engine.Play(sound, null, Vector3.zero);

            Run(0.45f);
            Assert.IsTrue(Engine.IsPlaying(handle.Id));
            Run(0.2f);
            Assert.IsTrue(Engine.IsPlaying(handle.Id), "Started after the delay and still playing");
            Run(0.4f);
            Assert.IsFalse(Engine.IsPlaying(handle.Id));
        }

        [Test]
        public void StoppingDuringTheDelay_NeverStartsTheSound()
        {
            var sound = Event(ClipNode("a", 0.3f));
            sound.delay = 1f;
            var handle = Engine.Play(sound, null, Vector3.zero);

            Engine.Stop(handle.Id, 0f);
            Run(0.1f);

            Assert.IsFalse(Engine.IsPlaying(handle.Id));
        }

        [Test]
        public void RandomStart_BeginsLoopsAtDifferentPositions()
        {
            var clip = ClipNode("loop", 2f, true);
            clip.randomStart = true;
            var sound = Event(clip);
            var offsets = new HashSet<int>();
            for (var i = 0; i < 10; i++)
            {
                Engine.Play(sound, null, Vector3.zero);
                offsets.Add(Mathf.RoundToInt(Engine.Instances[Engine.Instances.Count - 1].Voices[0].VirtualTime * 100f));
            }

            Assert.Greater(offsets.Count, 5);
        }
    }

    public sealed class AttenuationTests : Fixture
    {
        [Test]
        public void CustomCurve_DrivesTheAttenuation()
        {
            var spatial = new Spatial { rolloff = AudioRolloffMode.Custom, maxDistance = 10f, customCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f) };

            Assert.AreEqual(0.5f, spatial.Attenuation(5f), 0.001f);
            Assert.AreEqual(0f, spatial.Attenuation(20f), 0.001f);
        }

        [Test]
        public void DistanceLowPass_ClosesWithDistance()
        {
            var spatial = new Spatial { minDistance = 0f, maxDistance = 10f, lowPassAtMax = 500f };

            Assert.AreEqual(AudioMath.OpenLowPass, spatial.DistanceLowPass(0f), 1f);
            Assert.AreEqual(500f, spatial.DistanceLowPass(10f), 1f);
            Assert.That(spatial.DistanceLowPass(5f), Is.InRange(2000f, 5000f));
        }

        [Test]
        public void Preset_OverridesTheEventSettings()
        {
            var preset = Asset<AttenuationPreset>("Small Object");
            preset.spatial.maxDistance = 7f;
            var sound = Event(ClipNode("a"));

            Assert.AreEqual(30f, sound.Spatial.maxDistance);
            sound.attenuation = preset;
            Assert.AreEqual(7f, sound.Spatial.maxDistance);
        }

        [Test]
        public void FarSounds_GetTheDistanceLowPass()
        {
            Engine.Preview = false;
            var listener = Track(new GameObject("Listener"));
            Engine.Listener = listener.transform;
            var sound = Event(ClipNode("a", 5f));
            sound.spatial.minDistance = 0f;
            sound.spatial.maxDistance = 20f;
            sound.spatial.lowPassAtMax = 800f;
            sound.virtualizeWhenFar = false;

            Engine.Play(sound, null, new Vector3(20f, 0f, 0f));
            Run(0.05f);

            Assert.AreEqual(800f, Engine.Instances[0].LowPass, 1f);
        }
    }

    public sealed class OcclusionTests : Fixture
    {
        [Test]
        public void Wall_BetweenEmitterAndListener_MufflesTheSound()
        {
            Engine.Preview = false;
            var listener = Track(new GameObject("Listener"));
            Engine.Listener = listener.transform;
            var wall = Track(GameObject.CreatePrimitive(PrimitiveType.Cube));
            wall.transform.position = new Vector3(5f, 0f, 0f);
            wall.transform.localScale = new Vector3(0.5f, 10f, 10f);
            Physics.SyncTransforms();
            var sound = Event(ClipNode("a", 5f));
            sound.occlusion.enabled = true;
            sound.occlusion.smoothing = 0f;
            sound.occlusion.volume = -12f;
            sound.occlusion.lowPass = 1000f;

            Engine.Play(sound, null, new Vector3(10f, 0f, 0f));
            Run(0.05f);
            var instance = Engine.Instances[0];
            Assert.AreEqual(1f, instance.Occlusion, 0.001f);
            Assert.AreEqual(1000f, instance.LowPass, 1f);
            Assert.AreEqual(AudioMath.ToLinear(-12f), instance.Gain, 0.001f);

            wall.transform.position = new Vector3(5f, 50f, 0f);
            Physics.SyncTransforms();
            Run(0.3f);
            Assert.AreEqual(0f, instance.Occlusion, 0.001f);
        }
    }

    public sealed class ReverbTests : Fixture
    {
        [Test]
        public void Presets_AreReadFromUnity()
        {
            Assert.Greater(Engine.Reverb.Preset(AudioReverbPreset.Cave).DecayTime, Engine.Reverb.Preset(AudioReverbPreset.Room).DecayTime);
            Assert.AreEqual(-10000f, Engine.Reverb.Preset(AudioReverbPreset.Off).Room, 1f);
        }

        [Test]
        public void Zone_IsFullInsideAndFadesOutside()
        {
            var listener = Track(new GameObject("Listener"));
            var room = Track(new GameObject("Room", typeof(BoxCollider)));
            room.transform.localScale = new Vector3(10f, 4f, 10f);
            var zone = room.AddComponent<ReverbZone>();
            zone.fadeDistance = 4f;
            Physics.SyncTransforms();

            Engine.Reverb.Tick(listener.transform, ReverbZone.Active);
            Assert.AreEqual(1f, Engine.Reverb.Wet, 0.001f);
            Assert.AreEqual(zone, Engine.Reverb.Dominant);

            listener.transform.position = new Vector3(7f, 0f, 0f);
            Engine.Reverb.Tick(listener.transform, ReverbZone.Active);
            Assert.AreEqual(0.5f, Engine.Reverb.Wet, 0.01f);

            listener.transform.position = new Vector3(20f, 0f, 0f);
            Engine.Reverb.Tick(listener.transform, ReverbZone.Active);
            Assert.AreEqual(0f, Engine.Reverb.Wet);
        }
    }

    public sealed class MemoryTests : Fixture
    {
        [Test]
        public void Banks_AreReferenceCounted()
        {
            var shared = ClipNode("shared");
            var first = Asset<SoundBank>("Level 1");
            first.events.Add(Event(Container<LayerNode>(shared, ClipNode("only one"))));
            var second = Asset<SoundBank>("Level 2");
            second.events.Add(Event(new ClipNode { name = "shared", clip = shared.clip }));
            var memory = new AudioMemory();

            memory.Load(first);
            memory.Load(second);
            Assert.AreEqual(2, memory.Clips[shared.clip]);
            Assert.AreEqual(2, memory.Clips.Count);

            memory.Unload(first);
            Assert.IsFalse(memory.IsLoaded(first));
            Assert.AreEqual(1, memory.Clips.Count);
            Assert.AreEqual(1, memory.Clips[shared.clip]);

            memory.Unload(second);
            Assert.AreEqual(0, memory.Clips.Count);
        }
    }
}

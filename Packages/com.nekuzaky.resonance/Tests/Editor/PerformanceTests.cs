using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Nekuzaky.Resonance.Tests
{
    public sealed class PerformanceTests : Fixture
    {
        private static object _keep;

        [Test]
        public void AllocationCheck_SeesSmallAllocations()
        {
            Assert.That(() => { _keep = new int[4]; }, Is.AllocatingGCMemory());
        }

        [Test]
        public void PlayAndTick_AllocateNothingOnceWarmedUp()
        {
            var listener = Track(new GameObject("Listener"));
            Engine.Listener = listener.transform;
            var emitter = Track(new GameObject("Emitter"));
            var parameter = Asset<SoundParameter>("Speed");
            var surface = Asset<SwitchGroup>("Surface");
            var grass = Container<RandomNode>(ClipNode("a", 0.05f), ClipNode("b", 0.05f));
            grass.switchValue = "Default";
            var sound = Event(Container<SwitchNode>(grass));
            ((SwitchNode)sound.root).group = surface;
            sound.bus = Asset<AudioBus>("SFX");
            sound.spatial.binaural = true;
            sound.modulations.Add(new Modulation { parameter = parameter, curve = AnimationCurve.Linear(0f, 0f, 1f, -6f) });

            Cycle(sound, emitter, parameter, 50);

            Assert.That(() => Cycle(sound, emitter, parameter, 500), Is.Not.AllocatingGCMemory());
        }

        private void Cycle(SoundEvent sound, GameObject emitter, SoundParameter parameter, int plays)
        {
            for (var i = 0; i < plays; i++)
            {
                Engine.Syncs.SetParameter(parameter, i % 10 / 10f, null, true);
                Engine.Play(sound, emitter, Vector3.zero);
                Engine.Tick(0.03f);
            }
        }
    }

    public sealed class MeteringTests : Fixture
    {
        [Test]
        public void Attenuation_FollowsTheRolloff()
        {
            Assert.AreEqual(1f, AudioMath.Attenuation(AudioRolloffMode.Logarithmic, 1f, 30f, 0.5f));
            Assert.AreEqual(0.5f, AudioMath.Attenuation(AudioRolloffMode.Logarithmic, 1f, 30f, 2f), 0.0001f);
            Assert.AreEqual(1f / 30f, AudioMath.Attenuation(AudioRolloffMode.Logarithmic, 1f, 30f, 100f), 0.0001f);
            Assert.AreEqual(0.5f, AudioMath.Attenuation(AudioRolloffMode.Linear, 0f, 10f, 5f), 0.01f);
            Assert.AreEqual(0f, AudioMath.Attenuation(AudioRolloffMode.Linear, 0f, 10f, 20f));
        }

        [Test]
        public void Envelope_MeasuresTheClipLevel()
        {
            var clip = Track(AudioClip.Create("half", 4096, 1, 8000, false));
            var data = new float[4096];
            for (var i = 0; i < data.Length; i++)
                data[i] = i < 2048 ? 0.5f : 0f;
            clip.SetData(data, 0);

            Assert.AreEqual(0.5f, ClipEnvelopes.At(clip, 100), 0.001f);
            Assert.AreEqual(0f, ClipEnvelopes.At(clip, 3000), 0.001f);
        }
    }
}

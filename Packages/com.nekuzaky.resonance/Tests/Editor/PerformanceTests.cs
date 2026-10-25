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

        [Test]
        public void BusEffects_AllocateNothingOnceWarmedUp()
        {
            var parameter = Asset<SoundParameter>("Depth");
            var sound = EffectSound(parameter, out var snapshot);
            Engine.Mixer.Apply(snapshot);

            Cycle(sound, null, parameter, 50);

            Assert.That(() => Cycle(sound, null, parameter, 500), Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void BusEffectTailsAndCompressor_AllocateNothingOnceWarmedUp()
        {
            Engine.Simulate = false;
            var parameter = Asset<SoundParameter>("Depth");
            var sound = EffectSound(parameter, out var snapshot);
            Engine.Mixer.Apply(snapshot);

            StopCycle(sound, parameter, 80);

            Assert.That(() => StopCycle(sound, parameter, 400), Is.Not.AllocatingGCMemory());
            Assert.Greater(Engine.RingingVoices, 0);
        }

        [Test]
        public void Tick_StaysFastWithFiveHundredVoices()
        {
            var listener = Track(new GameObject("Listener"));
            Engine.Listener = listener.transform;
            Engine.MaxVoices = 512;
            var parameter = Asset<SoundParameter>("Speed");
            var sound = Event(Container<BlendNode>(ClipNode("low", 4f, true), ClipNode("high", 4f, true)));
            ((BlendNode)sound.root).parameter = parameter;
            sound.bus = Asset<AudioBus>("SFX");
            sound.modulations.Add(new Modulation { parameter = parameter, curve = AnimationCurve.Linear(0f, 0f, 1f, -6f) });
            for (var i = 0; i < 250; i++)
            {
                var emitter = Track(new GameObject("Emitter"));
                emitter.transform.position = new Vector3(i % 25 * 4f, 0f, i / 25 * 4f);
                Engine.Play(sound, emitter, Vector3.zero);
            }

            var milliseconds = AverageTick(120);

            Assert.AreEqual(500, Engine.Stats.VirtualVoices + Engine.Stats.RealVoices);
            Debug.Log($"[Scale] 250 sounds, 500 voices: {milliseconds:0.000} ms per tick");
            Assert.Less(milliseconds, 1f);
        }

        [Test]
        public void Tick_StaysFastWhileStealingEveryFrame()
        {
            var listener = Track(new GameObject("Listener"));
            Engine.Listener = listener.transform;
            var sound = Event(ClipNode("hit", 0.5f));
            sound.maxInstances = 48;
            AverageTick(1);
            var started = System.Diagnostics.Stopwatch.GetTimestamp();
            for (var frame = 0; frame < 120; frame++)
            {
                for (var i = 0; i < 20; i++)
                    Engine.Play(sound, null, new Vector3(i, 0f, frame % 7));
                Engine.Tick(1f / 60f);
            }
            var milliseconds = (System.Diagnostics.Stopwatch.GetTimestamp() - started) * 1000.0 / System.Diagnostics.Stopwatch.Frequency / 120.0;

            Assert.LessOrEqual(Engine.CountPlaying(sound, null), 48);
            Assert.LessOrEqual(Engine.Stats.Instances, Engine.MaxVoices);
            Debug.Log($"[Scale] 20 plays per frame against a limit of 48: {milliseconds:0.000} ms per frame");
            Assert.Less(milliseconds, 1f);
        }

        private double AverageTick(int frames)
        {
            for (var i = 0; i < 10; i++)
                Engine.Tick(1f / 60f);
            var started = System.Diagnostics.Stopwatch.GetTimestamp();
            for (var i = 0; i < frames; i++)
                Engine.Tick(1f / 60f);
            return (System.Diagnostics.Stopwatch.GetTimestamp() - started) * 1000.0 / System.Diagnostics.Stopwatch.Frequency / frames;
        }

        private SoundEvent EffectSound(SoundParameter parameter, out MixSnapshot snapshot)
        {
            var master = Asset<AudioBus>("Master");
            var sfx = Asset<AudioBus>("SFX");
            sfx.parent = master;
            var lowPass = sfx.AddEffect(BusEffectType.LowPass);
            lowPass.modulations.Add(new EffectModulation { parameter = parameter, curve = AnimationCurve.Linear(0f, 20000f, 1f, 800f) });
            sfx.AddEffect(BusEffectType.HighPass);
            sfx.AddEffect(BusEffectType.Distortion);
            var echo = sfx.AddEffect(BusEffectType.Delay);
            master.AddEffect(BusEffectType.Reverb);
            master.AddEffect(BusEffectType.Compressor);
            snapshot = Asset<MixSnapshot>("Underwater");
            snapshot.fadeIn = 2f;
            snapshot.effects.Add(new EffectOffset { bus = sfx, effect = echo.id, parameter = BusEffects.DelayWet, value = 1f });
            var sound = Event(Container<RandomNode>(ClipNode("a", 0.05f), ClipNode("b", 0.05f)));
            sound.bus = sfx;
            return sound;
        }

        private void StopCycle(SoundEvent sound, SoundParameter parameter, int plays)
        {
            for (var i = 0; i < plays; i++)
            {
                Engine.Syncs.SetParameter(parameter, i % 10 / 10f, null, true);
                var handle = Engine.Play(sound, null, Vector3.zero);
                Engine.Tick(0.03f);
                Engine.Stop(handle.Id, 0f);
                Engine.Tick(0.03f);
            }
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

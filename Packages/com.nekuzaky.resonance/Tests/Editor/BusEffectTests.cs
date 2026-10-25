using System.Threading;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Nekuzaky.Resonance.Tests
{
    public sealed class BusEffectTests : Fixture
    {
        #region Tests

        [Test]
        public void LowPass_AppliesToSoundsOnTheBusAndItsChildren()
        {
            var master = Asset<AudioBus>("Master");
            var sfx = Asset<AudioBus>("SFX");
            sfx.parent = master;
            master.AddEffect(BusEffectType.LowPass).SetValue(BusEffects.FilterCutoff, 1200f);

            var rack = PlayOn(sfx);

            Assert.IsTrue(rack.Dsp.enabled);
            Assert.AreEqual(1, rack.Dsp.Stages);
            Assert.AreEqual(StageKind.LowPass, rack.Dsp.Kind(0));
            Assert.AreEqual(1200f, rack.Dsp.Value(0, 0), 0.01f);
            Assert.IsFalse(rack.Echo.enabled);
            Assert.IsFalse(rack.Reverb.enabled);
        }

        [Test]
        public void BusWithoutEffects_LeavesEveryFilterOff()
        {
            var rack = PlayOn(Asset<AudioBus>("SFX"));

            Assert.IsFalse(rack.Active);
            Assert.IsFalse(rack.Dsp.enabled || rack.Echo.enabled || rack.Reverb.enabled);
        }

        [Test]
        public void Bypass_TurnsTheEffectOffWhilePlaying()
        {
            var bus = Asset<AudioBus>("SFX");
            var echo = bus.AddEffect(BusEffectType.Delay);
            var rack = PlayOn(bus);
            Assert.IsTrue(rack.Echo.enabled);

            echo.bypass = true;
            Engine.Tick(0.02f);

            Assert.IsFalse(rack.Echo.enabled);
        }

        [Test]
        public void ChainOrder_IsKeptForFiltersAndDistortion()
        {
            var bus = Asset<AudioBus>("Guitar");
            bus.AddEffect(BusEffectType.HighPass).SetValue(BusEffects.FilterCutoff, 300f);
            bus.AddEffect(BusEffectType.Distortion).SetValue(BusEffects.DistortionDrive, 0.7f);
            bus.AddEffect(BusEffectType.LowPass).SetValue(BusEffects.FilterCutoff, 4000f);

            var rack = PlayOn(bus);

            Assert.AreEqual(3, rack.Dsp.Stages);
            Assert.AreEqual(StageKind.HighPass, rack.Dsp.Kind(0));
            Assert.AreEqual(300f, rack.Dsp.Value(0, 0), 0.01f);
            Assert.AreEqual(StageKind.Distortion, rack.Dsp.Kind(1));
            Assert.AreEqual(0.7f, rack.Dsp.Value(1, 0), 0.0001f);
            Assert.AreEqual(StageKind.LowPass, rack.Dsp.Kind(2));
            Assert.AreEqual(4000f, rack.Dsp.Value(2, 0), 0.01f);
        }

        [Test]
        public void ChildChain_RunsBeforeTheParentChain()
        {
            var master = Asset<AudioBus>("Master");
            var sfx = Asset<AudioBus>("SFX");
            sfx.parent = master;
            sfx.AddEffect(BusEffectType.LowPass).SetValue(BusEffects.FilterCutoff, 3000f);
            master.AddEffect(BusEffectType.LowPass).SetValue(BusEffects.FilterCutoff, 8000f);

            var dsp = PlayOn(sfx).Dsp;

            Assert.AreEqual(2, dsp.Stages);
            Assert.AreEqual(3000f, dsp.Value(0, 0), 0.01f);
            Assert.AreEqual(8000f, dsp.Value(1, 0), 0.01f);
        }

        [Test]
        public void LowPass_AttenuatesHighFrequenciesOnly()
        {
            Assert.Less(Gain(StageKind.LowPass, 500f, 10000f), AudioMath.ToLinear(-40f));
            Assert.AreEqual(1f, Gain(StageKind.LowPass, 500f, 50f), 0.02f);
            Assert.AreEqual(0.7071f, Gain(StageKind.LowPass, 1000f, 1000f), 0.03f);
        }

        [Test]
        public void HighPass_AttenuatesLowFrequenciesOnly()
        {
            Assert.Less(Gain(StageKind.HighPass, 2000f, 100f), AudioMath.ToLinear(-40f));
            Assert.AreEqual(1f, Gain(StageKind.HighPass, 200f, 8000f), 0.02f);
        }

        [Test]
        public void Distortion_ClipsLoudPeaksAndKeepsTheDryMix()
        {
            Assert.AreEqual(1f, EffectDsp.SoftClip(3f), 0.0001f);
            Assert.AreEqual(-1f, EffectDsp.SoftClip(-10f), 0.0001f);
            Assert.AreEqual(0.1f, EffectDsp.SoftClip(0.1f), 0.001f);

            var dsp = Dsp(StageKind.Distortion, 1f, 0f, 1f);
            var data = Sine(1000f, 1f, 4800, 1);
            dsp.Process(data, 1);
            Assert.AreEqual(1f, Peak(data), 0.001f);
            Assert.Greater(Rms(data, 0), 0.9f);

            var dry = Dsp(StageKind.Distortion, 1f, 0f, 0f);
            var clean = Sine(1000f, 0.5f, 4800, 1);
            dry.Process(clean, 1);
            Assert.AreEqual(0.5f, Peak(clean), 0.001f);
        }

        [Test]
        public void Dsp_KeepsThePreviousStagesWhileAnUpdateIsBeingWritten()
        {
            var dsp = Dsp(StageKind.LowPass, 1200f, 1f, 1f);
            dsp.Process(new float[8], 1);

            dsp.BeginPublish();
            dsp.WriteStage(0, StageKind.Distortion, 1200f);
            dsp.Process(new float[8], 1);
            Assert.AreEqual(StageKind.LowPass, dsp.PreparedKind(0));
            Assert.AreEqual(1200f, dsp.PreparedValue(0));

            dsp.WriteStage(0, StageKind.Distortion, 0.5f);
            dsp.EndPublish();
            dsp.Process(new float[8], 1);
            Assert.AreEqual(StageKind.Distortion, dsp.PreparedKind(0));
            Assert.AreEqual(0.5f, dsp.PreparedValue(0));
        }

        [Test]
        public void Dsp_NeverPairsAStageKindWithTheParametersOfAnother()
        {
            var dsp = Dsp(StageKind.LowPass, 1200f, 1f, 1f);
            var low = Chain(StageKind.LowPass, 1200f, 1f, 1f);
            var crunch = Chain(StageKind.Distortion, 0.5f, 0f, 1f);
            dsp.Process(new float[2], 1);
            var stop = 0;
            var torn = 0;
            var processed = 0;
            var audio = new Thread(() =>
            {
                var data = new float[2];
                for (var i = 0; i < 50000000 && Volatile.Read(ref stop) == 0; i++)
                {
                    dsp.Process(data, 1);
                    var kind = dsp.PreparedKind(0);
                    var value = dsp.PreparedValue(0);
                    if (!(kind == StageKind.LowPass && value == 1200f) && !(kind == StageKind.Distortion && value == 0.5f))
                        torn++;
                    processed++;
                }
            });
            audio.Start();
            for (var i = 0; i < 200000; i++)
                dsp.Configure(i % 2 == 0 ? crunch : low);
            Volatile.Write(ref stop, 1);
            audio.Join();

            Assert.Greater(processed, 0);
            Assert.AreEqual(0, torn);
        }

        [Test]
        public void Dsp_RecoversFromANonFiniteFilterState()
        {
            var dsp = Dsp(StageKind.LowPass, 500f, 1f, 1f);
            dsp.Process(new[] { float.NaN, 0f, 0f, 0f }, 1);
            Assert.IsFalse(dsp.HasState);

            var data = Sine(100f, 0.5f, 4800, 1);
            dsp.Process(data, 1);

            Assert.IsTrue(float.IsFinite(Rms(data, 0)));
            Assert.Greater(Rms(data, 0, 2400), 0.3f);
        }

        [Test]
        public void Dsp_ProcessesEveryChannelWithoutAllocating()
        {
            var dsp = Dsp(StageKind.LowPass, 300f, 1f, 1f);
            var data = Sine(8000f, 1f, 4800, 2);
            dsp.Process(data, 2);

            Assert.Less(Rms(data, 0, 2400, 2), 0.02f);
            Assert.Less(Rms(data, 1, 2400, 2), 0.02f);
            Assert.That(() => dsp.Process(data, 2), Is.Not.AllocatingGCMemory());
        }

        [Test]
        public void Parameter_DrivesAnEffectThroughItsCurve()
        {
            var depth = Asset<SoundParameter>("Depth");
            var bus = Asset<AudioBus>("SFX");
            var lowPass = bus.AddEffect(BusEffectType.LowPass);
            lowPass.modulations.Add(new EffectModulation
            {
                parameter = depth,
                target = BusEffects.FilterCutoff,
                curve = AnimationCurve.Linear(0f, 20000f, 1f, 500f)
            });
            var rack = PlayOn(bus);
            Assert.AreEqual(20000f, rack.Dsp.Value(0, 0), 1f);

            Engine.Syncs.SetParameter(depth, 1f, null, true);
            Engine.Tick(0.02f);

            Assert.AreEqual(500f, rack.Dsp.Value(0, 0), 1f);
        }

        [Test]
        public void Snapshot_FadesEffectParameters()
        {
            var bus = Asset<AudioBus>("SFX");
            var lowPass = bus.AddEffect(BusEffectType.LowPass);
            lowPass.SetValue(BusEffects.FilterCutoff, 22000f);
            var echo = bus.AddEffect(BusEffectType.Delay);
            echo.SetValue(BusEffects.DelayWet, 0f);
            var underwater = Asset<MixSnapshot>("Underwater");
            underwater.fadeIn = 1f;
            underwater.effects.Add(new EffectOffset { bus = bus, effect = lowPass.id, parameter = BusEffects.FilterCutoff, value = 1000f });
            underwater.effects.Add(new EffectOffset { bus = bus, effect = echo.id, parameter = BusEffects.DelayWet, value = 0.8f });
            var rack = PlayOn(bus, 4f);
            Assert.AreEqual(0, rack.Dsp.Stages);

            Engine.Mixer.Apply(underwater);
            Run(0.5f, 0.05f);
            Engine.Tick(0f);
            var weight = Engine.Mixer.Snapshots[0].Weight;
            Assert.AreEqual(0.5f, weight, 0.06f);
            Assert.AreEqual(AudioMath.LogLerp(22000f, 1000f, weight), rack.Dsp.Value(0, 0), 1f);
            Assert.AreEqual(0.8f * weight, rack.Echo.wetMix, 0.001f);

            Run(0.6f, 0.05f);
            Engine.Tick(0f);
            Assert.AreEqual(1000f, rack.Dsp.Value(0, 0), 0.5f);
            Assert.AreEqual(0.8f, rack.Echo.wetMix, 0.001f);
        }

        [Test]
        public void Reverb_SendKeepsTheDrySignalAndInsertMixesIt()
        {
            var bus = Asset<AudioBus>("SFX");
            var reverb = bus.AddEffect(BusEffectType.Reverb);
            reverb.SetValue(BusEffects.ReverbMix, 0.5f);
            reverb.SetValue(BusEffects.ReverbDecay, 3f);
            var rack = PlayOn(bus);

            Assert.IsTrue(rack.Reverb.enabled);
            Assert.AreEqual(0f, rack.Reverb.dryLevel, 0.01f);
            Assert.AreEqual(EffectRack.Millibels(0.5f), rack.Reverb.room, 1f);
            Assert.AreEqual(3f, rack.Reverb.decayTime, 0.001f);

            reverb.routing = ReverbRouting.Insert;
            reverb.SetValue(BusEffects.ReverbMix, 0.75f);
            Engine.Tick(0.02f);

            Assert.AreEqual(EffectRack.Millibels(0.25f), rack.Reverb.dryLevel, 1f);
            Assert.AreEqual(EffectRack.Millibels(0.75f), rack.Reverb.room, 1f);
        }

        [Test]
        public void Delay_SetsTheEchoAndATailLongEnoughForItsRepeats()
        {
            var bus = Asset<AudioBus>("SFX");
            var echo = bus.AddEffect(BusEffectType.Delay);
            echo.SetValue(BusEffects.DelayTime, 250f);
            echo.SetValue(BusEffects.DelayFeedback, 0.5f);

            var rack = PlayOn(bus);

            Assert.AreEqual(250f, rack.Echo.delay, 0.01f);
            Assert.AreEqual(0.5f, rack.Echo.decayRatio, 0.0001f);
            Assert.AreEqual(EffectChain.DelayTail(0.25f, 0.5f), rack.Tail, 0.0001f);
            Assert.Greater(rack.Tail, 2.5f);
        }

        [Test]
        public void Compressor_ReducesTheBusAboveItsThreshold()
        {
            var bus = Asset<AudioBus>("Dialogue");
            var compressor = bus.AddEffect(BusEffectType.Compressor);
            compressor.SetValue(BusEffects.CompressorThreshold, -20f);
            compressor.SetValue(BusEffects.CompressorRatio, 4f);
            compressor.SetValue(BusEffects.CompressorAttack, 0f);
            compressor.SetValue(BusEffects.CompressorMakeup, 3f);
            var mixer = new BusMixer();

            mixer.BeginFrame();
            mixer.Detect(bus, 1f);
            mixer.Tick(0.02f);

            Assert.AreEqual(15f, mixer.GainReduction(bus), 0.001f);
            Assert.AreEqual(AudioMath.ToLinear(-12f), mixer.Gain(bus), 0.0001f);
        }

        [Test]
        public void Compressor_LeavesQuietSignalsAloneAndReleases()
        {
            var bus = Asset<AudioBus>("Dialogue");
            var compressor = bus.AddEffect(BusEffectType.Compressor);
            compressor.SetValue(BusEffects.CompressorThreshold, -20f);
            compressor.SetValue(BusEffects.CompressorAttack, 0f);
            compressor.SetValue(BusEffects.CompressorRelease, 0.1f);
            var mixer = new BusMixer();

            mixer.BeginFrame();
            mixer.Detect(bus, AudioMath.ToLinear(-30f));
            mixer.Tick(0.02f);
            Assert.AreEqual(0f, mixer.GainReduction(bus), 0.0001f);

            mixer.BeginFrame();
            mixer.Detect(bus, 1f);
            mixer.Tick(0.02f);
            Assert.Greater(mixer.GainReduction(bus), 10f);
            for (var i = 0; i < 100; i++)
            {
                mixer.BeginFrame();
                mixer.Tick(0.02f);
            }
            Assert.AreEqual(0f, mixer.GainReduction(bus), 0.01f);
            Assert.AreEqual(1f, mixer.Gain(bus), 0.001f);
        }

        [Test]
        public void Compressor_OnTheParentSeesTheChildBusLevel()
        {
            var master = Asset<AudioBus>("Master");
            var sfx = Asset<AudioBus>("SFX");
            sfx.parent = master;
            sfx.volume = -6f;
            var compressor = master.AddEffect(BusEffectType.Compressor);
            compressor.SetValue(BusEffects.CompressorThreshold, -12f);
            compressor.SetValue(BusEffects.CompressorRatio, 2f);
            compressor.SetValue(BusEffects.CompressorAttack, 0f);
            var mixer = new BusMixer();

            mixer.BeginFrame();
            mixer.Detect(sfx, 1f);
            mixer.Tick(0.02f);

            Assert.AreEqual(3f, mixer.GainReduction(master), 0.001f);
            Assert.AreEqual(0f, mixer.GainReduction(sfx));
        }

        [Test]
        public void Tail_KeepsTheVoiceRingingAfterTheSoundEnds()
        {
            Engine.Simulate = false;
            var bus = Asset<AudioBus>("SFX");
            bus.AddEffect(BusEffectType.Delay).SetValue(BusEffects.DelayFeedback, 0f);
            var sound = Event(ClipNode("hit", 0.1f));
            sound.bus = bus;
            var handle = Engine.Play(sound, null, Vector3.zero);
            Engine.Tick(0.02f);
            var rack = Engine.Instances[0].Voices[0].Effects;

            Engine.Stop(handle.Id, 0f);
            Engine.Tick(0.02f);
            Engine.Tick(0.02f);

            Assert.IsFalse(Engine.IsPlaying(handle.Id));
            Assert.AreEqual(1, Engine.RingingVoices);
            Assert.IsTrue(rack.Echo.enabled);
            Run(rack.Tail + 0.1f);
            Assert.AreEqual(0, Engine.RingingVoices);
        }

        [Test]
        public void Tail_IsNotKeptWithoutDelayOrReverb()
        {
            Engine.Simulate = false;
            var bus = Asset<AudioBus>("SFX");
            bus.AddEffect(BusEffectType.LowPass);
            var sound = Event(ClipNode("hit", 0.1f));
            sound.bus = bus;
            var handle = Engine.Play(sound, null, Vector3.zero);
            Engine.Tick(0.02f);

            Engine.Stop(handle.Id, 0f);
            Run(0.1f);

            Assert.AreEqual(0, Engine.RingingVoices);
        }

        [Test]
        public void Distortion_CompensatesTheGainTheSourceReallyApplies()
        {
            var bus = Asset<AudioBus>("Boosted");
            bus.volume = 12f;
            Crunch(bus);

            var peak = DistortedPeak(PlayOn(bus));

            Assert.AreEqual(1f, peak, 0.01f, "The source volume saturates at 1, so the drive must not be divided by the full bus gain");
        }

        [Test]
        public void Distortion_CompensatesCompressorMakeupAboveUnity()
        {
            var bus = Asset<AudioBus>("Makeup");
            var compressor = bus.AddEffect(BusEffectType.Compressor);
            compressor.SetValue(BusEffects.CompressorThreshold, 0f);
            compressor.SetValue(BusEffects.CompressorMakeup, 12f);
            Crunch(bus);
            var rack = PlayOn(bus);
            Engine.Tick(0.02f);

            Assert.AreEqual(AudioMath.ToLinear(12f), Engine.Mixer.Gain(bus), 0.01f);
            Assert.AreEqual(1f, DistortedPeak(rack), 0.01f);
        }

        [Test]
        public void StopAll_CutsTheTailsOfEndedSounds()
        {
            var rack = Ringing(Asset<AudioBus>("SFX"));

            Engine.StopAll(null, 0f);
            Engine.Tick(0.02f);

            Assert.AreEqual(0, Engine.RingingVoices);
            Assert.IsFalse(rack.Echo.enabled);
        }

        [Test]
        public void StopAll_FadesTailsOverTheFadeTime()
        {
            var rack = Ringing(Asset<AudioBus>("SFX"));

            Engine.StopAll(null, 0.2f);
            Run(0.1f);
            Assert.AreEqual(1, Engine.RingingVoices);
            Assert.AreEqual(0.5f, rack.Fader.Gain, 0.15f);
            Run(0.15f);

            Assert.AreEqual(0, Engine.RingingVoices);
        }

        [Test]
        public void Tail_FollowsTheBusVolumeAndMute()
        {
            var bus = Asset<AudioBus>("SFX");
            var rack = Ringing(bus);
            Assert.IsTrue(rack.Fader.enabled);
            Assert.AreEqual(1f, rack.Fader.Gain, 0.0001f);

            bus.volume = -6f;
            Engine.Tick(0.02f);
            Assert.AreEqual(AudioMath.ToLinear(-6f), rack.Fader.Gain, 0.001f);

            bus.mute = true;
            Engine.Tick(0.02f);
            Assert.AreEqual(0f, rack.Fader.Gain);

            bus.mute = false;
            Engine.Mixer.State(bus).UserVolume = AudioMath.SilenceDb;
            Engine.Tick(0.02f);
            Assert.Less(rack.Fader.Gain, 0.001f);
        }

        [Test]
        public void Tail_FollowsSnapshotsAppliedAfterTheRelease()
        {
            var bus = Asset<AudioBus>("SFX");
            var rack = Ringing(bus);
            var quiet = Asset<MixSnapshot>("Quiet");
            quiet.fadeIn = 0f;
            quiet.offsets.Add(new BusOffset { bus = bus, volume = -12f });

            Engine.Mixer.Apply(quiet);
            Engine.Tick(0.02f);

            Assert.AreEqual(AudioMath.ToLinear(-12f), rack.Fader.Gain, 0.001f);
        }

        [Test]
        public void ReusedTailVoice_StartsWithCleanEffects()
        {
            Engine.MaxVoices = 1;
            var bus = Asset<AudioBus>("SFX");
            bus.AddEffect(BusEffectType.LowPass).SetValue(BusEffects.FilterCutoff, 500f);
            var rack = Ringing(bus);
            var noise = Sine(5000f, 1f, 256, 1);
            rack.Dsp.Process(noise, 1);
            Assert.IsTrue(rack.Dsp.HasState);

            var sound = Event(ClipNode("next", 1f));
            sound.bus = bus;
            Engine.Play(sound, null, Vector3.zero);

            Assert.AreEqual(0, Engine.RingingVoices);
            Assert.AreSame(rack, Engine.Instances[0].Voices[0].Effects);
            Assert.AreEqual(1f, rack.Fader.Gain);
            rack.Dsp.Process(new float[4], 1);
            Assert.IsFalse(rack.Dsp.HasState, "The filter must not keep the old tail");
        }

        [Test]
        public void OldBusAsset_LoadsWithAnEmptyChain()
        {
            var bus = Asset<AudioBus>("Old");
            bus.effects = null;
            JsonUtility.FromJsonOverwrite("{\"volume\":-3.0,\"mute\":false,\"ducking\":[]}", bus);
            var snapshot = Asset<MixSnapshot>("Old Snapshot");
            JsonUtility.FromJsonOverwrite("{\"fadeIn\":0.5,\"fadeOut\":0.5,\"offsets\":[]}", snapshot);

            Assert.AreEqual(-3f, bus.volume);
            Assert.IsNotNull(snapshot.effects);
            Assert.IsFalse(new BusMixer().Chain(Asset<AudioBus>("Fresh")).Any);
        }

        [Test]
        public void ShortValueArray_FallsBackToDefaults()
        {
            var effect = new BusEffect(BusEffectType.Compressor) { values = new[] { -30f } };

            Assert.AreEqual(-30f, effect.Value(BusEffects.CompressorThreshold));
            Assert.AreEqual(BusEffects.Parameters(BusEffectType.Compressor)[BusEffects.CompressorRatio].Default, effect.Value(BusEffects.CompressorRatio));
            effect.SetValue(BusEffects.CompressorMakeup, 100f);
            Assert.AreEqual(24f, effect.Value(BusEffects.CompressorMakeup));
            Assert.AreEqual(-30f, effect.Value(BusEffects.CompressorThreshold));
        }

        [Test]
        public void EffectIds_StayUniqueWhenAnEffectIsDuplicated()
        {
            var bus = Asset<AudioBus>("SFX");
            var first = bus.AddEffect(BusEffectType.LowPass);
            bus.effects.Add(new BusEffect(BusEffectType.LowPass) { id = first.id });
            bus.effects.Add(new BusEffect(BusEffectType.Reverb));

            Assert.IsTrue(bus.AssignEffectIds());

            Assert.AreEqual(first, bus.FindEffect(first.id));
            Assert.AreNotEqual(first.id, bus.effects[1].id);
            Assert.AreNotEqual(0, bus.effects[2].id);
            Assert.AreNotEqual(bus.effects[1].id, bus.effects[2].id);
            Assert.IsFalse(bus.AssignEffectIds());
        }

        #endregion


        #region Private and Protected

        private EffectRack PlayOn(AudioBus bus, float seconds = 1f)
        {
            var sound = Event(ClipNode("tone", seconds, true));
            sound.bus = bus;
            Engine.Play(sound, null, Vector3.zero);
            Engine.Tick(0.02f);
            return Engine.Instances[0].Voices[0].Effects;
        }

        private EffectRack Ringing(AudioBus bus)
        {
            Engine.Simulate = false;
            bus.AddEffect(BusEffectType.Delay).SetValue(BusEffects.DelayFeedback, 0.5f);
            var sound = Event(ClipNode("hit", 0.1f));
            sound.bus = bus;
            var handle = Engine.Play(sound, null, Vector3.zero);
            Engine.Tick(0.02f);
            var rack = Engine.Instances[0].Voices[0].Effects;
            Engine.Stop(handle.Id, 0f);
            Engine.Tick(0.02f);
            Engine.Tick(0.02f);
            Assert.AreEqual(1, Engine.RingingVoices);
            return rack;
        }

        private static void Crunch(AudioBus bus)
        {
            var distortion = bus.AddEffect(BusEffectType.Distortion);
            distortion.SetValue(BusEffects.DistortionDrive, 12f / EffectDsp.MaxDriveDb);
            distortion.SetValue(BusEffects.DistortionOutput, 0f);
            distortion.SetValue(BusEffects.DistortionMix, 1f);
        }

        private static float DistortedPeak(EffectRack rack)
        {
            var data = Sine(1000f, 1f, 4800, 1);
            rack.Dsp.Process(data, 1);
            return Peak(data);
        }

        private EffectDsp Dsp(StageKind kind, float a, float b, float c)
        {
            var host = Track(new GameObject("Dsp") { hideFlags = HideFlags.HideAndDontSave });
            var dsp = host.AddComponent<EffectDsp>();
            dsp.Configure(Chain(kind, a, b, c));
            return dsp;
        }

        private static EffectChain Chain(StageKind kind, float a, float b, float c)
        {
            var chain = new EffectChain { Stages = 1 };
            chain.Kinds[0] = kind;
            chain.A[0] = a;
            chain.B[0] = b;
            chain.C[0] = c;
            chain.D[0] = 1f;
            return chain;
        }

        private float Gain(StageKind kind, float cutoff, float frequency)
        {
            var dsp = Dsp(kind, cutoff, 1f, 1f);
            var data = Sine(frequency, 1f, dsp.SampleRate, 1);
            dsp.Process(data, 1);
            return Rms(data, 0, data.Length / 2) / Mathf.Sqrt(0.5f);
        }

        private static float[] Sine(float frequency, float amplitude, int frames, int channels)
        {
            var data = new float[frames * channels];
            var rate = Mathf.Max(8000, AudioSettings.outputSampleRate);
            for (var i = 0; i < frames; i++)
            {
                for (var channel = 0; channel < channels; channel++)
                    data[i * channels + channel] = amplitude * Mathf.Sin(2f * Mathf.PI * frequency * i / rate);
            }
            return data;
        }

        private static float Rms(float[] data, int channel, int skipFrames = 0, int channels = 1)
        {
            var sum = 0.0;
            var count = 0;
            for (var i = skipFrames * channels + channel; i < data.Length; i += channels)
            {
                sum += data[i] * data[i];
                count++;
            }
            return count == 0 ? 0f : (float)System.Math.Sqrt(sum / count);
        }

        private static float Peak(float[] data)
        {
            var peak = 0f;
            foreach (var sample in data)
                peak = Mathf.Max(peak, Mathf.Abs(sample));
            return peak;
        }

        #endregion
    }
}

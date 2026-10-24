using NUnit.Framework;
using UnityEngine;

namespace Nekuzaky.Resonance.Tests
{
    public sealed class SyncTests : Fixture
    {
        [Test]
        public void Parameter_StartsAtItsDefaultAndIsClamped()
        {
            var parameter = Asset<SoundParameter>("Speed");
            parameter.min = 0f;
            parameter.max = 10f;
            parameter.defaultValue = 4f;
            var syncs = new SyncStore();

            Assert.AreEqual(4f, syncs.GetParameter(parameter, null));
            syncs.SetParameter(parameter, 50f, null, true);
            Assert.AreEqual(10f, syncs.GetParameter(parameter, null));
        }

        [Test]
        public void Parameter_SeeksAtItsSpeed()
        {
            var parameter = Asset<SoundParameter>("Intensity");
            parameter.max = 1f;
            parameter.seekSpeed = 0.5f;
            var syncs = new SyncStore();

            syncs.SetParameter(parameter, 1f, null, false);
            syncs.Tick(1f);

            Assert.AreEqual(0.5f, syncs.GetParameter(parameter, null), 0.0001f);
            syncs.Tick(1f);
            Assert.AreEqual(1f, syncs.GetParameter(parameter, null), 0.0001f);
        }

        [Test]
        public void PerEmitterParameter_IsIndependentPerGameObject()
        {
            var parameter = Asset<SoundParameter>("Rpm");
            parameter.max = 100f;
            parameter.scope = ParameterScope.PerEmitter;
            var car = Track(new GameObject("Car"));
            var truck = Track(new GameObject("Truck"));
            var syncs = new SyncStore();

            syncs.SetParameter(parameter, 80f, car, true);
            syncs.SetParameter(parameter, 20f, truck, true);

            Assert.AreEqual(80f, syncs.GetParameter(parameter, car));
            Assert.AreEqual(20f, syncs.GetParameter(parameter, truck));
            Assert.AreEqual(0f, syncs.GetParameter(parameter, null));
        }

        [Test]
        public void Switch_EmitterValueOverridesTheGlobalOne()
        {
            var group = Asset<SwitchGroup>("Surface");
            group.values = new System.Collections.Generic.List<string> { "Grass", "Stone", "Wood" };
            group.defaultIndex = 2;
            var player = Track(new GameObject("Player"));
            var syncs = new SyncStore();

            Assert.AreEqual("Wood", syncs.GetSwitch(group, player));
            syncs.SetSwitch(group, "Grass", null);
            Assert.AreEqual("Grass", syncs.GetSwitch(group, player));
            syncs.SetSwitch(group, "Stone", player);
            Assert.AreEqual("Stone", syncs.GetSwitch(group, player));
            Assert.AreEqual("Grass", syncs.ReaderFor(null).GetSwitch(group));
        }

        [Test]
        public void State_RaisesAnEventOnlyWhenItChanges()
        {
            var group = Asset<StateGroup>("Game");
            var syncs = new SyncStore();
            var changes = 0;
            syncs.StateChanged += (_, _) => changes++;

            syncs.SetState(group, "Combat");
            syncs.SetState(group, "Combat");
            syncs.SetState(group, "Explore");

            Assert.AreEqual(2, changes);
            Assert.AreEqual("Explore", syncs.GetState(group));
        }
    }

    public sealed class MixerTests : Fixture
    {
        [Test]
        public void BusGain_MultipliesUpTheHierarchy()
        {
            var master = Asset<AudioBus>("Master");
            master.volume = -6f;
            var sfx = Asset<AudioBus>("SFX");
            sfx.parent = master;
            sfx.volume = -6f;

            Assert.AreEqual(AudioMath.ToLinear(-12f), new BusMixer().Gain(sfx), 0.0001f);
        }

        [Test]
        public void Mute_SilencesTheBusAndItsChildren()
        {
            var master = Asset<AudioBus>("Master");
            var sfx = Asset<AudioBus>("SFX");
            sfx.parent = master;
            var mixer = new BusMixer();

            mixer.State(master).Muted = true;

            Assert.AreEqual(0f, mixer.Gain(sfx));
        }

        [Test]
        public void Solo_SilencesEveryOtherBus()
        {
            var music = Asset<AudioBus>("Music");
            var voice = Asset<AudioBus>("Voice");
            var mixer = new BusMixer();
            mixer.State(voice).Soloed = true;
            mixer.BeginFrame();

            Assert.AreEqual(0f, mixer.Gain(music));
            Assert.AreEqual(1f, mixer.Gain(voice), 0.0001f);
        }

        [Test]
        public void Ducking_LowersTheTargetWhileTheSourcePlays()
        {
            var music = Asset<AudioBus>("Music");
            var voice = Asset<AudioBus>("Voice");
            voice.ducking.Add(new DuckRule { target = music, amount = -10f, attack = 0.5f, release = 1f });
            var mixer = new BusMixer();

            for (var i = 0; i < 30; i++)
            {
                mixer.BeginFrame();
                mixer.CountVoice(voice, 0.5f);
                mixer.Tick(0.05f);
            }
            Assert.AreEqual(AudioMath.ToLinear(-10f), mixer.Gain(music), 0.001f);

            for (var i = 0; i < 30; i++)
            {
                mixer.BeginFrame();
                mixer.Tick(0.05f);
            }
            Assert.AreEqual(1f, mixer.Gain(music), 0.001f);
        }

        [Test]
        public void Snapshot_FadesInAndOut()
        {
            var music = Asset<AudioBus>("Music");
            var pause = Asset<MixSnapshot>("Pause");
            pause.fadeIn = 1f;
            pause.fadeOut = 1f;
            pause.offsets.Add(new BusOffset { bus = music, volume = -20f });
            var mixer = new BusMixer();

            mixer.Apply(pause);
            mixer.Tick(0.5f);
            Assert.AreEqual(-10f, mixer.SnapshotOffset(music), 0.001f);
            mixer.Tick(0.5f);
            Assert.AreEqual(-20f, mixer.SnapshotOffset(music), 0.001f);

            mixer.Remove(pause);
            mixer.Tick(1f);
            Assert.AreEqual(0f, mixer.SnapshotOffset(music), 0.001f);
            Assert.IsFalse(mixer.IsActive(pause));
        }
    }

    public sealed class AudioMathTests
    {
        [Test]
        public void Decibels_ConvertBothWays()
        {
            Assert.AreEqual(1f, AudioMath.ToLinear(0f), 0.0001f);
            Assert.AreEqual(0.5012f, AudioMath.ToLinear(-6f), 0.0001f);
            Assert.AreEqual(0f, AudioMath.ToLinear(-80f));
            Assert.AreEqual(-6f, AudioMath.ToDecibels(AudioMath.ToLinear(-6f)), 0.001f);
            Assert.AreEqual(2f, AudioMath.ToPitch(12f), 0.0001f);
        }

        [Test]
        public void BlendWeight_FadesOutsideTheRange()
        {
            var range = new Vector2(0.2f, 0.6f);

            Assert.AreEqual(1f, AudioMath.BlendWeight(0.4f, range, 0.2f));
            Assert.AreEqual(0.5f, AudioMath.BlendWeight(0.7f, range, 0.2f), 0.0001f);
            Assert.AreEqual(0f, AudioMath.BlendWeight(0.9f, range, 0.2f));
            Assert.AreEqual(0f, AudioMath.BlendWeight(0.1f, range, 0f));
        }

        [Test]
        public void NextBoundary_LandsOnTheNextBeat()
        {
            Assert.AreEqual(10.5, AudioMath.NextBoundary(10.0, 10.3, 0.5), 1e-9);
            Assert.AreEqual(10.5, AudioMath.NextBoundary(10.0, 10.5, 0.5), 1e-9);
            Assert.AreEqual(12.0, AudioMath.NextBoundary(10.0, 10.01, 2.0), 1e-9);
            Assert.AreEqual(10.0, AudioMath.NextBoundary(10.0, 9.0, 2.0), 1e-9);
        }
    }
}

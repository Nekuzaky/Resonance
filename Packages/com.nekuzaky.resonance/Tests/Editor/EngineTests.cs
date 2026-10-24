using NUnit.Framework;
using UnityEngine;

namespace Nekuzaky.Resonance.Tests
{
    public sealed class EngineTests : Fixture
    {
        [Test]
        public void Play_ReturnsAHandleThatPlaysUntilTheClipEnds()
        {
            var sound = Event(ClipNode("a", 0.5f));

            var handle = Engine.Play(sound, null, Vector3.zero);

            Assert.IsTrue(handle.IsValid);
            Assert.IsTrue(Engine.IsPlaying(handle.Id));
            Run(0.3f);
            Assert.IsTrue(Engine.IsPlaying(handle.Id));
            Run(0.4f);
            Assert.IsFalse(Engine.IsPlaying(handle.Id));
            Assert.AreEqual(0, Engine.Instances.Count);
        }

        [Test]
        public void ContinuousSequence_PlaysEveryClipBeforeEnding()
        {
            var sequence = Container<SequenceNode>(ClipNode("a", 0.3f), ClipNode("b", 0.3f));
            sequence.mode = SequenceMode.Continuous;
            var handle = Engine.Play(Event(sequence), null, Vector3.zero);

            Run(0.45f);
            Assert.IsTrue(Engine.IsPlaying(handle.Id));
            Run(0.3f);
            Assert.IsFalse(Engine.IsPlaying(handle.Id));
        }

        [Test]
        public void LoopingClip_PlaysUntilStoppedThenFadesOut()
        {
            var sound = Event(ClipNode("loop", 0.2f, true));
            sound.fadeOut = 0.5f;
            var handle = Engine.Play(sound, null, Vector3.zero);

            Run(2f);
            Assert.IsTrue(Engine.IsPlaying(handle.Id));

            Engine.Stop(handle.Id, -1f);
            Run(0.3f);
            Assert.IsTrue(Engine.IsPlaying(handle.Id), "Still fading out");
            Run(0.3f);
            Assert.IsFalse(Engine.IsPlaying(handle.Id));
        }

        [Test]
        public void MaxInstances_RejectNewKeepsTheFirstOnes()
        {
            var sound = Event(ClipNode("a", 5f));
            sound.maxInstances = 2;
            sound.limitBehavior = LimitBehavior.RejectNew;

            Assert.IsTrue(Engine.Play(sound, null, Vector3.zero).IsValid);
            Assert.IsTrue(Engine.Play(sound, null, Vector3.zero).IsValid);
            Assert.IsFalse(Engine.Play(sound, null, Vector3.zero).IsValid);
            Assert.AreEqual(PlayResult.RejectedLimit, Engine.Stats.LogAt(0).Result);
        }

        [Test]
        public void MaxInstances_StealOldestStopsTheFirstOne()
        {
            var sound = Event(ClipNode("a", 5f));
            sound.maxInstances = 1;
            var first = Engine.Play(sound, null, Vector3.zero);
            Run(0.1f);

            var second = Engine.Play(sound, null, Vector3.zero);
            Run(0.2f);

            Assert.IsTrue(second.IsValid);
            Assert.IsFalse(Engine.IsPlaying(first.Id));
            Assert.IsTrue(Engine.IsPlaying(second.Id));
            Assert.AreEqual(PlayResult.Stolen, Engine.Stats.LogAt(0).Result);
        }

        [Test]
        public void PerEmitterLimit_CountsEachEmitterSeparately()
        {
            var sound = Event(ClipNode("a", 5f));
            sound.maxInstances = 1;
            sound.limitScope = LimitScope.PerEmitter;
            sound.limitBehavior = LimitBehavior.RejectNew;
            var left = Track(new GameObject("Left"));
            var right = Track(new GameObject("Right"));

            Assert.IsTrue(Engine.Play(sound, left, Vector3.zero).IsValid);
            Assert.IsTrue(Engine.Play(sound, right, Vector3.zero).IsValid);
            Assert.IsFalse(Engine.Play(sound, left, Vector3.zero).IsValid);
        }

        [Test]
        public void Cooldown_BlocksPlaysThatComeTooSoon()
        {
            var sound = Event(ClipNode("a", 0.1f));
            sound.cooldown = 0.5f;

            Assert.IsTrue(Engine.Play(sound, null, Vector3.zero).IsValid);
            Assert.IsFalse(Engine.Play(sound, null, Vector3.zero).IsValid);
            Run(0.6f);
            Assert.IsTrue(Engine.Play(sound, null, Vector3.zero).IsValid);
        }

        [Test]
        public void VoiceBudget_HigherPriorityStealsFromLower()
        {
            Engine.MaxVoices = 2;
            var ambience = Event(ClipNode("wind", 10f), "Ambience");
            ambience.priority = 10;
            var dialogue = Event(ClipNode("line", 10f), "Dialogue");
            dialogue.priority = 200;

            var footstep = Event(ClipNode("step", 10f), "Footstep");
            footstep.priority = 5;

            var oldest = Engine.Play(ambience, null, Vector3.zero);
            Run(0.1f);
            Engine.Play(ambience, null, Vector3.zero);
            var line = Engine.Play(dialogue, null, Vector3.zero);
            var rejected = Engine.Play(footstep, null, Vector3.zero);

            Assert.IsTrue(line.IsValid);
            Assert.IsFalse(Engine.IsPlaying(oldest.Id), "The oldest of the lowest priority sounds is stolen first");
            Assert.IsFalse(rejected.IsValid, "Lower priority cannot steal from higher priority");
            Assert.AreEqual(PlayResult.RejectedVoices, Engine.Stats.LogAt(0).Result);
            Assert.AreEqual(1, Engine.CountPlaying(ambience, null));
        }

        [Test]
        public void DestroyedEmitter_StopsItsSounds()
        {
            var sound = Event(ClipNode("engine", 0.2f, true));
            sound.fadeOut = 0f;
            var car = new GameObject("Car");
            var handle = Engine.Play(sound, car, Vector3.zero);

            Run(0.1f);
            Object.DestroyImmediate(car);
            Run(0.1f);

            Assert.IsFalse(Engine.IsPlaying(handle.Id));
        }

        [Test]
        public void Emitter_PositionIsFollowed()
        {
            var sound = Event(ClipNode("engine", 0.2f, true));
            var car = Track(new GameObject("Car"));
            Engine.Play(sound, car, Vector3.zero);

            car.transform.position = new Vector3(3f, 0f, 4f);
            Run(0.05f);

            Assert.AreEqual(new Vector3(3f, 0f, 4f), Engine.Instances[0].Position);
        }

        [Test]
        public void Modulation_ChangesVolumeWithTheParameter()
        {
            var parameter = Asset<SoundParameter>("Distance");
            parameter.max = 1f;
            var sound = Event(ClipNode("a", 5f));
            sound.modulations.Add(new Modulation
            {
                parameter = parameter,
                target = ModulationTarget.Volume,
                curve = AnimationCurve.Linear(0f, 0f, 1f, -20f)
            });
            Engine.Play(sound, null, Vector3.zero);

            Engine.Syncs.SetParameter(parameter, 1f, null, true);
            Run(0.05f);

            Assert.AreEqual(AudioMath.ToLinear(-20f), Engine.Instances[0].Gain, 0.0001f);
        }

        [Test]
        public void EmptyEvent_IsRejectedAndLogged()
        {
            var sound = Event(Container<RandomNode>());

            Assert.IsFalse(Engine.Play(sound, null, Vector3.zero).IsValid);
            Assert.AreEqual(PlayResult.Empty, Engine.Stats.LogAt(0).Result);
        }

        [Test]
        public void VoicesAreReusedFromThePool()
        {
            var sound = Event(ClipNode("a", 0.05f));
            for (var i = 0; i < 20; i++)
            {
                Engine.Play(sound, null, Vector3.zero);
                Run(0.1f);
            }

            Assert.AreEqual(1, Engine.Stats.PooledVoices);
        }
    }

    public sealed class MusicTests : Fixture
    {
        [Test]
        public void Track_ComputesSegmentLengthsFromBars()
        {
            var track = Asset<MusicTrack>("Score");
            track.bpm = 120f;
            track.beatsPerBar = 4;
            var segment = new MusicSegment { name = "Explore", bars = 4, clip = Clip("explore", 10f) };
            track.segments.Add(segment);

            Assert.AreEqual(0.5, track.BeatLength, 1e-9);
            Assert.AreEqual(2.0, track.BarLength, 1e-9);
            Assert.AreEqual(8.0, track.LengthOf(segment), 1e-9);
            segment.bars = 0;
            Assert.AreEqual(10.0, track.LengthOf(segment), 0.001);
        }

        [Test]
        public void Track_MapsStatesToSegments()
        {
            var track = Asset<MusicTrack>("Score");
            track.segments.Add(new MusicSegment { name = "Explore" });
            track.segments.Add(new MusicSegment { name = "Combat" });
            track.links.Add(new MusicStateLink { state = "Fight", segment = "Combat" });
            track.firstSegment = "Explore";

            Assert.AreEqual("Combat", track.ForState("Fight").name);
            Assert.IsNull(track.ForState("Calm"));
            Assert.AreEqual("Explore", track.First.name);
        }

        [Test]
        public void StateChange_SchedulesTheLinkedSegment()
        {
            var state = Asset<StateGroup>("Game");
            var track = Asset<MusicTrack>("Score");
            track.stateGroup = state;
            track.segments.Add(new MusicSegment { name = "Explore", clip = Clip("explore", 4f) });
            track.segments.Add(new MusicSegment { name = "Combat", clip = Clip("combat", 4f) });
            track.links.Add(new MusicStateLink { state = "Fight", segment = "Combat" });

            Engine.Music.Play(track, AudioSettings.dspTime);
            Assert.AreEqual("Explore", Engine.Music.Current.name);

            Engine.Syncs.SetState(state, "Fight");

            Assert.AreEqual("Combat", Engine.Music.Pending.name);
        }
    }
}

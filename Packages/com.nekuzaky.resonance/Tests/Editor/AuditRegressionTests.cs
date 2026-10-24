using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Nekuzaky.Resonance.Tests
{
    public sealed class AuditRegressionTests : Fixture
    {
        [Test]
        public void PlayingFromAMarkerHandler_IsSafe()
        {
            var swing = ClipNode("swing", 1f);
            swing.markers.Add(new ClipMarker { name = "hit", time = 0.3f });
            var impact = Event(ClipNode("impact", 0.2f), "Impact");
            Engine.MarkerReached += _ => Engine.Play(impact, null, Vector3.zero);

            Engine.Play(Event(swing), null, Vector3.zero);
            Assert.DoesNotThrow(() => Run(0.5f));

            Assert.AreEqual(1, Engine.CountPlaying(impact, null));
        }

        [Test]
        public void PlayingFromAnEndHandler_UnderAFullVoiceBudget_KeepsInstancesConsistent()
        {
            Engine.MaxVoices = 2;
            var shortSound = Event(ClipNode("short", 0.1f), "Short");
            var loop = Event(ClipNode("loop", 1f, true), "Loop");
            Engine.SoundEnded += (_, _) => Engine.Play(loop, null, Vector3.zero);
            Engine.Play(loop, null, Vector3.zero);
            Engine.Play(shortSound, null, Vector3.zero);

            Assert.DoesNotThrow(() => Run(0.5f));

            var ids = new HashSet<int>();
            foreach (var instance in Engine.Instances)
            {
                Assert.AreNotEqual(InstanceState.Done, instance.State);
                Assert.IsTrue(ids.Add(instance.Id), "An instance was reused while still in the list");
            }
        }

        [Test]
        public void StolenSounds_ReportThatTheyEnded()
        {
            Engine.MaxVoices = 1;
            var low = Event(ClipNode("low", 5f), "Low");
            low.priority = 10;
            var high = Event(ClipNode("high", 5f), "High");
            high.priority = 200;
            var ended = new List<SoundHandle>();
            Engine.SoundEnded += (handle, _) => ended.Add(handle);

            var stolen = Engine.Play(low, null, Vector3.zero);
            Engine.Play(high, null, Vector3.zero);

            CollectionAssert.AreEqual(new[] { stolen }, ended);
        }

        [Test]
        public void FadeIn_StartsAfterTheDelay()
        {
            var sound = Event(ClipNode("a", 5f));
            sound.delay = 1f;
            sound.fadeIn = 0.5f;
            Engine.Play(sound, null, Vector3.zero);

            Run(1.1f);

            Assert.That(Engine.Instances[0].Fade, Is.InRange(0.05f, 0.5f));
        }

        [Test]
        public void DialogueSequence_SendsEveryLine()
        {
            var first = new DialogueNode { name = "A", speaker = "Guard" };
            first.lines.Add(new DialogueLine { language = "en", clip = Clip("a", 0.3f), subtitle = "Halt." });
            var second = new DialogueNode { name = "B", speaker = "Guard" };
            second.lines.Add(new DialogueLine { language = "en", clip = Clip("b", 0.3f), subtitle = "Who goes there?" });
            var sequence = Container<SequenceNode>(first, second);
            sequence.mode = SequenceMode.Continuous;
            var texts = new List<string>();
            Engine.SubtitleStarted += subtitle => texts.Add(subtitle.Text);

            Engine.Play(Event(sequence), null, Vector3.zero);
            Run(0.8f);

            CollectionAssert.AreEqual(new[] { "Halt.", "Who goes there?" }, texts);
        }

        [Test]
        public void StopAllOnADestroyedObject_LeavesOtherSoundsAlone()
        {
            var loop = Event(ClipNode("loop", 1f, true));
            var enemy = new GameObject("Enemy");
            var other = Engine.Play(loop, null, Vector3.zero);
            Object.DestroyImmediate(enemy);

            Engine.StopAll(enemy, 0f);
            Run(0.1f);

            Assert.IsTrue(Engine.IsPlaying(other.Id));
        }

        [Test]
        public void Cooldown_BelongsToEachEngine()
        {
            var sound = Event(ClipNode("a", 0.1f));
            sound.cooldown = 10f;
            var otherHost = Track(new GameObject("Other Host") { hideFlags = HideFlags.HideAndDontSave });
            var other = new ResonanceEngine(otherHost.transform, 9) { Simulate = true, Preview = true };

            Engine.Play(sound, null, Vector3.zero);

            Assert.IsTrue(other.Play(sound, null, Vector3.zero).IsValid);
            other.Shutdown();
        }

        [Test]
        public void VirtualVoices_DoNotDuck()
        {
            var music = Asset<AudioBus>("Music");
            var voice = Asset<AudioBus>("Voice");
            voice.ducking.Add(new DuckRule { target = music, amount = -10f, attack = 0f, release = 0f });
            var line = Event(ClipNode("line", 5f));
            line.bus = voice;
            Engine.Play(line, null, Vector3.zero);

            Run(0.2f);

            Assert.AreEqual(1f, Engine.Mixer.Gain(music), 0.001f);
        }
    }

    public sealed class MusicRegressionTests : Fixture
    {
        private MusicTrack Score(out StateGroup state)
        {
            state = Asset<StateGroup>("Game");
            var track = Asset<MusicTrack>("Score");
            track.stateGroup = state;
            track.segments.Add(new MusicSegment { name = "Explore", clip = Clip("explore", 4f) });
            track.segments.Add(new MusicSegment { name = "Rise", clip = Clip("rise", 1f) });
            track.segments.Add(new MusicSegment { name = "Combat", clip = Clip("combat", 4f) });
            track.links.Add(new MusicStateLink { state = "Explore", segment = "Explore" });
            track.links.Add(new MusicStateLink { state = "Fight", segment = "Combat", bridge = "Rise" });
            return track;
        }

        [Test]
        public void ReturningBeforeTheBoundary_KeepsTheCurrentSegment()
        {
            var track = Score(out var state);
            Engine.Music.Play(track, AudioSettings.dspTime);

            Engine.Syncs.SetState(state, "Fight");
            Assert.IsNotNull(Engine.Music.Pending);
            Engine.Syncs.SetState(state, "Explore");

            Assert.IsNull(Engine.Music.Pending);
            Assert.AreEqual("Explore", Engine.Music.Current.name);
        }

        [Test]
        public void Stopping_ForgetsAPlannedBridge()
        {
            var track = Score(out var state);
            Engine.Music.Play(track, AudioSettings.dspTime);
            Engine.Syncs.SetState(state, "Fight");
            Assert.IsNotNull(Engine.Music.AfterBridge);

            Engine.Music.Stop(0f);
            Engine.Music.Play(track, AudioSettings.dspTime);

            Assert.IsNull(Engine.Music.AfterBridge);
        }
    }
}

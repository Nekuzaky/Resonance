using NUnit.Framework;
using UnityEngine;

namespace Nekuzaky.Resonance.Tests
{
    public sealed class VoiceBudgetTests : Fixture
    {
        [Test]
        public void RejectedPlay_StealsNothing()
        {
            Engine.MaxVoices = 2;
            var line = Event(ClipNode("line", 10f), "Dialogue");
            line.priority = 200;
            var wind = Event(ClipNode("wind", 10f), "Ambience");
            wind.priority = 10;
            var chord = Event(Container<LayerNode>(ClipNode("a", 10f), ClipNode("b", 10f)), "Two Layers");
            chord.priority = 100;

            Engine.Play(line, null, Vector3.zero);
            var ambience = Engine.Play(wind, null, Vector3.zero);
            var rejected = Engine.Play(chord, null, Vector3.zero);

            Assert.IsFalse(rejected.IsValid);
            Assert.AreEqual(PlayResult.RejectedVoices, Engine.Stats.LogAt(0).Result);
            Assert.IsTrue(Engine.IsPlaying(ambience.Id), "A sound must not be stolen for a play that is rejected anyway");
        }

        [Test]
        public void SoundTooBigForTheBudget_StealsNothing()
        {
            Engine.MaxVoices = 1;
            var wind = Event(ClipNode("wind", 10f), "Ambience");
            wind.priority = 10;
            var chord = Event(Container<LayerNode>(ClipNode("a", 10f), ClipNode("b", 10f)), "Two Layers");

            var ambience = Engine.Play(wind, null, Vector3.zero);

            Assert.IsFalse(Engine.Play(chord, null, Vector3.zero).IsValid);
            Assert.IsTrue(Engine.IsPlaying(ambience.Id));
        }

        [Test]
        public void InstanceLimitSteal_ReusesTheStolenVoiceFirst()
        {
            Engine.MaxVoices = 3;
            var wind = Event(ClipNode("wind", 10f), "Ambience");
            wind.priority = 10;
            var shot = Event(ClipNode("shot", 10f), "Shot");
            shot.maxInstances = 2;

            var ambience = Engine.Play(wind, null, Vector3.zero);
            Engine.Play(shot, null, Vector3.zero);
            Engine.Play(shot, null, Vector3.zero);
            Assert.IsTrue(Engine.Play(shot, null, Vector3.zero).IsValid);

            Assert.IsTrue(Engine.IsPlaying(ambience.Id), "The voice of the stolen shot must be reclaimed before stealing another sound");
            Assert.AreEqual(2, Engine.CountPlaying(shot, null));
        }
    }

    public sealed class VirtualVoiceTests : Fixture
    {
        [Test]
        public void ContinuousSequence_KeepsItsPlaceAcrossClips()
        {
            var sequence = Container<SequenceNode>(ClipNode("a", 0.5f), ClipNode("b", 0.5f));
            sequence.mode = SequenceMode.Continuous;
            sequence.loop = true;
            Engine.Play(Event(sequence), null, Vector3.zero);

            Engine.Tick(0.3f);
            Engine.Tick(0.3f);
            Engine.Tick(0.3f);

            var voice = Engine.Instances[0].Voices[0];
            Assert.AreEqual(1, voice.ClipIndex);
            Assert.AreEqual(0.4f, voice.VirtualTime, 0.001f, "Time past the end of a clip carries into the next one");
        }

        [Test]
        public void VirtualTime_RunsAtThePitchTheSourcePlays()
        {
            var node = ClipNode("loop", 10f, true);
            node.pitch = 24f;
            var sound = Event(node);
            sound.pitch = 24f;
            Engine.Play(sound, null, Vector3.zero);

            Engine.Tick(0.1f);

            var voice = Engine.Instances[0].Voices[0];
            Assert.AreEqual(4f, voice.Source.pitch, 0.001f);
            Assert.AreEqual(0.4f, voice.VirtualTime, 0.001f);
        }
    }
}

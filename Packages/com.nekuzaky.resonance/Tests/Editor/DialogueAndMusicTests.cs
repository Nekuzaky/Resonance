using System.Collections.Generic;
using Nekuzaky.Resonance.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Nekuzaky.Resonance.Tests
{
    public sealed class DialogueTests : Fixture
    {
        private DialogueNode Line()
        {
            var node = new DialogueNode { name = "Greeting", speaker = "Guard", fallbackLanguage = "en" };
            node.lines.Add(new DialogueLine { language = "en", clip = Clip("hello", 0.3f), subtitle = "Hello there." });
            node.lines.Add(new DialogueLine { language = "fr", clip = Clip("bonjour", 0.3f), subtitle = "Bonjour." });
            return node;
        }

        [Test]
        public void Language_PicksTheMatchingLineOrTheFallback()
        {
            var node = Line();

            Assert.AreEqual("bonjour", node.LineFor("FR").clip.name);
            Assert.AreEqual("hello", node.LineFor("de").clip.name);
        }

        [Test]
        public void Engine_PlaysTheLineInTheCurrentLanguage()
        {
            Engine.Language = "fr";

            Engine.Play(Event(Line()), null, Vector3.zero);

            Assert.AreEqual("bonjour", Engine.Instances[0].Voices[0].Clip.Clip.name);
        }

        [Test]
        public void Subtitles_StartWithTheLineAndEndWithIt()
        {
            var started = new List<Subtitle>();
            var ended = new List<Subtitle>();
            Engine.SubtitleStarted += started.Add;
            Engine.SubtitleEnded += ended.Add;
            var guard = Track(new GameObject("Guard"));

            var handle = Engine.Play(Event(Line()), guard, Vector3.zero);
            Assert.AreEqual(1, started.Count);
            Assert.AreEqual("Hello there.", started[0].Text);
            Assert.AreEqual("Guard", started[0].Speaker);
            Assert.AreEqual(guard, started[0].Emitter);
            Assert.AreEqual(handle, started[0].Handle);

            Run(0.5f);
            Assert.AreEqual(1, ended.Count);
            Assert.AreEqual(handle, ended[0].Handle);
        }

        [Test]
        public void Banks_LoadDialogueClips()
        {
            var bank = Asset<SoundBank>("Voices");
            bank.events.Add(Event(Line()));
            var clips = new HashSet<AudioClip>();

            bank.CollectClips(clips);

            Assert.AreEqual(2, clips.Count);
        }
    }

    public sealed class MusicBridgeTests : Fixture
    {
        [Test]
        public void StateWithABridge_PlaysTheBridgeThenTheTarget()
        {
            var state = Asset<StateGroup>("Game");
            var track = Asset<MusicTrack>("Score");
            track.stateGroup = state;
            track.segments.Add(new MusicSegment { name = "Explore", clip = Clip("explore", 4f) });
            track.segments.Add(new MusicSegment { name = "Rise", clip = Clip("rise", 2f) });
            track.segments.Add(new MusicSegment { name = "Combat", clip = Clip("combat", 4f) });
            track.links.Add(new MusicStateLink { state = "Fight", segment = "Combat", bridge = "Rise" });

            Engine.Music.Play(track, AudioSettings.dspTime);
            Engine.Syncs.SetState(state, "Fight");

            Assert.AreEqual("Rise", Engine.Music.Pending.name);
            Assert.AreEqual("Combat", Engine.Music.AfterBridge.name);
        }
    }

    public sealed class PlatformTests : Fixture
    {
        [Test]
        public void Binaural_NeedsASpatializerPlugin()
        {
            var sound = Event(ClipNode("a", 5f));
            sound.spatial.binaural = true;
            Engine.Preview = false;

            Engine.Play(sound, null, Vector3.one);

            Assert.AreEqual(ResonanceEngine.HasSpatializer, Engine.Instances[0].Voices[0].Source.spatialize);
        }

        [Test]
        public void MobileQuality_IsLowerForEffectsThanForMusic()
        {
            Assert.Less(ImportAudit.SuggestedMobileQuality(false), ImportAudit.SuggestedMobileQuality(true));
            CollectionAssert.AreEquivalent(new[] { "Android", "iOS" }, ImportAudit.MobilePlatforms);
        }
    }
}

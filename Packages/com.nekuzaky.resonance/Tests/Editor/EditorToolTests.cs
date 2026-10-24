using System.Collections.Generic;
using Nekuzaky.Resonance.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Nekuzaky.Resonance.Tests
{
    public sealed class BulkImportTests : Fixture
    {
        [Test]
        public void TrailingNumbers_AreVariationsOfOneEvent()
        {
            BulkImport.Parse("door_open_03", null, out var name, out var variant);

            Assert.AreEqual("door_open", name);
            Assert.AreEqual(string.Empty, variant);
        }

        [Test]
        public void SwitchValues_AreReadFromTheName()
        {
            var surface = Asset<SwitchGroup>("Surface");
            surface.values = new List<string> { "Grass", "Stone" };

            BulkImport.Parse("Footstep-stone (2)", surface, out var name, out var variant);

            Assert.AreEqual("Footstep", name);
            Assert.AreEqual("Stone", variant);
        }

        [Test]
        public void Plan_GroupsClipsIntoEventsAndVariants()
        {
            var surface = Asset<SwitchGroup>("Surface");
            surface.values = new List<string> { "Grass", "Stone" };
            var clips = new[]
            {
                Clip("step_grass_01"), Clip("step_grass_02"), Clip("step_stone_01"), Clip("step_stone_02"), Clip("step_03"),
                Clip("door_open_01"), Clip("door_open_02")
            };

            var plan = BulkImport.Plan(clips, surface);

            Assert.AreEqual(2, plan.Count);
            var steps = plan.Find(group => group.EventName == "step");
            Assert.AreEqual(3, steps.Variants.Count);
            var tree = (SwitchNode)BulkImport.BuildTree(steps, surface);
            Assert.AreEqual(surface, tree.group);
            Assert.AreEqual(string.Empty, tree.children[tree.fallback].switchValue);
            var door = BulkImport.BuildTree(plan.Find(group => group.EventName == "door_open"), surface);
            Assert.IsInstanceOf<RandomNode>(door);
        }
    }

    public sealed class AuditTests : Fixture
    {
        [Test]
        public void BrokenEvents_AreReported()
        {
            var sound = Event(Container<SwitchNode>(new ClipNode { name = "Missing" }, Container<BlendNode>()));
            var problems = new List<Problem>();

            ProjectAudit.CheckEvent(sound, null, problems);

            var messages = string.Join("\n", problems.ConvertAll(problem => problem.Message));
            StringAssert.Contains("has no switch group", messages);
            StringAssert.Contains("has no audio clip", messages);
            StringAssert.Contains("has no parameter", messages);
            StringAssert.Contains("has no bus", messages);
        }

        [Test]
        public void MusicLinksToMissingSegments_AreReported()
        {
            var track = Asset<MusicTrack>("Score");
            track.segments.Add(new MusicSegment { name = "Explore", clip = Clip("explore"), next = "Outro" });
            track.links.Add(new MusicStateLink { state = "Fight", segment = "Combat" });
            var problems = new List<Problem>();

            ProjectAudit.CheckTrack(track, problems);

            Assert.AreEqual(2, problems.Count);
        }

        [Test]
        public void BusLoops_AreReported()
        {
            var a = Asset<AudioBus>("A");
            var b = Asset<AudioBus>("B");
            a.parent = b;
            b.parent = a;
            var problems = new List<Problem>();

            ProjectAudit.CheckBus(a, problems);

            Assert.AreEqual(Severity.Error, problems[0].Severity);
        }

        [Test]
        public void ImportSuggestions_FollowTheClipLength()
        {
            Assert.AreEqual(AudioClipLoadType.DecompressOnLoad, ImportAudit.SuggestedLoad(0.4f, false));
            Assert.AreEqual(AudioClipLoadType.CompressedInMemory, ImportAudit.SuggestedLoad(8f, false));
            Assert.AreEqual(AudioClipLoadType.Streaming, ImportAudit.SuggestedLoad(90f, false));
            Assert.AreEqual(AudioClipLoadType.CompressedInMemory, ImportAudit.SuggestedLoad(90f, true));
        }
    }

    public sealed class ProfilerHistoryTests : Fixture
    {
        [Test]
        public void History_KeepsTheLastSixtySecondsInOrder()
        {
            var history = new ProfilerHistory();
            var sound = Event(ClipNode("loop", 1f, true));
            Engine.Play(sound, null, Vector3.zero);
            Engine.Tick(0.01f);

            for (var i = 0; i < ProfilerHistory.Capacity + 25; i++)
                history.Record(Engine, i * 0.1);

            Assert.AreEqual(ProfilerHistory.Capacity, history.Count);
            Assert.AreEqual(25 * 0.1, history[0].Time, 1e-6);
            Assert.AreEqual((ProfilerHistory.Capacity + 24) * 0.1, history[history.Count - 1].Time, 1e-6);
            Assert.AreEqual(1, history[0].Sounds.Count);
            Assert.AreSame(sound, history[0].Sounds[0].Event);
        }
    }
}

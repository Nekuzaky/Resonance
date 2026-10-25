using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Nekuzaky.Resonance.Tests
{
    public sealed class BankLifetimeTests : Fixture
    {
        [Test]
        public void UnloadingABankWhileItPlays_WaitsForTheSoundToEnd()
        {
            var node = ClipNode("roar", 0.5f);
            var sound = Event(node);
            var bank = Asset<SoundBank>("Boss");
            bank.events.Add(sound);
            var unloaded = new List<AudioClip>();
            Engine.Memory.UnloadData = unloaded.Add;

            Engine.Memory.Load(bank);
            Engine.Play(sound, null, Vector3.zero);
            Engine.Memory.Unload(bank);

            Assert.IsFalse(Engine.Memory.IsLoaded(bank));
            Assert.AreEqual(0, unloaded.Count, "The clip is still playing");
            Run(0.6f);
            CollectionAssert.AreEqual(new[] { node.clip }, unloaded);
        }

        [Test]
        public void ReloadingBeforeTheSoundEnds_KeepsTheClip()
        {
            var node = ClipNode("roar", 0.5f);
            var sound = Event(node);
            var bank = Asset<SoundBank>("Boss");
            bank.events.Add(sound);
            var unloaded = new List<AudioClip>();
            Engine.Memory.UnloadData = unloaded.Add;

            Engine.Memory.Load(bank);
            Engine.Play(sound, null, Vector3.zero);
            Engine.Memory.Unload(bank);
            Engine.Memory.Load(bank);
            Run(0.6f);

            Assert.AreEqual(0, unloaded.Count);
            Assert.AreEqual(1, Engine.Memory.Clips[node.clip]);
        }

        [Test]
        public void EditingABankWhileLoaded_ReleasesWhatWasLoaded()
        {
            var node = ClipNode("roar", 0.5f);
            var bank = Asset<SoundBank>("Boss");
            bank.events.Add(Event(node));
            var unloaded = new List<AudioClip>();
            Engine.Memory.UnloadData = unloaded.Add;

            Engine.Memory.Load(bank);
            bank.events.Clear();
            Engine.Memory.Unload(bank);

            Assert.AreEqual(0, Engine.Memory.Clips.Count);
            CollectionAssert.AreEqual(new[] { node.clip }, unloaded);
        }
    }
}

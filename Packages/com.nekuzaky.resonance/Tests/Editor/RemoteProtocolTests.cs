using NUnit.Framework;
using UnityEngine;

namespace Nekuzaky.Resonance.Tests
{
    public sealed class RemoteProtocolTests : Fixture
    {
        [Test]
        public void TruncatedFrame_IsRejectedAndKeepsTheLastGoodOne()
        {
            var sound = Event(ClipNode("loop", 1f, true), "Engine Loop");
            Engine.Play(sound, Track(new GameObject("Car")), Vector3.zero);
            Engine.Tick(0.05f);
            var sent = new RemoteFrame();
            sent.Capture(Engine);
            var data = sent.Write();
            var received = new RemoteFrame();
            Assert.IsTrue(received.Read(data));

            for (var length = 1; length < data.Length; length++)
            {
                var truncated = new byte[length];
                System.Array.Copy(data, truncated, length);
                Assert.IsFalse(received.Read(truncated), $"{length} of {data.Length} bytes");
            }

            Assert.AreEqual(1, received.Sounds.Count);
            Assert.AreEqual("Engine Loop", received.Sounds[0].Event);
            Assert.AreEqual(sent.Platform, received.Platform);
        }

        [Test]
        public void GarbageFrame_IsRejected()
        {
            var data = new byte[64];
            for (var i = 0; i < data.Length; i++)
                data[i] = 0xFF;
            data[0] = RemoteFrame.Version;

            Assert.IsFalse(new RemoteFrame().Read(data));
        }
    }
}

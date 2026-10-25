using Nekuzaky.Resonance.Editor;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools.Constraints;
using Is = UnityEngine.TestTools.Constraints.Is;

namespace Nekuzaky.Resonance.Tests
{
    public sealed class ProfilerPerformanceTests : Fixture
    {
        private const int Sounds = 100;

        [TearDown]
        public void RestoreEngine() => Resonance.Override(null);

        [Test]
        public void HistoryRecord_AllocatesNothingOnceWarmedUp()
        {
            PlayMany();
            var history = new ProfilerHistory();
            for (var i = 0; i < ProfilerHistory.Capacity + 5; i++)
                history.Record(Engine, i * 0.1);

            Assert.That(() => history.Record(Engine, 100.0), Is.Not.AllocatingGCMemory());
            Assert.AreEqual(Sounds, history[history.Count - 1].Sounds.Count);
        }

        [Test]
        public void ProfilerRefresh_StaysCheapWithAHundredSounds()
        {
            PlayMany();
            Resonance.Override(Engine);
            var view = new ProfilerView();
            for (var i = 0; i < 5; i++)
            {
                view.Sample();
                view.Refresh();
            }

            const int refreshes = 50;
            var started = System.Diagnostics.Stopwatch.GetTimestamp();
            for (var i = 0; i < refreshes; i++)
            {
                Engine.Tick(0.1f);
                view.Sample();
                view.Refresh();
            }
            var milliseconds = (System.Diagnostics.Stopwatch.GetTimestamp() - started) * 1000.0 / System.Diagnostics.Stopwatch.Frequency / refreshes;

            Assert.AreEqual(Sounds, Engine.Instances.Count);
            Debug.Log($"[Scale] Profiler refresh with {Sounds} sounds: {milliseconds:0.000} ms per refresh");
            Assert.Less(milliseconds, 1.0);
        }

        private void PlayMany()
        {
            Engine.MaxVoices = 512;
            var bus = Asset<AudioBus>("SFX");
            var sound = Event(ClipNode("loop", 4f, true), "Engine Loop");
            sound.bus = bus;
            for (var i = 0; i < Sounds; i++)
                Engine.Play(sound, Track(new GameObject($"Emitter {i}")), Vector3.zero);
            Engine.Tick(0.02f);
        }
    }
}

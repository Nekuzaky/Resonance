using NUnit.Framework;
using UnityEngine;
using UnityEngine.Playables;

namespace Nekuzaky.Resonance.Timeline.Tests
{
    public sealed class TimelineTests
    {
        private GameObject _host;
        private ResonanceEngine _engine;
        private SoundEvent _sound;
        private StateGroup _group;

        [SetUp]
        public void CreateEngine()
        {
            _host = new GameObject("Timeline Test Host") { hideFlags = HideFlags.HideAndDontSave };
            _engine = new ResonanceEngine(_host.transform, 3) { Simulate = true, Preview = true };
            Resonance.Override(_engine);
            _sound = ScriptableObject.CreateInstance<SoundEvent>();
            _sound.root = new ClipNode { name = "loop", clip = AudioClip.Create("loop", 8000, 1, 8000, false), loop = true };
            _sound.fadeOut = 0f;
            _group = ScriptableObject.CreateInstance<StateGroup>();
            _group.states.Add("Cutscene");
        }

        [TearDown]
        public void Destroy()
        {
            Resonance.Override(null);
            _engine.Shutdown();
            Object.DestroyImmediate(((ClipNode)_sound.root).clip);
            Object.DestroyImmediate(_sound);
            Object.DestroyImmediate(_group);
            Object.DestroyImmediate(_host);
        }

        [Test]
        public void SoundClip_PlaysForTheLengthOfTheClip()
        {
            var behaviour = new SoundEventBehaviour { Sound = _sound, StopAtEnd = true, StopFade = 0f };

            behaviour.Begin(null);
            var handle = behaviour.Handle;
            _engine.Tick(0.1f);
            Assert.IsTrue(_engine.IsPlaying(handle.Id));

            behaviour.End();
            _engine.Tick(0.1f);
            Assert.IsFalse(_engine.IsPlaying(handle.Id));
        }

        [Test]
        public void SoundClip_CanLetTheSoundFinishOnItsOwn()
        {
            var behaviour = new SoundEventBehaviour { Sound = _sound, StopAtEnd = false };

            behaviour.Begin(null);
            var handle = behaviour.Handle;
            behaviour.End();
            _engine.Tick(0.1f);

            Assert.IsTrue(_engine.IsPlaying(handle.Id));
        }

        [Test]
        public void StateClip_SetsAndRestoresTheState()
        {
            Resonance.SetState(_group, "None");
            var behaviour = new GameStateBehaviour { Group = _group, State = "Cutscene", RestoreAtEnd = true };

            behaviour.Begin();
            Assert.AreEqual("Cutscene", Resonance.GetState(_group));
            behaviour.End();
            Assert.AreEqual("None", Resonance.GetState(_group));
        }

        [Test]
        public void Clips_CopyTheirSettingsIntoThePlayable()
        {
            var graph = PlayableGraph.Create();
            var clip = ScriptableObject.CreateInstance<SoundEventClip>();
            clip.sound = _sound;
            clip.stopAtEnd = false;

            var playable = (ScriptPlayable<SoundEventBehaviour>)clip.CreatePlayable(graph, null);
            var behaviour = playable.GetBehaviour();

            Assert.AreSame(_sound, behaviour.Sound);
            Assert.IsFalse(behaviour.StopAtEnd);
            graph.Destroy();
            Object.DestroyImmediate(clip);
        }
    }
}

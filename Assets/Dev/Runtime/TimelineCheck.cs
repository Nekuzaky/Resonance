using System.Collections.Generic;
using Nekuzaky.Resonance.Timeline;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Nekuzaky.Resonance.Dev
{
    [AddComponentMenu("")]
    internal sealed class TimelineCheck : MonoBehaviour
    {
        #region Private and Protected

        private SoundEvent _sound;
        private StateGroup _group;
        private float _start;
        private int _step;

        private TimelineAsset Build()
        {
            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            var sounds = timeline.CreateTrack<ResonanceTrack>(null, "Sounds");
            var soundClip = sounds.CreateClip<SoundEventClip>();
            soundClip.start = 0.5;
            soundClip.duration = 1.0;
            ((SoundEventClip)soundClip.asset).sound = _sound;
            var states = timeline.CreateTrack<ResonanceTrack>(null, "States");
            var stateClip = states.CreateClip<GameStateClip>();
            stateClip.start = 0.5;
            stateClip.duration = 1.0;
            var state = (GameStateClip)stateClip.asset;
            state.group = _group;
            state.state = "Cutscene";
            state.restoreAtEnd = true;
            return timeline;
        }

        private void Finish()
        {
            var engine = Resonance.Engine;
            Smoke.Check(engine.CountPlaying(_sound, null) == 0, "the timeline stopped its sound when the clip ended");
            Smoke.Check(Resonance.GetState(_group) == "Gameplay", "the timeline restored the previous state");
            Smoke.Pending--;
        }

        #endregion


        #region Unity Callbacks

        private void Start()
        {
            Smoke.Pending++;
            _start = Time.time;
            _sound = Smoke.Event(Smoke.Tone("Timeline Tone", 660f, 3f), -24f, false);
            _group = ScriptableObject.CreateInstance<StateGroup>();
            _group.name = "Cutscene Mode";
            _group.states = new List<string> { "Gameplay", "Cutscene" };
            var timeline = Build();
            var director = gameObject.AddComponent<PlayableDirector>();
            director.extrapolationMode = DirectorWrapMode.None;
            director.playableAsset = timeline;
            director.SetGenericBinding(timeline.GetOutputTrack(0), gameObject);
            director.Play();
        }

        private void Update()
        {
            var elapsed = Time.time - _start;
            if (_step == 0 && elapsed >= 1f)
            {
                Smoke.Check(Resonance.Engine.CountPlaying(_sound, null) == 1, "a PlayableDirector played the timeline sound");
                Smoke.Check(Resonance.GetState(_group) == "Cutscene", "the timeline set the Cutscene state");
                _step = 1;
            }
            else if (_step == 1 && elapsed >= 2.5f)
            {
                Finish();
                _step = 2;
            }
        }

        #endregion
    }
}

using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Nekuzaky.Resonance.Timeline
{
    public sealed class SoundEventBehaviour : PlayableBehaviour
    {
        #region Public

        public SoundEvent Sound;
        public bool StopAtEnd;
        public float StopFade;

        public SoundHandle Handle => _handle;

        public void Begin(GameObject emitter) => _handle = Resonance.Play(Sound, emitter);

        public void End()
        {
            if (StopAtEnd)
                _handle.Stop(StopFade);
            _handle = SoundHandle.None;
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (Application.isPlaying && info.evaluationType == FrameData.EvaluationType.Playback)
                Begin(info.output.GetUserData() as GameObject);
        }

        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (_handle.IsValid)
                End();
        }

        #endregion


        #region Private and Protected

        private SoundHandle _handle;

        #endregion
    }

    public sealed class GameStateBehaviour : PlayableBehaviour
    {
        #region Public

        public StateGroup Group;
        public string State;
        public bool RestoreAtEnd;

        public void Begin()
        {
            if (Group == null)
                return;
            _previous = Resonance.GetState(Group);
            _active = true;
            Resonance.SetState(Group, State);
        }

        public void End()
        {
            if (_active && RestoreAtEnd && Group != null)
                Resonance.SetState(Group, _previous);
            _active = false;
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            if (Application.isPlaying && info.evaluationType == FrameData.EvaluationType.Playback)
                Begin();
        }

        public override void OnBehaviourPause(Playable playable, FrameData info) => End();

        #endregion


        #region Private and Protected

        private string _previous;
        private bool _active;

        #endregion
    }
}

using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Nekuzaky.Resonance.Timeline
{
    [DisplayName("Game State")]
    public sealed class GameStateClip : PlayableAsset, ITimelineClipAsset
    {
        public StateGroup group;
        public string state = string.Empty;
        public bool restoreAtEnd;

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<GameStateBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            behaviour.Group = group;
            behaviour.State = state;
            behaviour.RestoreAtEnd = restoreAtEnd;
            return playable;
        }
    }
}

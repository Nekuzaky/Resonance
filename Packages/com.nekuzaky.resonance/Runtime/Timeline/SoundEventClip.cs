using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Nekuzaky.Resonance.Timeline
{
    [DisplayName("Sound Event")]
    public sealed class SoundEventClip : PlayableAsset, ITimelineClipAsset
    {
        public SoundEvent sound;
        public bool stopAtEnd = true;
        [Min(-1f)] public float stopFade = -1f;

        public ClipCaps clipCaps => ClipCaps.None;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<SoundEventBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            behaviour.Sound = sound;
            behaviour.StopAtEnd = stopAtEnd;
            behaviour.StopFade = stopFade;
            return playable;
        }
    }
}

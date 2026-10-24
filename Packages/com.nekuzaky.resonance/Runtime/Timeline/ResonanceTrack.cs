using System.ComponentModel;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Nekuzaky.Resonance.Timeline
{
    [TrackColor(0.18f, 0.66f, 0.72f)]
    [TrackClipType(typeof(SoundEventClip))]
    [TrackClipType(typeof(GameStateClip))]
    [TrackBindingType(typeof(GameObject))]
    [DisplayName("Resonance/Resonance Track")]
    public sealed class ResonanceTrack : TrackAsset
    {
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekuzaky.Resonance
{
    public enum MusicSync
    {
        Immediate,
        NextBeat,
        NextBar,
        EndOfSegment
    }

    [Serializable]
    public sealed class MusicSegment
    {
        public string name = "Segment";
        public AudioClip clip;
        [Min(0)] public int bars;
        public string next = string.Empty;
        [Range(-48f, 12f)] public float volume;
    }

    [Serializable]
    public sealed class MusicStateLink
    {
        public string state = string.Empty;
        public string segment = string.Empty;
        public string bridge = string.Empty;
    }

    [Serializable]
    public sealed class Stinger
    {
        public string name = "Stinger";
        public AudioClip clip;
        public MusicSync sync = MusicSync.NextBeat;
        [Range(-48f, 12f)] public float volume;
    }

    [CreateAssetMenu(menuName = "Resonance/Music Track", fileName = "New Music Track", order = 60)]
    public sealed class MusicTrack : ScriptableObject
    {
        #region Public

        [Min(1f)] public float bpm = 120f;
        [Range(1, 16)] public int beatsPerBar = 4;
        public AudioBus bus;
        [Range(-48f, 12f)] public float volume;
        public List<MusicSegment> segments = new();
        public string firstSegment = string.Empty;
        public StateGroup stateGroup;
        public List<MusicStateLink> links = new();
        public MusicSync transitionSync = MusicSync.NextBar;
        [Min(0f)] public float crossfade = 0.25f;
        public List<Stinger> stingers = new();

        public double BeatLength => 60.0 / Math.Max(1.0, bpm);

        public double BarLength => BeatLength * Math.Max(1, beatsPerBar);

        public MusicSegment Find(string segmentName)
        {
            foreach (var segment in segments)
            {
                if (segment != null && segment.name == segmentName)
                    return segment;
            }
            return null;
        }

        public MusicSegment First => Find(firstSegment) ?? (segments.Count > 0 ? segments[0] : null);

        public MusicSegment ForState(string state) => Find(LinkFor(state)?.segment);

        public MusicStateLink LinkFor(string state)
        {
            foreach (var link in links)
            {
                if (link != null && link.state == state)
                    return link;
            }
            return null;
        }

        public Stinger FindStinger(string stingerName)
        {
            foreach (var stinger in stingers)
            {
                if (stinger != null && stinger.name == stingerName)
                    return stinger;
            }
            return null;
        }

        public double LengthOf(MusicSegment segment)
        {
            if (segment == null)
                return 0.0;
            if (segment.bars > 0)
                return segment.bars * BarLength;
            return segment.clip != null ? segment.clip.length : 0.0;
        }

        #endregion
    }
}

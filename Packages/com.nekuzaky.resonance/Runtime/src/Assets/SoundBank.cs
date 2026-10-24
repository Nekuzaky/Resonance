using System.Collections.Generic;
using UnityEngine;

namespace Nekuzaky.Resonance
{
    [CreateAssetMenu(menuName = "Resonance/Sound Bank", fileName = "New Sound Bank", order = 80)]
    public sealed class SoundBank : ScriptableObject
    {
        #region Public

        public List<SoundEvent> events = new();
        public List<MusicTrack> music = new();

        public void CollectClips(HashSet<AudioClip> clips)
        {
            foreach (var sound in events)
                CollectEventClips(sound, clips);
            foreach (var track in music)
                CollectTrackClips(track, clips);
        }

        public static void CollectEventClips(SoundEvent sound, HashSet<AudioClip> clips)
        {
            if (sound == null)
                return;
            var walker = new NodeWalker(sound.root);
            for (var guard = 0; guard < NodeWalker.MaxNodes && walker.Next(out var node); guard++)
            {
                if (node is ClipNode { clip: not null } clip)
                    clips.Add(clip.clip);
                if (node is DialogueNode dialogue)
                    AddLines(dialogue, clips);
            }
        }

        private static void AddLines(DialogueNode dialogue, HashSet<AudioClip> clips)
        {
            foreach (var line in dialogue.lines)
            {
                if (line?.clip != null)
                    clips.Add(line.clip);
            }
        }

        public static void CollectTrackClips(MusicTrack track, HashSet<AudioClip> clips)
        {
            if (track == null)
                return;
            foreach (var segment in track.segments)
            {
                if (segment?.clip != null)
                    clips.Add(segment.clip);
            }
            foreach (var segment in track.segments)
            {
                if (segment?.layers == null)
                    continue;
                foreach (var layer in segment.layers)
                {
                    if (layer?.clip != null)
                        clips.Add(layer.clip);
                }
            }
            foreach (var stinger in track.stingers)
            {
                if (stinger?.clip != null)
                    clips.Add(stinger.clip);
            }
        }

        #endregion
    }
}

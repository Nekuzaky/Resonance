using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Nekuzaky.Resonance.Editor
{
    internal sealed class ImportGroup
    {
        public string EventName;
        public readonly SortedDictionary<string, List<AudioClip>> Variants = new(StringComparer.OrdinalIgnoreCase);

        public int ClipCount
        {
            get
            {
                var count = 0;
                foreach (var clips in Variants.Values)
                    count += clips.Count;
                return count;
            }
        }
    }

    internal static class BulkImport
    {
        #region Public

        public static List<ImportGroup> Plan(IEnumerable<AudioClip> clips, SwitchGroup surface)
        {
            var groups = new SortedDictionary<string, ImportGroup>(StringComparer.OrdinalIgnoreCase);
            foreach (var clip in clips)
            {
                if (clip == null)
                    continue;
                Parse(clip.name, surface, out var eventName, out var variant);
                if (!groups.TryGetValue(eventName, out var group))
                {
                    group = new ImportGroup { EventName = eventName };
                    groups[eventName] = group;
                }
                if (!group.Variants.TryGetValue(variant, out var list))
                {
                    list = new List<AudioClip>();
                    group.Variants[variant] = list;
                }
                list.Add(clip);
            }
            foreach (var group in groups.Values)
            {
                foreach (var list in group.Variants.Values)
                    list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            }
            return new List<ImportGroup>(groups.Values);
        }

        public static void Parse(string clipName, SwitchGroup surface, out string eventName, out string variant)
        {
            var name = Trailing.Replace(clipName.Trim(), string.Empty);
            variant = string.Empty;
            eventName = string.IsNullOrEmpty(name) ? clipName : name;
            if (surface == null)
                return;
            var cut = eventName.LastIndexOfAny(Separators);
            if (cut <= 0)
                return;
            var token = eventName.Substring(cut + 1);
            foreach (var value in surface.values)
            {
                if (!string.Equals(value, token, StringComparison.OrdinalIgnoreCase))
                    continue;
                variant = value;
                eventName = eventName.Substring(0, cut);
                return;
            }
        }

        public static SoundNode BuildTree(ImportGroup group, SwitchGroup surface)
        {
            if (group.Variants.Count == 1 && group.Variants.ContainsKey(string.Empty))
                return Variant(group.EventName, group.Variants[string.Empty]);
            var node = new SwitchNode { name = $"By {(surface != null ? surface.name : "Variant")}", group = surface };
            foreach (var pair in group.Variants)
            {
                var child = Variant(string.IsNullOrEmpty(pair.Key) ? "Default" : pair.Key, pair.Value);
                child.switchValue = pair.Key;
                if (string.IsNullOrEmpty(pair.Key))
                    node.fallback = node.children.Count;
                node.children.Add(child);
            }
            return node;
        }

        public static List<SoundEvent> Create(IReadOnlyList<ImportGroup> groups, SwitchGroup surface, string folder)
        {
            var created = new List<SoundEvent>();
            foreach (var group in groups)
            {
                var sound = ResonanceAssets.Create<SoundEvent>(group.EventName, folder);
                sound.root = BuildTree(group, surface);
                sound.root.volumeRandom = group.ClipCount > 1 ? 1.5f : 0f;
                sound.root.pitchRandom = group.ClipCount > 1 ? 0.5f : 0f;
                EditorUtility.SetDirty(sound);
                created.Add(sound);
            }
            AssetDatabase.SaveAssets();
            return created;
        }

        public static List<AudioClip> ClipsIn(string folder, bool recursive)
        {
            var clips = new List<AudioClip>();
            if (!AssetDatabase.IsValidFolder(folder))
                return clips;
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { folder }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!recursive && System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/') != folder.TrimEnd('/'))
                    continue;
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null)
                    clips.Add(clip);
            }
            return clips;
        }

        #endregion


        #region Private and Protected

        private static readonly Regex Trailing = new(@"[\s_\-\.]*\(?\d+\)?$", RegexOptions.Compiled);
        private static readonly char[] Separators = { '_', '-', ' ', '.' };

        private static SoundNode Variant(string name, List<AudioClip> clips)
        {
            if (clips.Count == 1)
                return new ClipNode { name = clips[0].name, clip = clips[0] };
            var random = new RandomNode { name = name, avoidRepeat = Mathf.Min(2, clips.Count - 1) };
            foreach (var clip in clips)
                random.children.Add(new ClipNode { name = clip.name, clip = clip });
            return random;
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Nekuzaky.Resonance.Editor
{
    internal sealed class AssetKind
    {
        public Type Type;
        public string Label;
        public string Glyph;
        public string Icon;
        public string DefaultName;
    }

    internal static class ResonanceAssets
    {
        #region Public

        public const string DefaultFolder = "Assets/Audio/Resonance";

        public static readonly AssetKind[] Kinds =
        {
            new() { Type = typeof(SoundEvent), Label = "Sound Events", Glyph = Glyphs.Event, Icon = nameof(SoundEvent), DefaultName = "New Sound Event" },
            new() { Type = typeof(MusicTrack), Label = "Music Tracks", Glyph = Glyphs.Music, Icon = nameof(MusicTrack), DefaultName = "New Music Track" },
            new() { Type = typeof(SoundParameter), Label = "Parameters", Glyph = Glyphs.Parameter, Icon = nameof(SoundParameter), DefaultName = "New Parameter" },
            new() { Type = typeof(SwitchGroup), Label = "Switch Groups", Glyph = Glyphs.SwitchGroup, Icon = nameof(SwitchGroup), DefaultName = "New Switch Group" },
            new() { Type = typeof(StateGroup), Label = "State Groups", Glyph = Glyphs.StateGroup, Icon = nameof(StateGroup), DefaultName = "New State Group" },
            new() { Type = typeof(AudioBus), Label = "Buses", Glyph = Glyphs.Bus, Icon = nameof(AudioBus), DefaultName = "New Bus" },
            new() { Type = typeof(MixSnapshot), Label = "Mix Snapshots", Glyph = Glyphs.Snapshot, Icon = nameof(MixSnapshot), DefaultName = "New Mix Snapshot" }
        };

        public static AssetKind KindOf(UnityEngine.Object asset)
        {
            if (asset == null)
                return null;
            foreach (var kind in Kinds)
            {
                if (kind.Type.IsInstanceOfType(asset))
                    return kind;
            }
            return null;
        }

        public static List<T> FindAll<T>() where T : ScriptableObject
        {
            var found = new List<T>();
            foreach (var asset in FindByType(typeof(T)))
                found.Add((T)asset);
            return found;
        }

        public static List<ScriptableObject> FindByType(Type type)
        {
            var found = new List<ScriptableObject>();
            foreach (var guid in AssetDatabase.FindAssets($"t:{type.Name}"))
            {
                var asset = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(guid), type) as ScriptableObject;
                if (asset != null)
                    found.Add(asset);
            }
            found.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));
            return found;
        }

        public static T Create<T>(string name, string folder = null) where T : ScriptableObject => (T)Create(typeof(T), name, folder);

        public static ScriptableObject Create(Type type, string name, string folder = null)
        {
            folder = EnsureFolder(string.IsNullOrEmpty(folder) ? DefaultFolder : folder);
            var asset = ScriptableObject.CreateInstance(type);
            var path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{name}.asset");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Undo.RegisterCreatedObjectUndo(asset, $"Create {name}");
            return asset;
        }

        public static SoundEvent CreateEventFromClips(IReadOnlyList<AudioClip> clips, string folder = null)
        {
            if (clips == null || clips.Count == 0)
                return null;

            var name = clips.Count == 1 ? clips[0].name : CommonName(clips);
            var sound = Create<SoundEvent>(name, folder ?? FolderOf(clips[0]));
            var root = clips.Count == 1 ? (ContainerNode)new LayerNode { name = "Layer" } : new RandomNode { name = "Random" };
            foreach (var clip in clips)
                root.children.Add(new ClipNode { name = clip.name, clip = clip });
            sound.root = clips.Count == 1 ? root.children[0] : root;
            EditorUtility.SetDirty(sound);
            AssetDatabase.SaveAssets();
            return sound;
        }

        public static string EnsureFolder(string folder)
        {
            folder = folder.Replace('\\', '/').TrimEnd('/');
            if (AssetDatabase.IsValidFolder(folder))
                return folder;

            var parts = folder.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
            return folder;
        }

        public static string FolderOf(UnityEngine.Object asset)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path) || !path.StartsWith("Assets", StringComparison.Ordinal))
                return DefaultFolder;
            return Path.GetDirectoryName(path)?.Replace('\\', '/') ?? DefaultFolder;
        }

        public static void CreateDefaultMix()
        {
            var master = Create<AudioBus>("Master");
            var music = Create<AudioBus>("Music");
            var sfx = Create<AudioBus>("SFX");
            var voice = Create<AudioBus>("Voice");
            var ambience = Create<AudioBus>("Ambience");
            music.parent = master;
            sfx.parent = master;
            voice.parent = master;
            ambience.parent = master;
            voice.ducking.Add(new DuckRule { target = music, amount = -9f });
            voice.ducking.Add(new DuckRule { target = sfx, amount = -4f });
            foreach (var bus in new[] { master, music, sfx, voice, ambience })
                EditorUtility.SetDirty(bus);
            AssetDatabase.SaveAssets();
        }

        #endregion


        #region Private and Protected

        private static string CommonName(IReadOnlyList<AudioClip> clips)
        {
            var prefix = clips[0].name;
            foreach (var clip in clips)
            {
                var length = 0;
                var limit = Mathf.Min(prefix.Length, clip.name.Length);
                while (length < limit && prefix[length] == clip.name[length])
                    length++;
                prefix = prefix.Substring(0, length);
            }
            prefix = prefix.TrimEnd('_', '-', ' ', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
            return string.IsNullOrEmpty(prefix) ? "New Sound Event" : prefix;
        }

        #endregion
    }
}

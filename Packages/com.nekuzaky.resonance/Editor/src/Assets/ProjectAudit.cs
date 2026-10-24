using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Nekuzaky.Resonance.Editor
{
    internal enum Severity
    {
        Error,
        Warning,
        Info
    }

    internal sealed class Problem
    {
        public Severity Severity;
        public ScriptableObject Asset;
        public string Message;
    }

    internal static class ProjectAudit
    {
        #region Public

        public static List<Problem> Run(ReferenceIndex references = null)
        {
            var problems = new List<Problem>();
            foreach (var sound in ResonanceAssets.FindAll<SoundEvent>())
                CheckEvent(sound, references, problems);
            foreach (var track in ResonanceAssets.FindAll<MusicTrack>())
                CheckTrack(track, problems);
            foreach (var bus in ResonanceAssets.FindAll<AudioBus>())
                CheckBus(bus, problems);
            problems.Sort((a, b) => a.Severity != b.Severity ? a.Severity.CompareTo(b.Severity) : string.CompareOrdinal(a.Asset.name, b.Asset.name));
            return problems;
        }

        public static void CheckEvent(SoundEvent sound, ReferenceIndex references, List<Problem> problems)
        {
            if (sound.root == null)
                Add(problems, Severity.Error, sound, "plays nothing: its sound tree is empty");
            var walker = new NodeWalker(sound.root);
            for (var guard = 0; guard < NodeWalker.MaxNodes && walker.Next(out var node); guard++)
                CheckNode(sound, node, problems);
            if (sound.bus == null)
                Add(problems, Severity.Warning, sound, "has no bus, so no fader, ducking or snapshot affects it");
            if (references != null && references.UsersOf(sound).Count == 0)
                Add(problems, Severity.Info, sound, "is not referenced by any scene, prefab or asset (it may still be played from code)");
        }

        public static void CheckTrack(MusicTrack track, List<Problem> problems)
        {
            if (track.segments.Count == 0)
                Add(problems, Severity.Error, track, "has no segment");
            foreach (var segment in track.segments)
            {
                if (segment == null)
                    continue;
                if (segment.clip == null)
                    Add(problems, Severity.Error, track, $"segment '{segment.name}' has no clip");
                if (!string.IsNullOrEmpty(segment.next) && track.Find(segment.next) == null)
                    Add(problems, Severity.Error, track, $"segment '{segment.name}' continues to '{segment.next}', which does not exist");
            }
            foreach (var link in track.links)
            {
                if (link == null)
                    continue;
                if (track.Find(link.segment) == null)
                    Add(problems, Severity.Error, track, $"state '{link.state}' links to segment '{link.segment}', which does not exist");
                if (track.stateGroup != null && !track.stateGroup.states.Contains(link.state))
                    Add(problems, Severity.Warning, track, $"state '{link.state}' is not in the state group '{track.stateGroup.name}'");
            }
        }

        public static void CheckBus(AudioBus bus, List<Problem> problems)
        {
            var current = bus.parent;
            for (var depth = 0; depth < AudioBus.MaxDepth && current != null; depth++)
            {
                if (current == bus)
                {
                    Add(problems, Severity.Error, bus, "is its own ancestor: its parent chain loops");
                    return;
                }
                current = current.parent;
            }
            foreach (var rule in bus.ducking)
            {
                if (rule?.target == null)
                    Add(problems, Severity.Warning, bus, "has a duck rule without a target bus");
            }
        }

        #endregion


        #region Private and Protected

        private static void CheckNode(SoundEvent sound, SoundNode node, List<Problem> problems)
        {
            switch (node)
            {
                case ClipNode { clip: null }:
                    Add(problems, Severity.Error, sound, $"clip node '{node.name}' has no audio clip");
                    break;
                case SwitchNode { group: null }:
                    Add(problems, Severity.Error, sound, $"switch '{node.name}' has no switch group");
                    break;
                case SwitchNode switchNode:
                    CheckSwitchValues(sound, switchNode, problems);
                    break;
                case BlendNode { parameter: null }:
                    Add(problems, Severity.Error, sound, $"blend '{node.name}' has no parameter");
                    break;
                case ContainerNode { children: { Count: 0 } }:
                    Add(problems, Severity.Warning, sound, $"{node.Kind.ToString().ToLowerInvariant()} '{node.name}' is empty");
                    break;
            }
        }

        private static void CheckSwitchValues(SoundEvent sound, SwitchNode node, List<Problem> problems)
        {
            foreach (var child in node.children)
            {
                if (child != null && !string.IsNullOrEmpty(child.switchValue) && !node.group.values.Contains(child.switchValue))
                    Add(problems, Severity.Warning, sound, $"'{child.name}' plays for '{child.switchValue}', which is not a value of '{node.group.name}'");
            }
        }

        private static void Add(List<Problem> problems, Severity severity, ScriptableObject asset, string message) =>
            problems.Add(new Problem { Severity = severity, Asset = asset, Message = message });

        #endregion
    }

    internal sealed class ReferenceIndex
    {
        #region Public

        public static ReferenceIndex Build()
        {
            var index = new ReferenceIndex();
            var paths = AssetDatabase.GetAllAssetPaths();
            for (var i = 0; i < paths.Length; i++)
            {
                var path = paths[i];
                if (!path.StartsWith("Assets/") || !IsUser(path))
                    continue;
                if (i % 200 == 0)
                    EditorUtility.DisplayProgressBar("Resonance", "Finding where sounds are used...", i / (float)paths.Length);
                foreach (var dependency in AssetDatabase.GetDependencies(path, false))
                    index.Link(dependency, path);
            }
            EditorUtility.ClearProgressBar();
            return index;
        }

        public IReadOnlyList<string> UsersOf(Object asset)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            return _users.TryGetValue(path, out var users) ? users : Empty;
        }

        #endregion


        #region Private and Protected

        private static readonly string[] Empty = new string[0];
        private readonly Dictionary<string, List<string>> _users = new();

        private static bool IsUser(string path) =>
            path.EndsWith(".unity") || path.EndsWith(".prefab") || path.EndsWith(".asset") || path.EndsWith(".playable") ||
            path.EndsWith(".anim") || path.EndsWith(".controller");

        private void Link(string dependency, string user)
        {
            if (dependency == user)
                return;
            if (!_users.TryGetValue(dependency, out var users))
            {
                users = new List<string>();
                _users[dependency] = users;
            }
            users.Add(user);
        }

        #endregion
    }
}

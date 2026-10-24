using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.Resonance.Editor
{
    internal sealed partial class EventEditor
    {
        #region Private and Protected

        private static readonly NodeKind[] MenuKinds =
        {
            NodeKind.Clip, NodeKind.Random, NodeKind.Sequence, NodeKind.Switch, NodeKind.Blend, NodeKind.Layer
        };

        private static void ShowKindMenu(Action<NodeKind> pick)
        {
            var menu = new GenericMenu();
            foreach (var kind in MenuKinds)
                menu.AddItem(new GUIContent(Describe(kind)), false, () => pick(kind));
            menu.ShowAsContext();
        }

        private static string Describe(NodeKind kind)
        {
            switch (kind)
            {
                case NodeKind.Random: return "Random  (pick one child)";
                case NodeKind.Sequence: return "Sequence  (one after the other)";
                case NodeKind.Switch: return "Switch  (pick by game value)";
                case NodeKind.Blend: return "Blend  (crossfade by parameter)";
                case NodeKind.Layer: return "Layer  (play all together)";
                default: return "Clip  (one audio file)";
            }
        }

        private void FillContextMenu(ContextualMenuPopulateEvent evt, Row row)
        {
            if (row == null)
                return;
            SelectNodeRow(row);
            foreach (var kind in MenuKinds)
                evt.menu.AppendAction($"Add/{Describe(kind)}", _ => Add(kind));
            if (row.IsEvent)
                return;
            foreach (var kind in MenuKinds)
            {
                if (kind != NodeKind.Clip)
                    evt.menu.AppendAction($"Wrap in/{Describe(kind)}", _ => Wrap(kind));
            }
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Duplicate", _ => Duplicate());
            evt.menu.AppendAction("Delete", _ => Delete());
        }

        private void SelectNodeRow(Row row)
        {
            var index = _rows.IndexOf(row);
            if (index >= 0 && index != _tree.selectedIndex)
                SelectIndex(index);
        }

        private ContainerNode TargetContainer()
        {
            var row = _selected ?? _rows[0];
            if (row.IsEvent)
                return _sound.root as ContainerNode;
            return row.Node as ContainerNode ?? row.Parent;
        }

        private void Add(NodeKind kind)
        {
            var node = SoundNodes.Create(kind);
            AddNodes(new[] { node });
            SelectNode(node);
        }

        private void AddNodes(IReadOnlyList<SoundNode> nodes)
        {
            Commit("Add sound node", () =>
            {
                if (_sound.root == null && nodes.Count == 1)
                {
                    _sound.root = nodes[0];
                    return;
                }
                var target = TargetContainer();
                if (target == null)
                {
                    var wrapper = new RandomNode { name = "Random" };
                    if (_sound.root != null)
                        wrapper.children.Add(_sound.root);
                    _sound.root = wrapper;
                    target = wrapper;
                }
                _collapsed.Remove(target);
                target.children.AddRange(nodes);
            });
        }

        private void Wrap(NodeKind kind)
        {
            var row = _selected;
            if (row == null || row.IsEvent || kind == NodeKind.Clip)
                return;
            var wrapper = (ContainerNode)SoundNodes.Create(kind);
            Commit("Wrap sound node", () =>
            {
                wrapper.children.Add(row.Node);
                Replace(row, wrapper);
            });
            SelectNode(wrapper);
        }

        private void Replace(Row row, SoundNode node)
        {
            if (row.Parent == null)
                _sound.root = node;
            else
                row.Parent.children[row.Index] = node;
        }

        private void Duplicate()
        {
            var row = _selected;
            if (row == null || row.IsEvent || row.Parent == null)
                return;
            var copy = row.Node.Clone();
            copy.name = row.Node.name + " Copy";
            Commit("Duplicate sound node", () => row.Parent.children.Insert(row.Index + 1, copy));
            SelectNode(copy);
        }

        private void Delete()
        {
            var row = _selected;
            if (row == null || row.IsEvent)
                return;
            var index = _rows.IndexOf(row);
            Commit("Delete sound node", () =>
            {
                if (row.Parent == null)
                    _sound.root = null;
                else
                    row.Parent.children.RemoveAt(row.Index);
            });
            SelectIndex(index - 1);
        }

        private void Move(int direction)
        {
            var row = _selected;
            if (row?.Parent == null)
                return;
            var target = row.Index + direction;
            if (target < 0 || target >= row.Parent.children.Count)
                return;
            Commit("Move sound node", () =>
            {
                var children = row.Parent.children;
                (children[row.Index], children[target]) = (children[target], children[row.Index]);
            });
            SelectNode(row.Node);
        }

        private static List<AudioClip> DraggedClips()
        {
            var clips = new List<AudioClip>();
            foreach (var reference in DragAndDrop.objectReferences)
            {
                if (reference is AudioClip clip)
                    clips.Add(clip);
            }
            return clips;
        }

        private void DropClips(Row row)
        {
            var clips = DraggedClips();
            if (clips.Count == 0 || row == null)
                return;
            DragAndDrop.AcceptDrag();
            SelectNodeRow(row);

            if (clips.Count == 1 && row.Node is ClipNode { clip: null } empty)
            {
                Commit("Assign clip", () => empty.clip = clips[0]);
                ShowProperties(_selected);
                return;
            }
            var nodes = new List<SoundNode>();
            foreach (var clip in clips)
                nodes.Add(new ClipNode { name = clip.name, clip = clip });
            AddNodes(nodes);
            SelectNode(nodes[nodes.Count - 1]);
        }

        #endregion
    }
}

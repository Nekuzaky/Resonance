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

        private sealed class Row
        {
            public string Path;
            public int Depth;
            public SoundNode Node;
            public ContainerNode Parent;
            public int Index;
            public bool IsEvent;
        }

        private sealed class RowView
        {
            public VisualElement Element;
            public VisualElement Toggle;
            public VisualElement Icon;
            public Label Name;
            public Label Tag;
        }

        private readonly List<Row> _rows = new();
        private readonly Stack<Row> _pending = new();
        private readonly HashSet<SoundNode> _collapsed = new();
        private readonly Dictionary<VisualElement, RowView> _views = new();
        private ListView _tree;
        private Row _selected;

        private VisualElement BuildTree()
        {
            var panel = Ui.Box("rs-tree");
            var actions = Ui.Box("rs-tree-actions");
            actions.Add(Ui.TextButton(Glyphs.Add, "Add", () => ShowKindMenu(Add)));
            actions.Add(Ui.TextButton(Glyphs.Hierarchy, "Wrap", () => ShowKindMenu(Wrap)));
            actions.Add(Ui.Spacer());
            actions.Add(Ui.IconButton(Glyphs.Up, "Move up", () => Move(-1), "rs-button--flat"));
            actions.Add(Ui.IconButton(Glyphs.Down, "Move down", () => Move(1), "rs-button--flat"));
            actions.Add(Ui.IconButton(Glyphs.Duplicate, "Duplicate", Duplicate, "rs-button--flat"));
            actions.Add(Ui.IconButton(Glyphs.Remove, "Delete", Delete, "rs-button--flat"));
            panel.Add(actions);

            _tree = new ListView
            {
                fixedItemHeight = 26f,
                selectionType = SelectionType.Single,
                itemsSource = _rows,
                makeItem = MakeRow,
                bindItem = BindRow,
                style = { flexGrow = 1f }
            };
            _tree.selectionChanged += _ => OnSelectionChanged();
            panel.Add(_tree);
            panel.Add(Ui.Help("Drag audio clips onto a row to add them. Space plays, Escape stops."));
            return panel;
        }

        private void RebuildTree()
        {
            _rows.Clear();
            _rows.Add(new Row { IsEvent = true, Path = string.Empty });
            _pending.Clear();
            if (_sound.root != null)
                _pending.Push(new Row { Node = _sound.root, Path = "root", Depth = 1 });

            for (var guard = 0; guard < NodeWalker.MaxNodes && _pending.Count > 0; guard++)
            {
                var row = _pending.Pop();
                _rows.Add(row);
                if (row.Node is not ContainerNode container || _collapsed.Contains(container))
                    continue;
                for (var i = container.children.Count - 1; i >= 0; i--)
                {
                    _pending.Push(new Row
                    {
                        Node = container.children[i],
                        Path = $"{row.Path}.children.Array.data[{i}]",
                        Depth = row.Depth + 1,
                        Parent = container,
                        Index = i
                    });
                }
            }
            _tree.RefreshItems();
        }

        private VisualElement MakeRow()
        {
            var view = new RowView { Element = Ui.Box("rs-node") };
            view.Toggle = Glyphs.Icon(Glyphs.Collapsed, "rs-node-toggle", "rs-icon--dim");
            view.Toggle.pickingMode = PickingMode.Position;
            view.Toggle.RegisterCallback<ClickEvent>(evt =>
            {
                ToggleCollapse(view.Element.userData as Row);
                evt.StopPropagation();
            });
            view.Icon = Glyphs.Icon(Glyphs.Clip);
            view.Name = Ui.Styled(string.Empty, "rs-node-name");
            view.Tag = Ui.Styled(string.Empty, "rs-node-tag");
            view.Element.Add(view.Toggle);
            view.Element.Add(view.Icon);
            view.Element.Add(view.Name);
            view.Element.Add(view.Tag);
            view.Element.RegisterCallback<DragUpdatedEvent>(_ =>
            {
                if (DraggedClips().Count > 0)
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            });
            view.Element.RegisterCallback<DragPerformEvent>(_ => DropClips(view.Element.userData as Row));
            view.Element.AddManipulator(new ContextualMenuManipulator(evt => FillContextMenu(evt, view.Element.userData as Row)));
            view.Element.userData = null;
            _views[view.Element] = view;
            return view.Element;
        }

        private void BindRow(VisualElement element, int index)
        {
            var row = _rows[index];
            var view = _views[element];
            element.userData = row;
            element.style.paddingLeft = 6f + row.Depth * 16f;
            element.EnableInClassList("rs-node--event", row.IsEvent);
            element.EnableInClassList("rs-node--missing", IsBroken(row));

            var container = row.Node as ContainerNode;
            view.Toggle.style.visibility = container != null && container.children.Count > 0 ? Visibility.Visible : Visibility.Hidden;
            Glyphs.Set(view.Toggle, container != null && _collapsed.Contains(container) ? Glyphs.Collapsed : Glyphs.Expanded);

            foreach (NodeKind kind in Enum.GetValues(typeof(NodeKind)))
                view.Icon.RemoveFromClassList(Ui.KindClass(kind));
            if (row.IsEvent)
            {
                Glyphs.Set(view.Icon, Glyphs.Event);
                view.Name.text = _sound.name;
                view.Tag.text = _sound.bus != null ? $"Event  ·  {_sound.bus.name}" : "Event";
                return;
            }
            Glyphs.Set(view.Icon, Glyphs.Of(row.Node.Kind));
            view.Icon.AddToClassList(Ui.KindClass(row.Node.Kind));
            view.Name.text = string.IsNullOrEmpty(row.Node.name) ? row.Node.Kind.ToString() : row.Node.name;
            view.Tag.text = TagOf(row);
        }

        private static string TagOf(Row row)
        {
            var node = row.Node;
            var tag = node switch
            {
                ClipNode clip => clip.clip != null ? (clip.loop ? "loop" : $"{clip.clip.length:0.00}s") : "no clip",
                RandomNode random => $"random of {random.children.Count}",
                SequenceNode sequence => sequence.mode == SequenceMode.Step ? "step" : sequence.loop ? "continuous, loop" : "continuous",
                SwitchNode switchNode => switchNode.group != null ? $"by {switchNode.group.name}" : "no switch group",
                BlendNode blend => blend.parameter != null ? $"by {blend.parameter.name}" : "no parameter",
                LayerNode layer => $"{layer.children.Count} layers",
                _ => string.Empty
            };
            switch (row.Parent)
            {
                case SwitchNode:
                    return $"= {(string.IsNullOrEmpty(node.switchValue) ? "?" : node.switchValue)}   {tag}";
                case BlendNode:
                    return $"{node.blendRange.x:0.##} to {node.blendRange.y:0.##}   {tag}";
                case RandomNode when !Mathf.Approximately(node.weight, 1f):
                    return $"x{node.weight:0.##}   {tag}";
                default:
                    return tag;
            }
        }

        private static bool IsBroken(Row row)
        {
            switch (row.Node)
            {
                case ClipNode clip: return clip.clip == null;
                case SwitchNode switchNode: return switchNode.group == null;
                case BlendNode blend: return blend.parameter == null;
                default: return false;
            }
        }

        private void ToggleCollapse(Row row)
        {
            if (row?.Node is not ContainerNode container)
                return;
            if (!_collapsed.Remove(container))
                _collapsed.Add(container);
            RebuildTree();
        }

        private void OnSelectionChanged()
        {
            var index = _tree.selectedIndex;
            _selected = index >= 0 && index < _rows.Count ? _rows[index] : null;
            ShowProperties(_selected);
        }

        private void SelectIndex(int index)
        {
            if (_rows.Count == 0)
                return;
            _tree.SetSelection(Mathf.Clamp(index, 0, _rows.Count - 1));
            OnSelectionChanged();
        }

        private void SelectNode(SoundNode node)
        {
            for (var i = 0; i < _rows.Count; i++)
            {
                if (_rows[i].Node == node && node != null)
                {
                    SelectIndex(i);
                    return;
                }
            }
            SelectIndex(0);
        }

        private void SelectPath(string path)
        {
            for (var i = 0; i < _rows.Count; i++)
            {
                if (_rows[i].Path == path)
                {
                    SelectIndex(i);
                    return;
                }
            }
            SelectIndex(0);
        }

        #endregion
    }
}

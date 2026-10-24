using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.Resonance.Editor
{
    internal sealed class AssetBrowser : VisualElement
    {
        #region Public

        public event Action<ScriptableObject> Selected;

        public ScriptableObject Selection => _selected;

        public AssetBrowser()
        {
            AddToClassList("rs-browser");

            var search = Ui.Box("rs-search");
            search.Add(Glyphs.Icon(Glyphs.Search, "rs-icon--small", "rs-icon--dim"));
            _search = new TextField();
            _search.RegisterValueChangedCallback(_ => Rebuild());
            search.Add(_search);
            Add(search);

            _list = new ScrollView(ScrollViewMode.Vertical) { style = { flexGrow = 1f } };
            Add(_list);

            _dropHint = Ui.Styled("Drop audio clips here to create a Sound Event. Hold Alt to create one event per clip.", "rs-drop-hint");
            Add(_dropHint);

            RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            RegisterCallback<DragPerformEvent>(OnDragPerform);
            RegisterCallback<DragLeaveEvent>(_ => _dropHint.RemoveFromClassList("rs-drop-hint--active"));
            Rebuild();
        }

        public void Select(ScriptableObject asset)
        {
            _selected = asset;
            foreach (var pair in _rows)
                pair.Value.EnableInClassList("rs-row--selected", pair.Key == asset);
            Selected?.Invoke(asset);
        }

        public void Rebuild()
        {
            _list.Clear();
            _rows.Clear();
            var filter = _search.value?.Trim() ?? string.Empty;
            foreach (var kind in ResonanceAssets.Kinds)
                AddSection(kind, filter);
        }

        #endregion


        #region Private and Protected

        private readonly TextField _search;
        private readonly ScrollView _list;
        private readonly Label _dropHint;
        private readonly Dictionary<ScriptableObject, VisualElement> _rows = new();
        private readonly HashSet<Type> _collapsed = new();
        private ScriptableObject _selected;

        private void AddSection(AssetKind kind, string filter)
        {
            var assets = ResonanceAssets.FindByType(kind.Type);
            if (filter.Length > 0)
                assets.RemoveAll(asset => asset.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0);

            var collapsed = _collapsed.Contains(kind.Type) && filter.Length == 0;
            var header = Ui.Box("rs-section-header");
            header.Add(Glyphs.Icon(collapsed ? Glyphs.Collapsed : Glyphs.Expanded, "rs-icon--small", "rs-icon--dim"));
            header.Add(Glyphs.AssetImage(kind.Icon, "rs-asset-icon"));
            header.Add(new Label(kind.Label.ToUpperInvariant()));
            header.Add(Ui.Styled(assets.Count.ToString(), "rs-section-count"));
            header.Add(Ui.Spacer());
            header.Add(Ui.IconButton(Glyphs.Add, $"Create a new {kind.DefaultName.Replace("New ", string.Empty)}", () => Create(kind), "rs-button--flat"));
            header.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.target is VisualElement element && element.GetFirstAncestorOfType<Button>() != null)
                    return;
                if (!_collapsed.Remove(kind.Type))
                    _collapsed.Add(kind.Type);
                Rebuild();
            });
            _list.Add(header);

            if (collapsed)
                return;
            foreach (var asset in assets)
                _list.Add(BuildRow(kind, asset));
        }

        private VisualElement BuildRow(AssetKind kind, ScriptableObject asset)
        {
            var row = Ui.Box("rs-row");
            row.Add(Glyphs.AssetImage(kind.Icon, "rs-asset-icon"));
            row.Add(new Label(asset.name));
            row.EnableInClassList("rs-row--selected", asset == _selected);
            row.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.clickCount == 2)
                    EditorGUIUtility.PingObject(asset);
                Select(asset);
            });
            row.AddManipulator(new ContextualMenuManipulator(evt =>
            {
                evt.menu.AppendAction("Show in Project", _ => EditorGUIUtility.PingObject(asset));
                evt.menu.AppendAction("Duplicate", _ => Duplicate(asset));
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Delete", _ => Delete(asset));
            }));
            _rows[asset] = row;
            return row;
        }

        private void Create(AssetKind kind)
        {
            var folder = _selected != null ? ResonanceAssets.FolderOf(_selected) : null;
            var asset = ResonanceAssets.Create(kind.Type, kind.DefaultName, folder);
            _collapsed.Remove(kind.Type);
            Rebuild();
            Select(asset);
        }

        private void Duplicate(ScriptableObject asset)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            var copyPath = AssetDatabase.GenerateUniqueAssetPath(path);
            if (!AssetDatabase.CopyAsset(path, copyPath))
                return;
            Rebuild();
            Select(AssetDatabase.LoadAssetAtPath<ScriptableObject>(copyPath));
        }

        private void Delete(ScriptableObject asset)
        {
            var path = AssetDatabase.GetAssetPath(asset);
            if (!EditorUtility.DisplayDialog("Delete asset", $"Move '{asset.name}' to the trash?\n\n{path}", "Delete", "Cancel"))
                return;
            AssetDatabase.MoveAssetToTrash(path);
            if (_selected == asset)
                Select(null);
            Rebuild();
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

        private void OnDragUpdated(DragUpdatedEvent evt)
        {
            if (DraggedClips().Count == 0)
                return;
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            _dropHint.AddToClassList("rs-drop-hint--active");
        }

        private void OnDragPerform(DragPerformEvent evt)
        {
            var clips = DraggedClips();
            _dropHint.RemoveFromClassList("rs-drop-hint--active");
            if (clips.Count == 0)
                return;
            DragAndDrop.AcceptDrag();

            SoundEvent last = null;
            if (evt.altKey)
            {
                foreach (var clip in clips)
                    last = ResonanceAssets.CreateEventFromClips(new[] { clip });
            }
            else
            {
                last = ResonanceAssets.CreateEventFromClips(clips);
            }
            Rebuild();
            Select(last);
        }

        #endregion
    }
}

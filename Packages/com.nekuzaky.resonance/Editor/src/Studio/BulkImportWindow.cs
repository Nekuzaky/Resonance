using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.Resonance.Editor
{
    internal sealed class BulkImportWindow : EditorWindow
    {
        #region Public

        [MenuItem("Tools/Resonance/Bulk Import Clips", priority = 22)]
        public static void Open()
        {
            var window = GetWindow<BulkImportWindow>(true, "Resonance Bulk Import");
            window.minSize = new Vector2(560f, 480f);
            window.Show();
        }

        #endregion


        #region Private and Protected

        private ObjectField _folder;
        private ObjectField _surface;
        private Toggle _recursive;
        private ScrollView _preview;
        private Label _summary;
        private Button _create;
        private List<ImportGroup> _groups = new();

        private string FolderPath => _folder.value != null ? AssetDatabase.GetAssetPath(_folder.value) : null;

        private void Refresh()
        {
            _preview.Clear();
            var path = FolderPath;
            var clips = string.IsNullOrEmpty(path) ? new List<AudioClip>() : BulkImport.ClipsIn(path, _recursive.value);
            _groups = BulkImport.Plan(clips, _surface.value as SwitchGroup);
            foreach (var group in _groups)
                _preview.Add(Row(group));
            _summary.text = clips.Count == 0
                ? "Pick a folder that contains audio clips."
                : $"{clips.Count} clips become {_groups.Count} Sound Events.";
            _create.SetEnabled(_groups.Count > 0);
        }

        private static VisualElement Row(ImportGroup group)
        {
            var row = Ui.Box("rs-table-row");
            row.Add(Glyphs.AssetImage(nameof(SoundEvent), "rs-asset-icon"));
            var name = Ui.Styled(group.EventName, "rs-cell");
            name.style.width = 200f;
            row.Add(name);
            var parts = new List<string>();
            foreach (var pair in group.Variants)
                parts.Add(string.IsNullOrEmpty(pair.Key) ? $"{pair.Value.Count} clips" : $"{pair.Key}: {pair.Value.Count}");
            row.Add(Ui.Styled(string.Join("   ", parts), "rs-cell", "rs-cell--dim"));
            return row;
        }

        private void CreateEvents()
        {
            var path = FolderPath;
            if (string.IsNullOrEmpty(path) || _groups.Count == 0)
                return;
            var created = BulkImport.Create(_groups, _surface.value as SwitchGroup, path);
            Debug.Log($"Resonance created {created.Count} Sound Events in {path}.");
            if (created.Count > 0)
                StudioWindow.Open(created[0]);
            Close();
        }

        #endregion


        #region Unity Callbacks

        private void CreateGUI()
        {
            var root = rootVisualElement;
            root.styleSheets.Add(Glyphs.StyleSheet);
            root.AddToClassList("rs-root");
            var content = Ui.Box("rs-props-content");
            content.style.flexGrow = 1f;
            content.Add(Ui.Styled("Turn a folder of clips into Sound Events", "rs-empty-title"));
            content.Add(Ui.Help("Clips that differ only by a trailing number become one event with a Random container: " +
                                "step_01, step_02 and step_03 make 'step'. Pick a Switch Group and clips whose name ends with one of " +
                                "its values are grouped by it: step_grass_01 and step_stone_01 make 'step', switched by surface."));
            _folder = new ObjectField("Folder") { objectType = typeof(DefaultAsset), allowSceneObjects = false };
            _surface = new ObjectField("Switch Group (optional)") { objectType = typeof(SwitchGroup), allowSceneObjects = false };
            _recursive = new Toggle("Include Subfolders");
            _folder.RegisterValueChangedCallback(_ => Refresh());
            _surface.RegisterValueChangedCallback(_ => Refresh());
            _recursive.RegisterValueChangedCallback(_ => Refresh());
            content.Add(_folder);
            content.Add(_surface);
            content.Add(_recursive);
            _summary = Ui.Styled(string.Empty, "rs-help");
            content.Add(_summary);
            _preview = new ScrollView { style = { flexGrow = 1f } };
            content.Add(_preview);
            _create = Ui.TextButton(Glyphs.Magic, "Create Sound Events", CreateEvents, "rs-button--primary");
            content.Add(_create);
            root.Add(content);
            if (Selection.activeObject is DefaultAsset folder && AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(folder)))
                _folder.value = folder;
            Refresh();
        }

        #endregion
    }
}

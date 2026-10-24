using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.Resonance.Editor
{
    internal sealed partial class EventEditor : VisualElement
    {
        #region Public

        public EventEditor(SoundEvent sound)
        {
            _sound = sound;
            _serialized = new SerializedObject(sound);
            style.flexGrow = 1f;
            focusable = true;

            var transport = Ui.Box("rs-preview-row");
            transport.style.marginBottom = 0f;
            transport.Add(Ui.TextButton(Glyphs.Play, "Play", Play, "rs-button--primary"));
            transport.Add(Ui.TextButton(Glyphs.Stop, "Stop", Stop));
            _status = Ui.Styled(string.Empty, "rs-node-tag");
            transport.Add(_status);
            transport.Add(Ui.TextButton(Glyphs.Hierarchy, "Used By", ShowUsers, "rs-button--flat"));
            Add(Toolbar.Build(sound, nameof(SoundEvent), transport));

            var split = Ui.Box("rs-split");
            split.Add(BuildTree());
            split.Add(BuildProperties());
            Add(split);

            _preview = new PreviewPanel(sound);
            Add(_preview);

            RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
            RegisterCallback<AttachToPanelEvent>(_ => Undo.undoRedoPerformed += OnUndoRedo);
            RegisterCallback<DetachFromPanelEvent>(_ => Undo.undoRedoPerformed -= OnUndoRedo);
            RebuildTree();
            SelectIndex(0);
        }

        public void Refresh()
        {
            if (_sound == null)
                return;
            var engine = PreviewEngine.Engine;
            var playing = engine.CountPlaying(_sound, null);
            _status.text = playing > 0 ? $"{playing} playing" : _sound.IsLooping ? "loops until stopped" : string.Empty;
            _preview.Refresh();
        }

        #endregion


        #region Private and Protected

        private readonly SoundEvent _sound;
        private readonly SerializedObject _serialized;
        private readonly Label _status;
        private readonly PreviewPanel _preview;

        private void Play()
        {
            _serialized.ApplyModifiedProperties();
            PreviewEngine.Engine.Play(_sound, null, Vector3.zero);
        }

        private void Stop() => PreviewEngine.Engine.StopEvent(_sound, -1f);

        private void ShowUsers()
        {
            var users = ReferenceIndex.Build().UsersOf(_sound);
            var menu = new UnityEditor.GenericMenu();
            if (users.Count == 0)
                menu.AddDisabledItem(new GUIContent("Not used by any scene, prefab or asset"));
            foreach (var path in users)
            {
                var target = path;
                menu.AddItem(new GUIContent(target.Replace('/', '\\')), false, () =>
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(target)));
            }
            menu.ShowAsContext();
        }

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.target is TextField || evt.target is BaseField<string> || evt.target is TextElement { focusable: true })
                return;
            if (evt.keyCode == KeyCode.Space)
            {
                Play();
                evt.StopPropagation();
            }
            else if (evt.keyCode == KeyCode.Escape)
            {
                Stop();
                evt.StopPropagation();
            }
        }

        private void OnUndoRedo()
        {
            if (_sound == null)
                return;
            _serialized.Update();
            var path = _selected?.Path;
            RebuildTree();
            SelectPath(path);
            _preview.Rebuild();
        }

        private void Commit(string undoName, System.Action change)
        {
            Undo.RecordObject(_sound, undoName);
            change();
            EditorUtility.SetDirty(_sound);
            _serialized.Update();
            _sound.ResetRuntimeState();
            RebuildTree();
            _preview.Rebuild();
        }

        #endregion
    }
}

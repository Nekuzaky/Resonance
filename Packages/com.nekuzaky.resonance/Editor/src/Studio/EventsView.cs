using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.Resonance.Editor
{
    internal sealed class EventsView : IStudioView
    {
        #region Public

        public VisualElement Root { get; }

        public EventsView()
        {
            Root = Ui.Box("rs-body");
            _browser = new AssetBrowser();
            _browser.Selected += Show;
            _main = Ui.Box("rs-main");
            Root.Add(_browser);
            Root.Add(_main);
            Show(null);
        }

        public void Select(ScriptableObject asset) => _browser.Select(asset);

        public void Refresh() => _eventEditor?.Refresh();

        public void ProjectChanged()
        {
            _browser.Rebuild();
            if (!ReferenceEquals(_shown, null) && _shown == null)
                Show(null);
        }

        #endregion


        #region Private and Protected

        private readonly AssetBrowser _browser;
        private readonly VisualElement _main;
        private EventEditor _eventEditor;
        private ScriptableObject _shown;

        private void Show(ScriptableObject asset)
        {
            _main.Clear();
            _eventEditor = null;
            _shown = asset;
            switch (asset)
            {
                case null:
                    _main.Add(BuildEmpty());
                    break;
                case SoundEvent sound:
                    _eventEditor = new EventEditor(sound);
                    _main.Add(_eventEditor);
                    break;
                default:
                    _main.Add(new AssetEditor(asset));
                    break;
            }
        }

        private VisualElement BuildEmpty()
        {
            var empty = Ui.Box("rs-empty");
            empty.Add(Glyphs.AssetImage("Resonance", "rs-asset-icon--large"));
            empty.Add(Ui.Styled("Design your game's sound", "rs-empty-title"));
            empty.Add(Ui.Styled(
                "Pick an asset on the left, or drag audio clips from the Project window onto the list to build a Sound Event. " +
                "Add a Sound Emitter component to any GameObject to play it, no code needed.", "rs-empty-text"));

            var buttons = Ui.Box("rs-preview-row");
            buttons.Add(Ui.TextButton(Glyphs.Add, "New Sound Event", () =>
            {
                var sound = ResonanceAssets.Create<SoundEvent>("New Sound Event");
                _browser.Rebuild();
                _browser.Select(sound);
            }, "rs-button--primary"));
            buttons.Add(Ui.TextButton(Glyphs.Bus, "Create Default Mix", () =>
            {
                ResonanceAssets.CreateDefaultMix();
                _browser.Rebuild();
            }));
            buttons.Add(Ui.TextButton(Glyphs.Docs, "Getting Started", () => Application.OpenURL(StudioWindow.DocumentationUrl)));
            empty.Add(buttons);
            return empty;
        }

        #endregion
    }

    internal sealed class AssetEditor : VisualElement
    {
        public AssetEditor(ScriptableObject asset)
        {
            style.flexGrow = 1f;
            var kind = ResonanceAssets.KindOf(asset);
            Add(Toolbar.Build(asset, kind?.Icon ?? "Resonance", null));

            var scroll = new ScrollView { style = { flexGrow = 1f } };
            scroll.AddToClassList("rs-props");
            var content = Ui.Box("rs-props-content");
            var live = LiveControls.Build(asset);
            if (live != null)
                content.Add(live);
            content.Add(Ui.GroupTitle(Glyphs.Settings, "Settings"));
            content.Add(new UnityEditor.UIElements.InspectorElement(asset));
            scroll.Add(content);
            Add(scroll);
        }
    }

    internal static class Toolbar
    {
        public static VisualElement Build(ScriptableObject asset, string icon, VisualElement extra)
        {
            var bar = Ui.Box("rs-toolbar");
            bar.Add(Glyphs.AssetImage(icon, "rs-asset-icon"));
            var name = new TextField { value = asset.name, isDelayed = true, tooltip = "Rename the asset" };
            name.style.minWidth = 200f;
            name.RegisterValueChangedCallback(evt =>
            {
                var result = AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(asset), evt.newValue);
                if (!string.IsNullOrEmpty(result))
                {
                    Debug.LogWarning(result);
                    name.SetValueWithoutNotify(asset.name);
                }
            });
            bar.Add(name);
            if (extra != null)
                bar.Add(extra);
            bar.Add(Ui.Spacer());
            bar.Add(Ui.IconButton(Glyphs.Ping, "Show in the Project window", () => EditorGUIUtility.PingObject(asset), "rs-button--flat"));
            return bar;
        }
    }
}

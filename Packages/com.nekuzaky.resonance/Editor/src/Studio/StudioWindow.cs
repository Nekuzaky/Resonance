using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.Resonance.Editor
{
    internal interface IStudioView
    {
        VisualElement Root { get; }
        void Refresh();
        void ProjectChanged();
    }

    internal sealed class StudioWindow : EditorWindow
    {
        #region Public

        public const string Title = "Resonance Studio";
        public const string RepositoryUrl = "https://github.com/Nekuzaky/Resonance";
        public const string DocumentationUrl = RepositoryUrl + "/blob/main/Packages/com.nekuzaky.resonance/Documentation~/index.md";
        public const string CoffeeUrl = "https://buymeacoffee.com/nekuzaky";

        public const int EventsTab = 0;
        public const int MixerTab = 1;
        public const int MusicTab = 2;
        public const int ProfilerTab = 3;

        [MenuItem("Tools/Resonance/Resonance Studio %&r", priority = 1)]
        [MenuItem("Window/Audio/Resonance Studio", priority = 10)]
        public static void OpenFromMenu() => Open();

        [MenuItem("Tools/Resonance/Create Default Mix (Master, Music, SFX, Voice, Ambience)", priority = 20)]
        public static void CreateDefaultMix() => ResonanceAssets.CreateDefaultMix();

        [MenuItem("Tools/Resonance/Documentation", priority = 40)]
        public static void OpenDocumentation() => Application.OpenURL(DocumentationUrl);

        public static StudioWindow Open()
        {
            var window = GetWindow<StudioWindow>();
            window.minSize = new Vector2(760f, 420f);
            window.Show();
            return window;
        }

        public static void Open(ScriptableObject asset)
        {
            var window = Open();
            if (asset is MusicTrack track)
            {
                window.ShowTab(MusicTab);
                window._music.Select(track);
                return;
            }
            window.ShowTab(EventsTab);
            window._events.Select(asset);
        }

        [OnOpenAsset]
        public static bool OnOpenAsset(EntityId entityId, int line)
        {
            var asset = EditorUtility.EntityIdToObject(entityId) as ScriptableObject;
            if (asset is not SoundEvent && asset is not MusicTrack)
                return false;
            Open(asset);
            return true;
        }

        public void ShowTab(int index)
        {
            _tab = Mathf.Clamp(index, 0, _views.Count - 1);
            for (var i = 0; i < _views.Count; i++)
            {
                _views[i].Root.style.display = i == _tab ? DisplayStyle.Flex : DisplayStyle.None;
                _tabs[i].EnableInClassList("rs-tab--active", i == _tab);
            }
            _views[_tab].Refresh();
        }

        #endregion


        #region Private and Protected

        [SerializeField] private int _tab;

        private readonly List<IStudioView> _views = new();
        private readonly List<Button> _tabs = new();
        private EventsView _events;
        private MusicView _music;
        private Label _modePill;

        #endregion


        #region Unity Callbacks

        private void OnEnable() => titleContent = new GUIContent(Title, Glyphs.AssetIcon("Resonance"), "Game audio middleware");

        private void CreateGUI()
        {
            var root = rootVisualElement;
            EnsureStyle();
            root.AddToClassList("rs-root");

            _events = new EventsView();
            _music = new MusicView();
            _views.Clear();
            _views.Add(_events);
            _views.Add(new MixerView());
            _views.Add(_music);
            _views.Add(new ProfilerView());

            root.Add(BuildHeader());
            var body = Ui.Box("rs-body");
            foreach (var view in _views)
            {
                view.Root.style.flexGrow = 1f;
                body.Add(view.Root);
            }
            root.Add(body);
            ShowTab(_tab);
            root.schedule.Execute(Tick).Every(100);
        }

        private void OnProjectChange()
        {
            foreach (var view in _views)
                view.ProjectChanged();
        }

        #endregion


        #region Private and Protected

        private VisualElement BuildHeader()
        {
            var header = Ui.Box("rs-header");
            header.Add(Glyphs.Icon(Glyphs.Logo, "rs-icon--title"));
            header.Add(Ui.Styled("Resonance", "rs-title"));
            AddTab(header, Glyphs.Event, "Events", EventsTab);
            AddTab(header, Glyphs.Bus, "Mixer", MixerTab);
            AddTab(header, Glyphs.Music, "Music", MusicTab);
            AddTab(header, Glyphs.Profiler, "Profiler", ProfilerTab);
            header.Add(Ui.Spacer());
            _modePill = Ui.Styled("Preview", "rs-pill");
            _modePill.tooltip = "Which engine the Mixer, Music and Profiler tabs show.";
            header.Add(_modePill);
            header.Add(Ui.IconButton(Glyphs.Docs, "Documentation", OpenDocumentation, "rs-button--flat"));
            header.Add(Ui.IconButton(Glyphs.GitHub, "GitHub repository", () => Application.OpenURL(RepositoryUrl), "rs-button--flat"));
            header.Add(Ui.IconButton(Glyphs.Coffee, "Support the project", () => Application.OpenURL(CoffeeUrl), "rs-button--flat"));
            return header;
        }

        private void AddTab(VisualElement header, string glyph, string text, int index)
        {
            var tab = new Button(() => ShowTab(index));
            tab.AddToClassList("rs-tab");
            tab.Add(Glyphs.Icon(glyph));
            tab.Add(new Label(text));
            header.Add(tab);
            _tabs.Add(tab);
        }

        private void EnsureStyle()
        {
            var sheet = Glyphs.StyleSheet;
            if (sheet != null && !rootVisualElement.styleSheets.Contains(sheet))
                rootVisualElement.styleSheets.Add(sheet);
        }

        private void Tick()
        {
            if (_views.Count == 0)
                return;
            EnsureStyle();
            var playing = Application.isPlaying;
            _modePill.text = playing ? "Play Mode" : "Preview";
            _modePill.EnableInClassList("rs-pill--live", playing);
            _views[_tab].Refresh();
        }

        #endregion
    }
}

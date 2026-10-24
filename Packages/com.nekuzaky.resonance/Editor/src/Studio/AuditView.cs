using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.Resonance.Editor
{
    internal sealed class AuditView : IStudioView
    {
        #region Public

        public VisualElement Root { get; }

        public AuditView()
        {
            Root = new ScrollView(ScrollViewMode.Vertical);
            Root.AddToClassList("rs-profiler");
            var toolbar = Ui.Box("rs-preview-row");
            toolbar.Add(Ui.TextButton(Glyphs.Search, "Check Project", () => Rebuild(false), "rs-button--primary"));
            toolbar.Add(Ui.TextButton(Glyphs.Hierarchy, "Check With References", () => Rebuild(true)));
            toolbar.Add(Ui.TextButton(Glyphs.Magic, "Bulk Import Clips", BulkImportWindow.Open));
            Root.Add(toolbar);
            Root.Add(Ui.Help("Check With References also scans scenes, prefabs and assets to find sounds nothing uses. It can take a while on large projects."));
            _content = new VisualElement();
            Root.Add(_content);
            Rebuild(false);
        }

        public void Refresh()
        {
        }

        public void ProjectChanged()
        {
        }

        #endregion


        #region Private and Protected

        private readonly VisualElement _content;

        private void Rebuild(bool withReferences)
        {
            _content.Clear();
            var problems = ProjectAudit.Run(withReferences ? ReferenceIndex.Build() : null);
            _content.Add(Ui.GroupTitle(Glyphs.Warning, $"Problems ({problems.Count})"));
            if (problems.Count == 0)
                _content.Add(Ui.Help("No problem found in Sound Events, Music Tracks and Buses."));
            foreach (var problem in problems)
                _content.Add(ProblemRow(problem));
            AddImportSuggestions();
        }

        private static VisualElement ProblemRow(Problem problem)
        {
            var row = Ui.Box("rs-table-row");
            var glyph = problem.Severity == Severity.Error ? Glyphs.Rejected : problem.Severity == Severity.Warning ? Glyphs.Warning : Glyphs.Info;
            var tint = problem.Severity == Severity.Error ? "rs-icon--bad" : problem.Severity == Severity.Warning ? "rs-icon--warn" : "rs-icon--dim";
            row.Add(Glyphs.Icon(glyph, "rs-icon--small", tint));
            var kind = ResonanceAssets.KindOf(problem.Asset);
            row.Add(Glyphs.AssetImage(kind?.Icon ?? "Resonance", "rs-asset-icon"));
            var name = Ui.Styled(problem.Asset.name, "rs-cell");
            name.style.width = 180f;
            row.Add(name);
            row.Add(Ui.Styled(problem.Message, "rs-cell", "rs-cell--dim"));
            row.RegisterCallback<ClickEvent>(_ => StudioWindow.Open(problem.Asset));
            row.tooltip = "Open in the Studio";
            return row;
        }

        private void AddImportSuggestions()
        {
            var suggestions = ImportAudit.Analyze();
            _content.Add(Ui.GroupTitle(Glyphs.Memory, $"Import Settings ({suggestions.Count})"));
            if (suggestions.Count == 0)
            {
                _content.Add(Ui.Help("Every clip used by Resonance has the suggested load type."));
                return;
            }
            _content.Add(Ui.TextButton(Glyphs.Magic, "Apply All Suggestions", () =>
            {
                ImportAudit.Apply(suggestions);
                Rebuild(false);
            }));
            foreach (var suggestion in suggestions)
                _content.Add(SuggestionRow(suggestion));
        }

        private static VisualElement SuggestionRow(ImportSuggestion suggestion)
        {
            var row = Ui.Box("rs-table-row");
            var name = Ui.Styled(suggestion.Clip.name, "rs-cell");
            name.style.width = 180f;
            row.Add(name);
            var target = Ui.Styled(suggestion.Target, "rs-cell", "rs-cell--dim");
            target.style.width = 70f;
            row.Add(target);
            var change = Ui.Styled(string.IsNullOrEmpty(suggestion.Platform)
                ? $"{suggestion.CurrentLoad} > {suggestion.SuggestedLoad}"
                : $"add override: {suggestion.SuggestedLoad}, Vorbis {suggestion.SuggestedQuality * 100f:0}%", "rs-cell", "rs-cell--warn");
            change.style.width = 300f;
            row.Add(change);
            row.Add(Ui.Styled(suggestion.Reason, "rs-cell", "rs-cell--dim"));
            row.RegisterCallback<ClickEvent>(_ => EditorGUIUtility.PingObject(suggestion.Clip));
            return row;
        }

        #endregion
    }
}

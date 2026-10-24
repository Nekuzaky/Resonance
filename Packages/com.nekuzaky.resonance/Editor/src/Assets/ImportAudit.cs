using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Nekuzaky.Resonance.Editor
{
    internal sealed class ImportSuggestion
    {
        public AudioClip Clip;
        public string Path;
        public AudioClipLoadType CurrentLoad;
        public AudioClipLoadType SuggestedLoad;
        public bool CurrentPreload;
        public bool SuggestedPreload;
        public string Reason;
    }

    internal static class ImportAudit
    {
        #region Public

        public const float StreamingSeconds = 15f;
        public const float DecompressSeconds = 3f;

        [MenuItem("Tools/Resonance/Optimize Audio Import Settings", priority = 21)]
        public static void OptimizeFromMenu()
        {
            var suggestions = Analyze();
            if (suggestions.Count == 0)
            {
                EditorUtility.DisplayDialog("Resonance", "Every clip used by Resonance already has the suggested import settings.", "OK");
                return;
            }
            if (EditorUtility.DisplayDialog("Resonance", $"{suggestions.Count} clips can load better. Apply the suggested import settings?", "Apply", "Cancel"))
                Apply(suggestions);
        }

        public static List<ImportSuggestion> Analyze()
        {
            var music = new HashSet<AudioClip>();
            foreach (var track in ResonanceAssets.FindAll<MusicTrack>())
                SoundBank.CollectTrackClips(track, music);
            var all = new HashSet<AudioClip>(music);
            foreach (var sound in ResonanceAssets.FindAll<SoundEvent>())
                SoundBank.CollectEventClips(sound, all);
            var banked = new HashSet<AudioClip>();
            foreach (var bank in ResonanceAssets.FindAll<SoundBank>())
                bank.CollectClips(banked);

            var suggestions = new List<ImportSuggestion>();
            foreach (var clip in all)
            {
                var suggestion = Suggest(clip, music.Contains(clip), banked.Contains(clip));
                if (suggestion != null)
                    suggestions.Add(suggestion);
            }
            suggestions.Sort((a, b) => string.CompareOrdinal(a.Path, b.Path));
            return suggestions;
        }

        public static void Apply(IReadOnlyList<ImportSuggestion> suggestions)
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var suggestion in suggestions)
                {
                    if (AssetImporter.GetAtPath(suggestion.Path) is not AudioImporter importer)
                        continue;
                    var settings = importer.defaultSampleSettings;
                    settings.loadType = suggestion.SuggestedLoad;
                    settings.preloadAudioData = suggestion.SuggestedPreload;
                    importer.defaultSampleSettings = settings;
                    importer.SaveAndReimport();
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        public static AudioClipLoadType SuggestedLoad(float seconds, bool isMusic)
        {
            if (isMusic)
                return AudioClipLoadType.CompressedInMemory;
            if (seconds > StreamingSeconds)
                return AudioClipLoadType.Streaming;
            return seconds > DecompressSeconds ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
        }

        #endregion


        #region Private and Protected

        private static ImportSuggestion Suggest(AudioClip clip, bool isMusic, bool isBanked)
        {
            var path = AssetDatabase.GetAssetPath(clip);
            if (AssetImporter.GetAtPath(path) is not AudioImporter importer)
                return null;
            var settings = importer.defaultSampleSettings;
            var load = SuggestedLoad(clip.length, isMusic);
            var preload = !isBanked && load != AudioClipLoadType.Streaming;
            if (settings.loadType == load && settings.preloadAudioData == preload)
                return null;
            return new ImportSuggestion
            {
                Clip = clip,
                Path = path,
                CurrentLoad = settings.loadType,
                SuggestedLoad = load,
                CurrentPreload = settings.preloadAudioData,
                SuggestedPreload = preload,
                Reason = Reason(clip.length, isMusic, isBanked)
            };
        }

        private static string Reason(float seconds, bool isMusic, bool isBanked)
        {
            var load = isMusic
                ? "music stays compressed in memory so transitions land on the beat"
                : seconds > StreamingSeconds ? $"longer than {StreamingSeconds:0} s, streamed from disk"
                : seconds > DecompressSeconds ? "medium length, compressed in memory" : "short, decompressed for instant playback";
            return isBanked ? load + "; loaded by a Sound Bank" : load;
        }

        #endregion
    }
}

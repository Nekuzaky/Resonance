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
        public string Platform;
        public string[] Platforms;
        public float SuggestedQuality = -1f;

        public string Target => Platforms is { Length: > 0 } ? string.Join(" + ", Platforms) : "Default";
    }

    internal static class ImportAudit
    {
        #region Public

        public const float StreamingSeconds = 15f;
        public const float DecompressSeconds = 3f;
        public const float MobileQuality = 0.35f;
        public const float MobileMusicQuality = 0.5f;

        public static readonly string[] MobilePlatforms = { "Android", "iOS" };

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
                AddMobile(clip, music.Contains(clip), banked.Contains(clip), suggestions);
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
                    if (AssetImporter.GetAtPath(suggestion.Path) is AudioImporter importer)
                        ApplyOne(importer, suggestion);
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

        private static void ApplyOne(AudioImporter importer, ImportSuggestion suggestion)
        {
            if (suggestion.Platforms is { Length: > 0 })
            {
                foreach (var platform in suggestion.Platforms)
                {
                    var mobile = importer.GetOverrideSampleSettings(platform);
                    mobile.loadType = suggestion.SuggestedLoad;
                    mobile.preloadAudioData = suggestion.SuggestedPreload;
                    mobile.compressionFormat = AudioCompressionFormat.Vorbis;
                    mobile.quality = suggestion.SuggestedQuality;
                    mobile.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
                    importer.SetOverrideSampleSettings(platform, mobile);
                }
            }
            else
            {
                var settings = importer.defaultSampleSettings;
                settings.loadType = suggestion.SuggestedLoad;
                settings.preloadAudioData = suggestion.SuggestedPreload;
                importer.defaultSampleSettings = settings;
            }
            importer.SaveAndReimport();
        }

        public static float SuggestedMobileQuality(bool isMusic) => isMusic ? MobileMusicQuality : MobileQuality;

        private static void AddMobile(AudioClip clip, bool isMusic, bool isBanked, List<ImportSuggestion> suggestions)
        {
            var path = AssetDatabase.GetAssetPath(clip);
            if (AssetImporter.GetAtPath(path) is not AudioImporter importer)
                return;
            var missing = new List<string>();
            foreach (var platform in MobilePlatforms)
            {
                if (!importer.ContainsSampleSettingsOverride(platform))
                    missing.Add(platform);
            }
            if (missing.Count == 0)
                return;
            var load = SuggestedLoad(clip.length, isMusic);
            suggestions.Add(new ImportSuggestion
            {
                Clip = clip,
                Path = path,
                Platform = missing[0],
                Platforms = missing.ToArray(),
                CurrentLoad = importer.defaultSampleSettings.loadType,
                SuggestedLoad = load,
                SuggestedPreload = !isBanked && load != AudioClipLoadType.Streaming,
                SuggestedQuality = SuggestedMobileQuality(isMusic),
                Reason = $"smaller on phones: Vorbis at {SuggestedMobileQuality(isMusic) * 100f:0}% quality, optimized sample rate"
            });
        }

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

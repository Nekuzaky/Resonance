using System.Collections.Generic;
using Nekuzaky.Resonance.Editor;
using UnityEditor;
using UnityEngine;

namespace Nekuzaky.Resonance.Dev
{
    internal static partial class DemoBuilder
    {
        #region Public

        public const string Root = "Assets/Demo";
        public const string AudioFolder = Root + "/Audio";
        public const string AssetFolder = Root + "/Resonance";
        public const string ScenePath = Root + "/Resonance Demo.unity";

        [MenuItem("Tools/Resonance/Dev/Rebuild Demo", priority = 200)]
        public static void Build()
        {
            WriteAudio();
            var assets = BuildAssets();
            BuildScene(assets);
            AssetDatabase.SaveAssets();
            Debug.Log("Resonance demo rebuilt.");
        }

        #endregion


        #region Private and Protected

        private sealed class DemoAssets
        {
            public AudioBus Master;
            public AudioBus Music;
            public AudioBus Sfx;
            public AudioBus Voice;
            public AudioBus Ambience;
            public SoundParameter CarSpeed;
            public SwitchGroup Surface;
            public StateGroup Game;
            public MixSnapshot Pause;
            public SoundEvent Footsteps;
            public SoundEvent Wind;
            public SoundEvent Engine;
            public SoundEvent Coin;
            public SoundEvent Click;
            public SoundEvent Narrator;
            public MusicTrack Score;
        }

        private static void WriteAudio()
        {
            for (var i = 1; i <= 4; i++)
            {
                Write($"footstep_grass_{i}", Footstep(0.3f, 10 + i, 0.18f, 0.25f));
                Write($"footstep_stone_{i}", Footstep(0.18f, 20 + i, 0.75f, 0.6f));
            }
            Write("wind_loop", Synth.Wind(Synth.Buffer(6.5f), 3));
            Write("engine_low", Normalized(data => Synth.Engine(data, 55f, 0.05f, 4), 2f, 0.7f));
            Write("engine_high", Normalized(data => Synth.Engine(data, 110f, 0.12f, 5), 2f, 0.7f));
            Write("coin", Normalized(data => Synth.Tones(data, new[] { 987.77f, 1318.51f }, 0.08f, 6f, true), 0.45f, 0.6f));
            Write("ui_click", Normalized(data => Synth.Tones(data, new[] { 2000f }, 0.05f, 80f, false), 0.06f, 0.5f));
            Write("voice_line", Normalized(data => Synth.Voice(data, 8), 2.6f, 0.75f));
            Write("music_explore", MusicSynth.Explore());
            Write("music_combat", MusicSynth.Combat());
            Write("stinger_alert", MusicSynth.Stinger());
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { AudioFolder }))
                Configure(AssetDatabase.GUIDToAssetPath(guid));
        }

        private static float[] Footstep(float seconds, int seed, float brightness, float thump)
        {
            var data = Synth.Buffer(seconds);
            Synth.Footstep(data, seed, brightness, thump);
            Synth.Normalize(data, 0.8f);
            return data;
        }

        private static float[] Normalized(System.Action<float[]> fill, float seconds, float peak)
        {
            var data = Synth.Buffer(seconds);
            fill(data);
            Synth.Normalize(data, peak);
            return data;
        }

        private static void Write(string name, float[] data) => Synth.WriteWav($"{AudioFolder}/{name}.wav", data);

        private static void Configure(string path)
        {
            if (AssetImporter.GetAtPath(path) is not AudioImporter importer)
                return;
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.7f;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            importer.SaveAndReimport();
        }

        private static AudioClip ClipAt(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/{name}.wav");

        private static T Fresh<T>(string name) where T : ScriptableObject
        {
            var path = $"{AssetFolder}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
                AssetDatabase.DeleteAsset(path);
            var asset = (T)ResonanceAssets.Create(typeof(T), name, AssetFolder);
            return asset;
        }

        private static ClipNode Clip(string name, float volume = 0f) => new() { name = name, clip = ClipAt(name), volume = volume };

        private static RandomNode RandomOf(string name, IEnumerable<string> clips)
        {
            var node = new RandomNode { name = name, avoidRepeat = 1 };
            foreach (var clip in clips)
                node.children.Add(Clip(clip));
            return node;
        }

        #endregion
    }
}

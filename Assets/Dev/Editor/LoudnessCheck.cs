using System.IO;
using System.Text;
using Nekuzaky.Resonance.Editor;
using UnityEditor;
using UnityEngine;

namespace Nekuzaky.Resonance.Dev
{
    internal static class LoudnessCheck
    {
        public const string Output = "Tools/Output/loudness.tsv";

        public static void Run()
        {
            var report = new StringBuilder();
            foreach (var guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Demo/Audio" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var measure = Loudness.Measure(AssetDatabase.LoadAssetAtPath<AudioClip>(path));
                if (measure.Valid)
                    report.Append(path).Append('\t').Append(measure.Loudness.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)).Append('\n');
            }
            File.WriteAllText(Output, report.ToString());
            Debug.Log($"[LoudnessCheck] wrote {Output}");
            EditorApplication.Exit(0);
        }
    }
}

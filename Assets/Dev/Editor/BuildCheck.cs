using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Nekuzaky.Resonance.Dev
{
    internal static class BuildCheck
    {
        public const string Output = "Tools/Output/build/windows/ResonanceDemo.exe";

        public const string BankPath = "Assets/Demo/Resonance/Demo Bank.asset";

        public static void Run()
        {
            if (!BuildAddressables())
            {
                EditorApplication.Exit(1);
                return;
            }
            Build(BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone, Output);
        }

        public static void RunAndroid() => CompileScripts(BuildTarget.Android, BuildTargetGroup.Android);

        public static void RunWebGL() => CompileScripts(BuildTarget.WebGL, BuildTargetGroup.WebGL);

        private static void CompileScripts(BuildTarget target, BuildTargetGroup group)
        {
            var settings = new UnityEditor.Build.Player.ScriptCompilationSettings { target = target, group = group };
            var output = $"Tools/Output/build/scripts-{target}";
            Directory.CreateDirectory(output);
            var result = UnityEditor.Build.Player.PlayerBuildInterface.CompilePlayerScripts(settings, output);
            var compiled = result.assemblies != null && result.assemblies.Contains("Nekuzaky.Resonance.dll");
            Debug.Log($"[BuildCheck] {target} player scripts: {(compiled ? "compiled" : "FAILED")}, {result.assemblies?.Count ?? 0} assemblies");
            EditorApplication.Exit(compiled ? 0 : 1);
        }

        private static bool BuildAddressables()
        {
            var settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
            var entry = settings.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(BankPath), settings.DefaultGroup);
            entry.address = "Demo Bank";
            AssetDatabase.SaveAssets();
            AddressableAssetSettings.BuildPlayerContent(out var result);
            var built = string.IsNullOrEmpty(result.Error);
            Debug.Log($"[BuildCheck] Addressables content: {(built ? "built" : result.Error)} in {result.Duration:0.0} s");
            return built;
        }

        private static void Build(BuildTarget target, BuildTargetGroup group, string output)
        {
            var options = new BuildPlayerOptions
            {
                scenes = new[] { DemoBuilder.ScenePath },
                locationPathName = output,
                target = target,
                targetGroup = group,
                options = BuildOptions.None
            };
            Directory.CreateDirectory(Path.GetDirectoryName(output) ?? ".");
            var summary = BuildPipeline.BuildPlayer(options).summary;
            Debug.Log($"[BuildCheck] {target} {summary.result}: {summary.totalErrors} errors, {summary.totalWarnings} warnings, " +
                      $"{summary.totalSize / 1048576d:0.0} MB in {summary.totalTime.TotalSeconds:0} s");
            EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}

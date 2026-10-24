using System.Diagnostics;
using System.IO;
using Nekuzaky.Resonance.Editor;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Nekuzaky.Resonance.Dev
{
    internal static class RemoteCheck
    {
        public const string Output = "Tools/Output/build/remote/ResonanceDemo.exe";

        private static double _deadline;
        private static int _mostSounds;
        private static Process _player;

        public static void Run()
        {
            var options = new BuildPlayerOptions
            {
                scenes = new[] { DemoBuilder.ScenePath },
                locationPathName = Output,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.Development | BuildOptions.ConnectWithProfiler
            };
            Directory.CreateDirectory(Path.GetDirectoryName(Output) ?? ".");
            var summary = BuildPipeline.BuildPlayer(options).summary;
            Debug.Log($"[Remote] build {summary.result}");
            if (summary.result != BuildResult.Succeeded)
            {
                EditorApplication.Exit(1);
                return;
            }
            var log = Path.GetFullPath("Tools/Output/logs/remote-player.log");
            _player = Process.Start(Path.GetFullPath(Output), $"-resonanceSmoke -screen-width 640 -screen-height 360 -screen-fullscreen 0 -logFile \"{log}\"");
            _deadline = EditorApplication.timeSinceStartup + 45.0;
            EditorApplication.update += Wait;
        }

        private static void Wait()
        {
            if (RemoteSession.FramesReceived > 0)
                _mostSounds = Mathf.Max(_mostSounds, RemoteSession.Frame.Sounds.Count);
            var playerDone = _player == null || _player.HasExited;
            if (!playerDone && EditorApplication.timeSinceStartup < _deadline)
                return;
            EditorApplication.update -= Wait;
            var frames = RemoteSession.FramesReceived;
            Debug.Log($"[Remote] frames={frames} mostSounds={_mostSounds} platform={RemoteSession.Frame.Platform} plays={RemoteSession.Frame.Plays}");
            if (!playerDone)
                _player.Kill();
            EditorApplication.Exit(frames >= 10 && _mostSounds > 0 ? 0 : 1);
        }
    }
}

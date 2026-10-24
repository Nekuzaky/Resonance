using UnityEditor;
using UnityEditor.Networking.PlayerConnection;
using UnityEngine.Networking.PlayerConnection;

namespace Nekuzaky.Resonance.Editor
{
    [InitializeOnLoad]
    internal static class RemoteSession
    {
        #region Public

        public const double Timeout = 2.0;

        public static RemoteFrame Frame { get; } = new();

        public static int FramesReceived { get; private set; }

        public static int PlayerId { get; private set; } = -1;

        public static bool Live => FramesReceived > 0 && EditorApplication.timeSinceStartup - _lastReceived < Timeout;

        public static int Version { get; private set; }

        #endregion


        #region Private and Protected

        private static double _lastReceived = double.NegativeInfinity;

        static RemoteSession()
        {
            EditorConnection.instance.Initialize();
            EditorConnection.instance.Register(RemoteFrame.MessageId, OnMessage);
            AssemblyReloadEvents.beforeAssemblyReload += () => EditorConnection.instance.Unregister(RemoteFrame.MessageId, OnMessage);
        }

        private static void OnMessage(MessageEventArgs args)
        {
            if (!Frame.Read(args.data))
                return;
            PlayerId = args.playerId;
            FramesReceived++;
            Version++;
            _lastReceived = EditorApplication.timeSinceStartup;
        }

        #endregion
    }
}

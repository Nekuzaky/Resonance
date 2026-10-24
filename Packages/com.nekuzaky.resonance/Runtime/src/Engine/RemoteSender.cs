#if DEVELOPMENT_BUILD && !UNITY_EDITOR
using UnityEngine.Networking.PlayerConnection;
#endif

namespace Nekuzaky.Resonance
{
    internal sealed class RemoteSender
    {
        public const float Interval = 0.2f;

#if DEVELOPMENT_BUILD && !UNITY_EDITOR
        private readonly RemoteFrame _frame = new();
        private float _next;

        public void Tick(ResonanceEngine engine)
        {
            if (engine.Time < _next || !PlayerConnection.instance.isConnected)
                return;
            _next = engine.Time + Interval;
            engine.Metering = true;
            _frame.Capture(engine);
            PlayerConnection.instance.Send(RemoteFrame.MessageId, _frame.Write());
        }
#else
        public void Tick(ResonanceEngine engine)
        {
        }
#endif
    }
}

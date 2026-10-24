using UnityEngine;

namespace Nekuzaky.Resonance.Dev
{
    [AddComponentMenu("")]
    internal sealed class ListenerTap : MonoBehaviour
    {
        #region Public

        public float[] Buffer;
        public volatile int Count;
        public volatile int Channels;

        #endregion


        #region Unity Callbacks

        private void OnAudioFilterRead(float[] data, int channels)
        {
            Channels = channels;
            var count = Count;
            var room = Buffer != null ? Mathf.Min(data.Length, Buffer.Length - count) : 0;
            if (room > 0)
                System.Array.Copy(data, 0, Buffer, count, room);
            Count = count + room;
            System.Array.Clear(data, 0, data.Length);
        }

        #endregion
    }
}

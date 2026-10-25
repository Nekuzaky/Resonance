using System.Threading;
using UnityEngine;

namespace Nekuzaky.Resonance
{
    [AddComponentMenu("")]
    [ExecuteAlways]
    internal sealed class TailFader : MonoBehaviour
    {
        #region Public

        public const float MaxGain = 16f;

        public float Gain => Volatile.Read(ref _target);

        public void Set(float gain) => Volatile.Write(ref _target, Mathf.Clamp(gain, 0f, MaxGain));

        public void Snap(float gain)
        {
            Set(gain);
            Interlocked.Exchange(ref _snap, 1);
        }

        public void Process(float[] data, int channels)
        {
            if (data == null || channels <= 0)
                return;
            var target = Volatile.Read(ref _target);
            if (Interlocked.Exchange(ref _snap, 0) != 0)
                _current = target;
            var frames = data.Length / channels;
            if (frames == 0 || (target == 1f && _current == 1f))
                return;
            var gain = _current;
            var step = (target - gain) / frames;
            for (var frame = 0; frame < frames; frame++)
            {
                gain += step;
                var offset = frame * channels;
                for (var channel = 0; channel < channels; channel++)
                    data[offset + channel] *= gain;
            }
            _current = target;
        }

        #endregion


        #region Private and Protected

        private float _target = 1f;
        private float _current = 1f;
        private int _snap;

        #endregion


        #region Unity Callbacks

        private void OnAudioFilterRead(float[] data, int channels) => Process(data, channels);

        #endregion
    }
}

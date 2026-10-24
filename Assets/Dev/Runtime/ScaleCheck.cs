using UnityEngine;

namespace Nekuzaky.Resonance.Dev
{
    [AddComponentMenu("")]
    internal sealed class ScaleCheck : MonoBehaviour
    {
        #region Private and Protected

        private const int Sounds = 400;
        private const int WarmUpFrames = 10;
        private const int MeasuredFrames = 120;

        private SoundEvent _sound;
        private int _frame;
        private double _total;
        private double _worst;
        private int _real;
        private int _virtual;

        private void Finish()
        {
            var average = _total / MeasuredFrames;
            Smoke.Check(Resonance.Engine.CountPlaying(_sound, null) == Sounds, $"{Sounds} looping sounds play at once");
            Smoke.Check(average < 1.0, $"engine cost with {Sounds} sounds: {average:0.000} ms per frame on average, {_worst:0.000} ms at worst, " +
                                       $"{_real} real and {_virtual} virtual voices");
            Resonance.Stop(_sound, 0f);
            Smoke.Pending--;
        }

        #endregion


        #region Unity Callbacks

        private void Start()
        {
            Smoke.Pending++;
            var engine = Resonance.Engine;
            engine.MaxVoices = 512;
            _sound = Smoke.Event(Smoke.Tone("Scale Tone", 220f, 2f), -60f, true);
            for (var i = 0; i < Sounds; i++)
            {
                var emitter = new GameObject("Scale Emitter");
                emitter.transform.position = new Vector3(i % 20 * 3f - 30f, 0f, i / 20 * 3f - 30f);
                Resonance.Play(_sound, emitter);
            }
        }

        private void Update()
        {
            _frame++;
            if (_frame <= WarmUpFrames || _frame > WarmUpFrames + MeasuredFrames)
                return;
            var stats = Resonance.Engine.Stats;
            _total += stats.TickMilliseconds;
            _worst = Mathf.Max((float)_worst, (float)stats.TickMilliseconds);
            _real = stats.RealVoices;
            _virtual = stats.VirtualVoices;
            if (_frame == WarmUpFrames + MeasuredFrames)
                Finish();
        }

        #endregion
    }
}

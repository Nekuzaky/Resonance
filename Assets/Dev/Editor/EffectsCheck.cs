using System.Collections.Generic;
using System.Text;
using Nekuzaky.Resonance.Editor;
using UnityEditor;
using UnityEngine;

namespace Nekuzaky.Resonance.Dev
{
    internal static class EffectsCheck
    {
        #region Public

        public static void Run()
        {
            _rate = AudioSettings.outputSampleRate;
            var listener = new GameObject("Effects Check Listener") { hideFlags = HideFlags.HideAndDontSave };
            listener.AddComponent<AudioListener>();
            _tap = listener.AddComponent<ListenerTap>();
            _tap.Buffer = new float[_rate * 8 * Seconds];
            _steps.Clear();
            AddSteps();
            _next = 0;
            _nextAt = 0;
            _started = EditorApplication.timeSinceStartup;
            EditorApplication.update += Update;
        }

        #endregion


        #region Private and Protected

        private const int Seconds = 16;
        private const float Spacing = 1.4f;

        private sealed class Step
        {
            public string Name;
            public System.Action Play;
            public int Start;
        }

        private static readonly List<Step> _steps = new();
        private static readonly List<Object> _created = new();
        private static ListenerTap _tap;
        private static int _rate;
        private static int _next;
        private static int _nextAt;
        private static double _started;
        private static float _dryRms;
        private static bool _failed;

        private static void AddSteps()
        {
            var engine = PreviewEngine.Engine;
            var sine = Clip("sine", 0.3f, false, 0.5f);
            var burst = Clip("burst", 0.1f, true, 0.5f);
            Add("dry", engine, Sound("Dry", sine, Bus("Dry")));
            var low = Bus("LowPass");
            low.AddEffect(BusEffectType.LowPass).SetValue(BusEffects.FilterCutoff, 200f);
            Add("lowpass", engine, Sound("Low", sine, low));
            var echo = Bus("Delay");
            var delay = echo.AddEffect(BusEffectType.Delay);
            delay.SetValue(BusEffects.DelayTime, 300f);
            delay.SetValue(BusEffects.DelayFeedback, 0f);
            delay.SetValue(BusEffects.DelayWet, 1f);
            Add("delay", engine, Sound("Echo", burst, echo));
            var room = Bus("Reverb");
            var reverb = room.AddEffect(BusEffectType.Reverb);
            reverb.SetValue(BusEffects.ReverbMix, 1f);
            reverb.SetValue(BusEffects.ReverbDecay, 2f);
            Add("reverb", engine, Sound("Room", burst, room));
            var crunch = Bus("Distortion");
            crunch.volume = -20f;
            var distortion = crunch.AddEffect(BusEffectType.Distortion);
            distortion.SetValue(BusEffects.DistortionDrive, 12f / EffectDsp.MaxDriveDb);
            distortion.SetValue(BusEffects.DistortionOutput, 0f);
            Add("distortion", engine, Sound("Crunch", sine, crunch));
            AddMusic(engine, sine);
        }

        private static void AddMusic(ResonanceEngine engine, AudioClip sine)
        {
            var music = Bus("Music");
            music.AddEffect(BusEffectType.LowPass).SetValue(BusEffects.FilterCutoff, 200f);
            var track = ScriptableObject.CreateInstance<MusicTrack>();
            _created.Add(track);
            track.bus = music;
            var segment = new MusicSegment { name = "Loop", clip = Clip("silence", 1f, false, 0f) };
            segment.layers.Add(new MusicLayer { name = "Tone", clip = sine });
            track.segments.Add(segment);
            _steps.Add(new Step { Name = "music layer", Play = () => engine.Music.Play(track, AudioSettings.dspTime) });
        }

        private static void Add(string name, ResonanceEngine engine, SoundEvent sound) =>
            _steps.Add(new Step { Name = name, Play = () => engine.Play(sound, null, Vector3.zero) });

        private static AudioBus Bus(string name)
        {
            var bus = ScriptableObject.CreateInstance<AudioBus>();
            bus.name = name;
            _created.Add(bus);
            return bus;
        }

        private static SoundEvent Sound(string name, AudioClip clip, AudioBus bus)
        {
            var sound = ScriptableObject.CreateInstance<SoundEvent>();
            sound.name = name;
            sound.root = new ClipNode { name = name, clip = clip };
            sound.bus = bus;
            _created.Add(sound);
            return sound;
        }

        private static AudioClip Clip(string name, float seconds, bool noise, float amplitude)
        {
            var frames = (int)(seconds * _rate);
            var data = new float[frames];
            var random = new System.Random(3);
            for (var i = 0; i < frames; i++)
                data[i] = amplitude * (noise ? (float)(random.NextDouble() * 2.0 - 1.0) : Mathf.Sin(2f * Mathf.PI * 1000f * i / _rate));
            var clip = AudioClip.Create(name, frames, 1, _rate, false);
            clip.SetData(data, 0);
            _created.Add(clip);
            return clip;
        }

        private static void Update()
        {
            var channels = Mathf.Max(1, _tap.Channels);
            var count = _tap.Count;
            if (EditorApplication.timeSinceStartup - _started > Seconds + 10)
                Finish("timed out waiting for audio");
            else if (_next < _steps.Count && count >= _nextAt)
            {
                var step = _steps[_next];
                step.Start = count;
                step.Play();
                _next++;
                _nextAt = count + (int)(Spacing * _rate * channels);
            }
            else if (_next >= _steps.Count && count >= _nextAt)
                Finish(null);
        }

        private static void Finish(string error)
        {
            EditorApplication.update -= Update;
            PreviewEngine.Engine.Music.Stop(0f);
            var report = new StringBuilder();
            if (error != null)
            {
                _failed = true;
                report.Append(error);
            }
            else
                Analyze(report);
            Debug.Log($"[EffectsCheck] {(_failed ? "FAIL" : "PASS")} {report}");
            foreach (var item in _created)
            {
                if (item != null)
                    Object.DestroyImmediate(item);
            }
            EditorApplication.Exit(_failed ? 1 : 0);
        }

        private static void Analyze(StringBuilder report)
        {
            var channels = Mathf.Max(1, _tap.Channels);
            report.Append($"rate={_rate} channels={channels};");
            foreach (var step in _steps)
            {
                var onset = Onset(step.Start, channels);
                report.Append($" [{step.Name}] ");
                if (onset < 0)
                {
                    _failed = true;
                    report.Append("no sound;");
                    continue;
                }
                Judge(step.Name, onset, channels, report);
            }
        }

        private static void Judge(string name, int onset, int channels, StringBuilder report)
        {
            switch (name)
            {
                case "dry":
                    _dryRms = Rms(onset, 0.05f, 0.25f, channels);
                    report.Append($"rms {Db(_dryRms):0.0} dB;");
                    break;
                case "lowpass":
                case "music layer":
                    var low = Rms(onset, 0.05f, 0.25f, channels);
                    Expect(report, Db(low) < Db(_dryRms) - 15f, $"rms {Db(low):0.0} dB vs dry {Db(_dryRms):0.0} dB;");
                    break;
                case "delay":
                    var gap = Rms(onset, 0.16f, 0.27f, channels);
                    var repeat = Rms(onset, 0.31f, 0.38f, channels);
                    Expect(report, Db(repeat) > Db(gap) + 20f, $"gap {Db(gap):0.0} dB, echo after the clip ended {Db(repeat):0.0} dB;");
                    break;
                case "reverb":
                    var tail = Rms(onset, 0.4f, 0.8f, channels);
                    Expect(report, Db(tail) > -60f, $"tail 0.3-0.7 s after the clip ended {Db(tail):0.0} dB;");
                    break;
                case "distortion":
                    var peak = Peak(onset, 0.05f, 0.25f, channels);
                    Expect(report, peak > 0.08f && peak < 0.1f, $"peak {peak:0.000} with the bus at -20 dB (0.091 when the drive ignores the bus fader);");
                    break;
            }
        }

        private static void Expect(StringBuilder report, bool ok, string text)
        {
            _failed |= !ok;
            report.Append(ok ? "ok " : "FAILED ").Append(text);
        }

        private static int Onset(int start, int channels)
        {
            var end = Mathf.Min(_tap.Count, start + (int)(0.5f * _rate) * channels);
            for (var i = start; i < end; i += channels)
            {
                if (Mathf.Abs(_tap.Buffer[i]) > 0.005f)
                    return i - i % channels;
            }
            return -1;
        }

        private static float Rms(int onset, float from, float to, int channels)
        {
            var sum = 0.0;
            var count = 0;
            var end = Mathf.Min(_tap.Count, onset + (int)(to * _rate) * channels);
            for (var i = onset + (int)(from * _rate) * channels; i < end; i += channels)
            {
                sum += _tap.Buffer[i] * _tap.Buffer[i];
                count++;
            }
            return count == 0 ? 0f : (float)System.Math.Sqrt(sum / count);
        }

        private static float Peak(int onset, float from, float to, int channels)
        {
            var peak = 0f;
            var end = Mathf.Min(_tap.Count, onset + (int)(to * _rate) * channels);
            for (var i = onset + (int)(from * _rate) * channels; i < end; i += channels)
                peak = Mathf.Max(peak, Mathf.Abs(_tap.Buffer[i]));
            return peak;
        }

        private static float Db(float linear) => linear <= 1e-7f ? -140f : 20f * Mathf.Log10(linear);

        #endregion
    }
}

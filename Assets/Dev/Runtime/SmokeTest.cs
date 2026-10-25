using System;
using UnityEngine;

namespace Nekuzaky.Resonance.Dev
{
    [AddComponentMenu("")]
    internal sealed class SmokeTest : MonoBehaviour
    {
        #region Private and Protected

        private const string Flag = "-resonanceSmoke";

        private float _start;
        private int _step;
        private float _deepestDuck;
        private float _wettest;
        private float _mostOccluded;
        private int _subtitles;
        private bool _bridged;
        private int _beats;
        private int _markers;
        private float _loudestLayer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), Flag) < 0)
                return;
            var host = new GameObject("Resonance Smoke Test");
            DontDestroyOnLoad(host);
            host.AddComponent<SmokeTest>();
            host.AddComponent<TimelineCheck>();
            host.AddComponent<AddressablesCheck>();
            host.AddComponent<BusEffectsCheck>();
        }

        private static void Check(bool condition, string message) => Smoke.Check(condition, message);

        private static float MusicDuck(ResonanceEngine engine)
        {
            var track = engine.Music.Track;
            if (track == null || track.bus == null || !engine.Mixer.Buses.TryGetValue(track.bus, out var state))
                return 0f;
            return state.Duck;
        }

        private void Report(ResonanceEngine engine)
        {
            var stats = engine.Stats;
            var music = engine.Music.Current != null ? engine.Music.Current.name : "none";
            Debug.Log($"[Smoke] plays={stats.TotalPlays} rejected={stats.TotalRejected} instances={stats.Instances} " +
                      $"real={stats.RealVoices} virtual={stats.VirtualVoices} pooled={stats.PooledVoices} " +
                      $"tick={stats.TickMilliseconds:0.000}ms music={music} deepestDuck={_deepestDuck:0.0}dB");
        }

        private static bool IsSegment(ResonanceEngine engine, string name) => engine.Music.Current != null && engine.Music.Current.name == name;

        private void FirstCheck(ResonanceEngine engine)
        {
            Report(engine);
            Check(engine.Stats.TotalPlays >= 8, "sounds were played");
            Check(IsSegment(engine, "Explore"), "music starts on Explore");
            Check(_deepestDuck < -5f, "the narrator ducked the music bus");
            var track = engine.Music.Track;
            Resonance.SetState(track != null ? track.stateGroup : null, "Combat");
        }

        private void SecondCheck(ResonanceEngine engine)
        {
            Report(engine);
            Check(IsSegment(engine, "Combat"), "music moved to Combat");
            Check(engine.Stats.PooledVoices <= engine.MaxVoices, "voice pool stays within the budget");
            Check(engine.Memory.Banks.Count == 1 && engine.Memory.Clips.Count > 10, "the demo bank holds its clips");
            Check(_wettest > 0.9f, $"the stone hall reverb reached {_wettest:0.00}");
            Check(_mostOccluded > 0.3f, $"the wall occluded the car up to {_mostOccluded:0.00}");
            Check(_subtitles > 0, $"the narrator sent {_subtitles} subtitles");
            Check(_bridged, "the score went through the Rise bridge");
            Check(_beats >= 16, $"music beat events fired {_beats} times");
            Check(_markers > 0, $"clip markers fired {_markers} times");
            Check(_loudestLayer > 0.05f, $"the lead layer rose to {_loudestLayer:0.00} near the combat zone");
            gameObject.AddComponent<ScaleCheck>();
        }

        private static void Finish()
        {
            Debug.Log(Smoke.Failures == 0 ? "[Smoke] OK" : $"[Smoke] {Smoke.Failures} FAILED");
            Application.Quit(Smoke.Failures == 0 ? 0 : 1);
        }

        #endregion


        #region Unity Callbacks

        private void Start()
        {
            _start = Time.time;
            Resonance.Engine.SubtitleStarted += _ => _subtitles++;
            Resonance.Engine.MusicBeat += (_, _) => _beats++;
            Resonance.Engine.MarkerReached += _ => _markers++;
        }

        private void Update()
        {
            var engine = Resonance.Engine;
            var elapsed = Time.time - _start;
            _bridged |= IsSegment(engine, "Rise");
            _loudestLayer = Mathf.Max(_loudestLayer, engine.Music.LayerVolume(0));
            _deepestDuck = Mathf.Min(_deepestDuck, MusicDuck(engine));
            _wettest = Mathf.Max(_wettest, engine.Reverb.Wet);
            foreach (var instance in engine.Instances)
                _mostOccluded = Mathf.Max(_mostOccluded, instance.Occlusion);
            if (_step == 0 && elapsed >= 7.5f)
            {
                FirstCheck(engine);
                _step = 1;
            }
            else if (_step == 1 && elapsed >= 16f)
            {
                SecondCheck(engine);
                _step = 2;
            }
            else if (_step == 2 && Smoke.Pending == 0)
            {
                Finish();
                _step = 3;
            }
        }

        #endregion
    }
}

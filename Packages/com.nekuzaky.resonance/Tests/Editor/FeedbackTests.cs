using System.Collections.Generic;
using Nekuzaky.Resonance.Editor;
using NUnit.Framework;
using UnityEngine;

namespace Nekuzaky.Resonance.Tests
{
    public sealed class MarkerTests : Fixture
    {
        [Test]
        public void Markers_FireWhenPlaybackPassesThem()
        {
            var clip = ClipNode("swing", 1f);
            clip.markers.Add(new ClipMarker { name = "whoosh", time = 0.2f });
            clip.markers.Add(new ClipMarker { name = "impact", time = 0.7f });
            var reached = new List<string>();
            Engine.MarkerReached += marker => reached.Add(marker.Name);
            var sword = Track(new GameObject("Sword"));

            Engine.Play(Event(clip), sword, Vector3.zero);
            Run(0.4f);
            CollectionAssert.AreEqual(new[] { "whoosh" }, reached);
            Run(0.5f);
            CollectionAssert.AreEqual(new[] { "whoosh", "impact" }, reached);
        }

        [Test]
        public void Markers_FireAgainOnEveryLoop()
        {
            var clip = ClipNode("engine", 0.5f, true);
            clip.markers.Add(new ClipMarker { name = "cycle", time = 0.25f });
            var count = 0;
            Engine.MarkerReached += _ => count++;

            Engine.Play(Event(clip), null, Vector3.zero);
            Run(1.6f);

            Assert.AreEqual(3, count);
        }

        [Test]
        public void SoundEnded_ReportsTheHandleAndTheEmitter()
        {
            var ended = new List<(SoundHandle, GameObject)>();
            Engine.SoundEnded += (handle, emitter) => ended.Add((handle, emitter));
            var door = Track(new GameObject("Door"));

            var played = Engine.Play(Event(ClipNode("creak", 0.2f)), door, Vector3.zero);
            Run(0.3f);

            Assert.AreEqual(1, ended.Count);
            Assert.AreEqual(played, ended[0].Item1);
            Assert.AreEqual(door, ended[0].Item2);
        }
    }

    public sealed class MusicLayerTests : Fixture
    {
        [Test]
        public void Layers_FollowTheirParameter()
        {
            var intensity = Asset<SoundParameter>("Intensity");
            var track = Asset<MusicTrack>("Score");
            track.crossfade = 0f;
            var segment = new MusicSegment { name = "Explore", clip = Clip("explore", 4f) };
            segment.layers.Add(new MusicLayer { name = "Drums", clip = Clip("drums", 4f), parameter = intensity, range = new Vector2(0.5f, 1f), fade = 0.25f });
            track.segments.Add(segment);
            Engine.Music.Play(track, AudioSettings.dspTime);

            Engine.Syncs.SetParameter(intensity, 0f, null, true);
            Engine.Tick(0.02f);
            Assert.AreEqual(0f, Engine.Music.LayerVolume(0), 0.001f);

            Engine.Syncs.SetParameter(intensity, 1f, null, true);
            Engine.Tick(0.02f);
            Assert.AreEqual(1f, Engine.Music.LayerVolume(0), 0.001f);
        }

        [Test]
        public void Banks_LoadLayerClips()
        {
            var track = Asset<MusicTrack>("Score");
            var segment = new MusicSegment { name = "Explore", clip = Clip("explore") };
            segment.layers.Add(new MusicLayer { clip = Clip("drums") });
            track.segments.Add(segment);
            var clips = new HashSet<AudioClip>();

            SoundBank.CollectTrackClips(track, clips);

            Assert.AreEqual(2, clips.Count);
        }
    }

    public sealed class VolumeAndLoudnessTests : Fixture
    {
        [Test]
        public void VolumeSlider_MapsToDecibels()
        {
            Assert.AreEqual(0f, VolumeSetting.ToDecibels(1f), 0.001f);
            Assert.AreEqual(-12.04f, VolumeSetting.ToDecibels(0.5f), 0.01f);
            Assert.AreEqual(AudioMath.SilenceDb, VolumeSetting.ToDecibels(0f));
        }

        [Test]
        public void Gating_IgnoresSilence()
        {
            var blocks = new List<float> { 0.01f, 0.01f, 0.01f, 0.01f, 1e-12f, 1e-12f };

            Assert.AreEqual(-20.69f, Loudness.Gated(blocks), 0.01f);
        }

        [Test]
        public void Measure_ReadsAOneKilohertzSineAtItsRmsLevel()
        {
            var clip = Track(AudioClip.Create("sine", 96000, 1, 48000, false));
            var data = new float[96000];
            for (var i = 0; i < data.Length; i++)
                data[i] = 0.5f * Mathf.Sin(2f * Mathf.PI * 1000f * i / 48000f);
            clip.SetData(data, 0);

            var result = Loudness.Measure(clip);

            Assert.IsTrue(result.Valid);
            Assert.AreEqual(-9.03f, result.Loudness, 0.1f);
            Assert.AreEqual(-6.02f, result.Peak, 0.1f);
        }

        [Test]
        public void Balance_BringsVariationsTogether()
        {
            var quiet = Track(AudioClip.Create("quiet", 8000, 1, 8000, false));
            var loud = Track(AudioClip.Create("loud", 8000, 1, 8000, false));
            Fill(quiet, 0.05f);
            Fill(loud, 0.8f);
            var sound = Event(Container<RandomNode>(new ClipNode { name = "q", clip = quiet }, new ClipNode { name = "l", clip = loud }));

            var imbalance = Loudness.Analyze(sound);
            Assert.IsNotNull(imbalance);
            Loudness.Balance(imbalance);

            Assert.IsNull(Loudness.Analyze(sound));
        }

        private static void Fill(AudioClip clip, float amplitude)
        {
            var data = new float[clip.samples];
            for (var i = 0; i < data.Length; i++)
                data[i] = amplitude * Mathf.Sin(i * 0.3f);
            clip.SetData(data, 0);
        }
    }
}

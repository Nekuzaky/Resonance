using System.Collections.Generic;
using UnityEngine;

namespace Nekuzaky.Resonance
{
    internal struct ReverbSettings
    {
        public float Room;
        public float RoomHF;
        public float RoomLF;
        public float DecayTime;
        public float DecayHFRatio;
        public float Reflections;
        public float ReflectionsDelay;
        public float Reverb;
        public float ReverbDelay;
        public float HFReference;
        public float LFReference;
        public float Diffusion;
        public float Density;

        public static ReverbSettings Read(AudioReverbZone zone) => new()
        {
            Room = zone.room,
            RoomHF = zone.roomHF,
            RoomLF = zone.roomLF,
            DecayTime = zone.decayTime,
            DecayHFRatio = zone.decayHFRatio,
            Reflections = zone.reflections,
            ReflectionsDelay = zone.reflectionsDelay,
            Reverb = zone.reverb,
            ReverbDelay = zone.reverbDelay,
            HFReference = zone.HFReference,
            LFReference = zone.LFReference,
            Diffusion = zone.diffusion,
            Density = zone.density
        };

        public void Write(AudioReverbZone zone)
        {
            zone.room = Mathf.RoundToInt(Room);
            zone.roomHF = Mathf.RoundToInt(RoomHF);
            zone.roomLF = Mathf.RoundToInt(RoomLF);
            zone.decayTime = DecayTime;
            zone.decayHFRatio = DecayHFRatio;
            zone.reflections = Mathf.RoundToInt(Reflections);
            zone.reflectionsDelay = ReflectionsDelay;
            zone.reverb = Mathf.RoundToInt(Reverb);
            zone.reverbDelay = ReverbDelay;
            zone.HFReference = HFReference;
            zone.LFReference = LFReference;
            zone.diffusion = Diffusion;
            zone.density = Density;
        }

        public void Add(ReverbSettings other, float weight)
        {
            Room += other.Room * weight;
            RoomHF += other.RoomHF * weight;
            RoomLF += other.RoomLF * weight;
            DecayTime += other.DecayTime * weight;
            DecayHFRatio += other.DecayHFRatio * weight;
            Reflections += other.Reflections * weight;
            ReflectionsDelay += other.ReflectionsDelay * weight;
            Reverb += other.Reverb * weight;
            ReverbDelay += other.ReverbDelay * weight;
            HFReference += other.HFReference * weight;
            LFReference += other.LFReference * weight;
            Diffusion += other.Diffusion * weight;
            Density += other.Density * weight;
        }
    }

    internal sealed class ReverbMixer
    {
        #region Public

        public ReverbZone Dominant { get; private set; }

        public float Wet { get; private set; }

        public ReverbMixer(Transform host)
        {
            var go = new GameObject("Reverb") { hideFlags = host.gameObject.hideFlags };
            go.transform.SetParent(host, false);
            _zone = go.AddComponent<AudioReverbZone>();
            _zone.minDistance = 100000f;
            _zone.maxDistance = 100001f;
            _zone.enabled = false;
        }

        public void Tick(Transform listener, IReadOnlyList<ReverbZone> zones)
        {
            Dominant = null;
            Wet = 0f;
            if (_zone == null)
                return;
            if (listener == null || zones.Count == 0)
            {
                _zone.enabled = false;
                return;
            }
            var mix = Blend(listener.position, zones, out var total);
            _zone.enabled = total > 0f;
            if (!_zone.enabled)
                return;
            _zone.transform.position = listener.position;
            _zone.reverbPreset = AudioReverbPreset.User;
            mix.Write(_zone);
        }

        public ReverbSettings Preset(AudioReverbPreset preset)
        {
            if (Presets.TryGetValue(preset, out var settings))
                return settings;
            var previous = _zone.reverbPreset;
            _zone.reverbPreset = preset;
            settings = ReverbSettings.Read(_zone);
            _zone.reverbPreset = previous;
            Presets[preset] = settings;
            return settings;
        }

        #endregion


        #region Private and Protected

        private static readonly Dictionary<AudioReverbPreset, ReverbSettings> Presets = new();
        private readonly AudioReverbZone _zone;

        private ReverbSettings Blend(Vector3 position, IReadOnlyList<ReverbZone> zones, out float total)
        {
            var sum = new ReverbSettings();
            total = 0f;
            var strongest = 0f;
            for (var i = 0; i < zones.Count; i++)
            {
                var zone = zones[i];
                var weight = zone != null ? zone.WeightAt(position) : 0f;
                if (weight <= 0f)
                    continue;
                sum.Add(Preset(zone.preset), weight);
                total += weight;
                if (weight > strongest)
                {
                    strongest = weight;
                    Dominant = zone;
                }
            }
            if (total <= 0f)
                return sum;
            var wet = Mathf.Min(1f, total);
            var mix = new ReverbSettings();
            mix.Add(Preset(AudioReverbPreset.Off), 1f - wet);
            mix.Add(sum, wet / total);
            Wet = wet;
            return mix;
        }

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Nekuzaky.Resonance
{
    internal struct RemoteSound
    {
        public string Event;
        public string Emitter;
        public string Bus;
        public int Voices;
        public bool Virtual;
        public bool Spatial;
        public float Distance;
        public float Level;
        public float Occlusion;
    }

    internal struct RemoteBus
    {
        public string Name;
        public float Level;
        public float Duck;
    }

    internal sealed class RemoteFrame
    {
        #region Public

        public static readonly Guid MessageId = new("5d3c2a4e-9b7f-4c1e-8f2a-6b0d9e3c7a51");
        public const byte Version = 1;

        public float Time;
        public string Platform = string.Empty;
        public int Instances;
        public int Real;
        public int Virtual;
        public int Pooled;
        public int Plays;
        public int Rejected;
        public int OnDemand;
        public double Cost;
        public string Reverb = string.Empty;
        public float Wet;
        public readonly List<RemoteSound> Sounds = new();
        public readonly List<RemoteBus> Buses = new();

        public void Capture(ResonanceEngine engine)
        {
            var stats = engine.Stats;
            Time = engine.Time;
            Platform = Application.platform.ToString();
            Instances = stats.Instances;
            Real = stats.RealVoices;
            Virtual = stats.VirtualVoices;
            Pooled = stats.PooledVoices;
            Plays = stats.TotalPlays;
            Rejected = stats.TotalRejected;
            OnDemand = stats.OnDemandLoads;
            Cost = stats.TickMilliseconds;
            Reverb = engine.Reverb.Dominant != null ? engine.Reverb.Dominant.preset.ToString() : string.Empty;
            Wet = engine.Reverb.Wet;
            CaptureSounds(engine);
            Buses.Clear();
            foreach (var pair in engine.Mixer.Buses)
            {
                if (pair.Key != null)
                    Buses.Add(new RemoteBus { Name = pair.Key.name, Level = pair.Value.Meter, Duck = pair.Value.Duck });
            }
        }

        public byte[] Write()
        {
            using var stream = new MemoryStream(1024);
            using var writer = new BinaryWriter(stream);
            writer.Write(Version);
            writer.Write(Time);
            writer.Write(Platform);
            writer.Write(Instances);
            writer.Write(Real);
            writer.Write(Virtual);
            writer.Write(Pooled);
            writer.Write(Plays);
            writer.Write(Rejected);
            writer.Write(OnDemand);
            writer.Write(Cost);
            writer.Write(Reverb);
            writer.Write(Wet);
            WriteSounds(writer);
            WriteBuses(writer);
            return stream.ToArray();
        }

        public bool Read(byte[] data)
        {
            if (data == null || data.Length == 0 || data[0] != Version)
                return false;
            using var reader = new BinaryReader(new MemoryStream(data));
            reader.ReadByte();
            Time = reader.ReadSingle();
            Platform = reader.ReadString();
            Instances = reader.ReadInt32();
            Real = reader.ReadInt32();
            Virtual = reader.ReadInt32();
            Pooled = reader.ReadInt32();
            Plays = reader.ReadInt32();
            Rejected = reader.ReadInt32();
            OnDemand = reader.ReadInt32();
            Cost = reader.ReadDouble();
            Reverb = reader.ReadString();
            Wet = reader.ReadSingle();
            ReadSounds(reader);
            ReadBuses(reader);
            return true;
        }

        #endregion


        #region Private and Protected

        private const int MaxItems = 512;

        private void CaptureSounds(ResonanceEngine engine)
        {
            Sounds.Clear();
            foreach (var instance in engine.Instances)
            {
                Sounds.Add(new RemoteSound
                {
                    Event = instance.Event != null ? instance.Event.name : "(deleted)",
                    Emitter = instance.EmitterName,
                    Bus = instance.Event != null && instance.Event.bus != null ? instance.Event.bus.name : "-",
                    Voices = instance.Voices.Count,
                    Virtual = instance.IsVirtual,
                    Spatial = instance.Event != null && instance.Event.spatialize,
                    Distance = instance.Distance,
                    Level = instance.Level,
                    Occlusion = instance.Occlusion
                });
            }
        }

        private void WriteSounds(BinaryWriter writer)
        {
            var count = Mathf.Min(Sounds.Count, MaxItems);
            writer.Write(count);
            for (var i = 0; i < count; i++)
            {
                var sound = Sounds[i];
                writer.Write(sound.Event ?? string.Empty);
                writer.Write(sound.Emitter ?? string.Empty);
                writer.Write(sound.Bus ?? string.Empty);
                writer.Write(sound.Voices);
                writer.Write(sound.Virtual);
                writer.Write(sound.Spatial);
                writer.Write(sound.Distance);
                writer.Write(sound.Level);
                writer.Write(sound.Occlusion);
            }
        }

        private void ReadSounds(BinaryReader reader)
        {
            Sounds.Clear();
            var count = Mathf.Clamp(reader.ReadInt32(), 0, MaxItems);
            for (var i = 0; i < count; i++)
            {
                Sounds.Add(new RemoteSound
                {
                    Event = reader.ReadString(),
                    Emitter = reader.ReadString(),
                    Bus = reader.ReadString(),
                    Voices = reader.ReadInt32(),
                    Virtual = reader.ReadBoolean(),
                    Spatial = reader.ReadBoolean(),
                    Distance = reader.ReadSingle(),
                    Level = reader.ReadSingle(),
                    Occlusion = reader.ReadSingle()
                });
            }
        }

        private void WriteBuses(BinaryWriter writer)
        {
            var count = Mathf.Min(Buses.Count, MaxItems);
            writer.Write(count);
            for (var i = 0; i < count; i++)
            {
                writer.Write(Buses[i].Name ?? string.Empty);
                writer.Write(Buses[i].Level);
                writer.Write(Buses[i].Duck);
            }
        }

        private void ReadBuses(BinaryReader reader)
        {
            Buses.Clear();
            var count = Mathf.Clamp(reader.ReadInt32(), 0, MaxItems);
            for (var i = 0; i < count; i++)
                Buses.Add(new RemoteBus { Name = reader.ReadString(), Level = reader.ReadSingle(), Duck = reader.ReadSingle() });
        }

        #endregion
    }
}

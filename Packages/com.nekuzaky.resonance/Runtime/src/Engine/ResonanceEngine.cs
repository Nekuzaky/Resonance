using System.Collections.Generic;
using UnityEngine;

namespace Nekuzaky.Resonance
{
    public sealed partial class ResonanceEngine
    {
        #region Public

        public const int DefaultMaxVoices = 64;
        public const float StealFade = 0.04f;

        public ResonanceStats Stats { get; } = new();

        public int MaxVoices { get; set; } = DefaultMaxVoices;

        public bool Preview { get; set; }

        public bool Metering { get; set; }

        public Transform Listener { get; set; }

        public float Time => _time;

        public ResonanceEngine(Transform host, int seed = 0)
        {
            _host = host;
            _resolver = new Resolver(seed == 0 ? System.Environment.TickCount : seed);
            _plan = new Plan(_layerPool);
            Music = new MusicPlayer(host, Syncs, Mixer);
        }

        #endregion


        #region Private and Protected

        internal bool Simulate { get; set; }

        internal readonly SyncStore Syncs = new();
        internal readonly BusMixer Mixer = new();
        internal readonly MusicPlayer Music;

        internal IReadOnlyList<SoundInstance> Instances => _instances;

        private readonly Transform _host;
        private readonly Resolver _resolver;
        private readonly Plan _plan;
        private readonly Stack<PlanLayer> _layerPool = new();
        private readonly Stack<Voice> _free = new();
        private readonly List<Voice> _voices = new();
        private readonly List<SoundInstance> _instances = new();
        private readonly float[] _samples = new float[256];
        private AudioListener _audioListener;
        private float _nextListenerSearch;
        private float _time;
        private int _nextId = 1;

        #endregion


        #region Main API

        internal SoundHandle Play(SoundEvent sound, GameObject emitter, Vector3 position)
        {
            if (sound == null)
                return SoundHandle.None;

            var admission = Admit(sound, emitter);
            if (admission != PlayResult.Played && admission != PlayResult.Stolen)
                return Reject(sound, emitter, admission);

            _resolver.Resolve(sound.root, Syncs.ReaderFor(emitter), _plan);
            if (_plan.Layers.Count == 0)
                return Reject(sound, emitter, PlayResult.Empty);

            var room = MakeRoom(sound, _plan.Layers.Count);
            if (room < 0)
            {
                _plan.Release();
                return Reject(sound, emitter, PlayResult.RejectedVoices);
            }

            var instance = Begin(sound, emitter, position);
            var stolen = room > 0 || admission == PlayResult.Stolen;
            Stats.Record(new EventLogEntry(_time, sound.name, instance.EmitterName, stolen ? PlayResult.Stolen : PlayResult.Played));
            sound.LastPlayTime = _time;
            return new SoundHandle(instance.Id);
        }

        internal bool IsPlaying(int id)
        {
            var instance = Find(id);
            return instance != null && instance.State != InstanceState.Done;
        }

        internal void Stop(int id, float fade)
        {
            var instance = Find(id);
            if (instance != null)
                BeginStop(instance, fade < 0f ? instance.Event.fadeOut : fade);
        }

        internal void StopAll(GameObject emitter, float fade)
        {
            foreach (var instance in _instances)
            {
                if (emitter == null || instance.Emitter == emitter)
                    BeginStop(instance, fade < 0f ? instance.Event.fadeOut : fade);
            }
        }

        internal void StopEvent(SoundEvent sound, float fade)
        {
            foreach (var instance in _instances)
            {
                if (instance.Event == sound)
                    BeginStop(instance, fade < 0f ? instance.Event.fadeOut : fade);
            }
        }

        internal int CountPlaying(SoundEvent sound, GameObject emitter)
        {
            var count = 0;
            foreach (var instance in _instances)
            {
                if (instance.State == InstanceState.Playing && instance.Event == sound && (emitter == null || instance.Emitter == emitter))
                    count++;
            }
            return count;
        }

        public void Shutdown()
        {
            for (var i = _instances.Count - 1; i >= 0; i--)
                Release(_instances[i]);
            _instances.Clear();
            Music.Stop(0f);
        }

        #endregion


        #region Private and Protected

        private PlayResult Admit(SoundEvent sound, GameObject emitter)
        {
            if (sound.cooldown > 0f && _time - sound.LastPlayTime < sound.cooldown)
                return PlayResult.Cooldown;
            if (sound.maxInstances <= 0)
                return PlayResult.Played;

            var scope = sound.limitScope == LimitScope.PerEmitter ? emitter : null;
            if (CountPlaying(sound, scope) < sound.maxInstances)
                return PlayResult.Played;
            if (sound.limitBehavior == LimitBehavior.RejectNew)
                return PlayResult.RejectedLimit;

            var victim = PickVictim(sound, scope);
            if (victim == null)
                return PlayResult.RejectedLimit;
            BeginStop(victim, StealFade);
            return PlayResult.Stolen;
        }

        private SoundInstance PickVictim(SoundEvent sound, GameObject scope)
        {
            SoundInstance victim = null;
            foreach (var instance in _instances)
            {
                if (instance.State != InstanceState.Playing || instance.Event != sound || (scope != null && instance.Emitter != scope))
                    continue;
                if (victim == null)
                    victim = instance;
                else if (sound.limitBehavior == LimitBehavior.StealQuietest ? instance.Level < victim.Level : instance.Age > victim.Age)
                    victim = instance;
            }
            return victim;
        }

        private int MakeRoom(SoundEvent sound, int needed)
        {
            var stole = 0;
            for (var guard = 0; guard < MaxVoices + 1 && ActiveVoices() + needed > MaxVoices; guard++)
            {
                var victim = LowestPriority(sound.priority);
                if (victim == null)
                    return -1;
                Release(victim);
                _instances.Remove(victim);
                stole = 1;
            }
            return stole;
        }

        private SoundInstance LowestPriority(int priority)
        {
            SoundInstance lowest = null;
            foreach (var instance in _instances)
            {
                if (instance.Event.priority > priority)
                    continue;
                if (lowest == null || instance.Event.priority < lowest.Event.priority ||
                    (instance.Event.priority == lowest.Event.priority && instance.Age > lowest.Age))
                    lowest = instance;
            }
            return lowest;
        }

        private int ActiveVoices()
        {
            var count = 0;
            foreach (var instance in _instances)
                count += instance.Voices.Count;
            return count;
        }

        private SoundHandle Reject(SoundEvent sound, GameObject emitter, PlayResult result)
        {
            Stats.Record(new EventLogEntry(_time, sound.name, emitter != null ? emitter.name : "(world)", result));
            return SoundHandle.None;
        }

        private SoundInstance Find(int id)
        {
            foreach (var instance in _instances)
            {
                if (instance.Id == id)
                    return instance;
            }
            return null;
        }

        private static void BeginStop(SoundInstance instance, float fade)
        {
            if (instance.State != InstanceState.Playing)
            {
                if (instance.State == InstanceState.Stopping && fade < instance.FadeOutTime)
                    instance.FadeOutTime = fade;
                return;
            }
            instance.State = InstanceState.Stopping;
            instance.FadeOutTime = Mathf.Max(0f, fade);
            if (instance.FadeOutTime <= 0f)
                instance.Fade = 0f;
        }

        #endregion
    }
}

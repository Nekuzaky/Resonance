using System;
using UnityEngine;

namespace Nekuzaky.Resonance
{
    public readonly struct SoundHandle : IEquatable<SoundHandle>
    {
        public static readonly SoundHandle None = default;

        public readonly int Id;

        public SoundHandle(int id) => Id = id;

        public bool IsValid => Id != 0;

        public bool IsPlaying => IsValid && Resonance.IsPlaying(this);

        public void Stop(float fade = -1f) => Resonance.Stop(this, fade);

        public bool Equals(SoundHandle other) => Id == other.Id;

        public override bool Equals(object obj) => obj is SoundHandle other && Equals(other);

        public override int GetHashCode() => Id;

        public override string ToString() => IsValid ? $"Sound #{Id}" : "Sound (none)";
    }

    public static class Resonance
    {
        #region Public

        public static ResonanceEngine Engine
        {
            get
            {
                if (_override != null)
                    return _override;
                if (Application.isPlaying)
                    return ResonanceRuntime.Ensure().Engine;
                return EditorEngine?.Invoke();
            }
        }

        public static ResonanceStats Stats => Engine?.Stats;

        public static SoundHandle Play(SoundEvent sound, GameObject emitter = null)
        {
            var engine = Engine;
            return engine == null ? SoundHandle.None : engine.Play(sound, emitter, emitter != null ? emitter.transform.position : Vector3.zero);
        }

        public static SoundHandle Play(SoundEvent sound, Component emitter) => Play(sound, emitter != null ? emitter.gameObject : null);

        public static SoundHandle PlayAt(SoundEvent sound, Vector3 position) => Engine?.Play(sound, null, position) ?? SoundHandle.None;

        public static bool IsPlaying(SoundHandle handle) => handle.IsValid && Engine != null && Engine.IsPlaying(handle.Id);

        public static void Stop(SoundHandle handle, float fade = -1f) => Engine?.Stop(handle.Id, fade);

        public static void Stop(SoundEvent sound, float fade = -1f) => Engine?.StopEvent(sound, fade);

        public static void StopAll(GameObject emitter = null, float fade = -1f) => Engine?.StopAll(emitter, fade);

        public static void SetParameter(SoundParameter parameter, float value, GameObject emitter = null, bool immediate = false) =>
            Engine?.Syncs.SetParameter(parameter, value, emitter, immediate);

        public static float GetParameter(SoundParameter parameter, GameObject emitter = null) =>
            Engine?.Syncs.GetParameter(parameter, emitter) ?? (parameter != null ? parameter.defaultValue : 0f);

        public static void SetSwitch(SwitchGroup group, string value, GameObject emitter = null) => Engine?.Syncs.SetSwitch(group, value, emitter);

        public static string GetSwitch(SwitchGroup group, GameObject emitter = null) => Engine?.Syncs.GetSwitch(group, emitter) ?? string.Empty;

        public static void SetState(StateGroup group, string state) => Engine?.Syncs.SetState(group, state);

        public static string GetState(StateGroup group) => Engine?.Syncs.GetState(group) ?? (group != null ? group.DefaultState : string.Empty);

        public static void PlayMusic(MusicTrack track) => Engine?.Music.Play(track, AudioSettings.dspTime);

        public static void StopMusic(float fade = 1f) => Engine?.Music.Stop(fade);

        public static void PlayMusicSegment(string segment, MusicSync sync = MusicSync.NextBar) => Engine?.Music.RequestNamed(segment, sync);

        public static void PlayStinger(string stinger) => Engine?.Music.PlayStinger(stinger);

        public static MusicTrack CurrentMusic => Engine?.Music.Track;

        public static void ApplySnapshot(MixSnapshot snapshot) => Engine?.Mixer.Apply(snapshot);

        public static void RemoveSnapshot(MixSnapshot snapshot) => Engine?.Mixer.Remove(snapshot);

        public static void SetBusVolume(AudioBus bus, float decibels)
        {
            if (bus != null && Engine != null)
                Engine.Mixer.State(bus).UserVolume = decibels;
        }

        #endregion

        #region Private and Protected

        internal static Func<ResonanceEngine> EditorEngine;

        private static ResonanceEngine _override;

        internal static void Override(ResonanceEngine engine) => _override = engine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _override = null;

        #endregion
    }
}

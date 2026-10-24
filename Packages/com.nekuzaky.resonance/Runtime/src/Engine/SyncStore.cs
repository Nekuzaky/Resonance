using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekuzaky.Resonance
{
    internal sealed class ParameterValue
    {
        public float Current;
        public float Target;
    }

    internal sealed class EmitterSyncs : ISyncReader
    {
        public readonly Dictionary<SoundParameter, ParameterValue> Parameters = new();
        public readonly Dictionary<SwitchGroup, string> Switches = new();
        public SyncStore Store;

        public string GetSwitch(SwitchGroup group) => Switches.TryGetValue(group, out var value) ? value : Store.GetSwitch(group, null);
    }

    internal sealed class SyncStore : ISyncReader
    {
        #region Public

        public event Action<StateGroup, string> StateChanged;

        public IReadOnlyDictionary<SoundParameter, ParameterValue> GlobalParameters => _parameters;

        public IReadOnlyDictionary<StateGroup, string> States => _states;

        public int EmitterCount => _emitters.Count;

        #endregion


        #region Private and Protected

        private readonly Dictionary<SoundParameter, ParameterValue> _parameters = new();
        private readonly Dictionary<SwitchGroup, string> _switches = new();
        private readonly Dictionary<StateGroup, string> _states = new();
        private readonly Dictionary<GameObject, EmitterSyncs> _emitters = new();
        private readonly List<GameObject> _dead = new();

        #endregion


        #region Main API

        public void SetParameter(SoundParameter parameter, float value, GameObject emitter, bool immediate)
        {
            if (parameter == null)
                return;

            var slot = Slot(ParametersOf(parameter, emitter, true), parameter);
            slot.Target = parameter.Clamp(value);
            if (immediate || parameter.seekSpeed <= 0f)
                slot.Current = slot.Target;
        }

        public float GetParameter(SoundParameter parameter, GameObject emitter)
        {
            if (parameter == null)
                return 0f;
            var map = ParametersOf(parameter, emitter, false);
            if (map != null && map.TryGetValue(parameter, out var local))
                return local.Current;
            return _parameters.TryGetValue(parameter, out var global) ? global.Current : parameter.Clamp(parameter.defaultValue);
        }

        public void SetSwitch(SwitchGroup group, string value, GameObject emitter)
        {
            if (group == null)
                return;
            if (emitter == null)
                _switches[group] = value ?? string.Empty;
            else
                Emitter(emitter).Switches[group] = value ?? string.Empty;
        }

        public string GetSwitch(SwitchGroup group, GameObject emitter)
        {
            if (group == null)
                return string.Empty;
            if (emitter != null && _emitters.TryGetValue(emitter, out var local) && local.Switches.TryGetValue(group, out var value))
                return value;
            return _switches.TryGetValue(group, out var global) ? global : group.DefaultValue;
        }

        public string GetSwitch(SwitchGroup group) => GetSwitch(group, null);

        public ISyncReader ReaderFor(GameObject emitter) => emitter != null && _emitters.TryGetValue(emitter, out var local) ? local : this;

        public void SetState(StateGroup group, string state)
        {
            if (group == null)
                return;
            state ??= string.Empty;
            if (_states.TryGetValue(group, out var current) && current == state)
                return;
            _states[group] = state;
            StateChanged?.Invoke(group, state);
        }

        public string GetState(StateGroup group)
        {
            if (group == null)
                return string.Empty;
            return _states.TryGetValue(group, out var state) ? state : group.DefaultState;
        }

        public void Tick(float deltaTime)
        {
            Seek(_parameters, deltaTime);
            _dead.Clear();
            foreach (var pair in _emitters)
            {
                if (pair.Key == null)
                    _dead.Add(pair.Key);
                else
                    Seek(pair.Value.Parameters, deltaTime);
            }
            foreach (var key in _dead)
                _emitters.Remove(key);
        }

        public void Clear()
        {
            _parameters.Clear();
            _switches.Clear();
            _states.Clear();
            _emitters.Clear();
        }

        #endregion


        #region Private and Protected

        private Dictionary<SoundParameter, ParameterValue> ParametersOf(SoundParameter parameter, GameObject emitter, bool create)
        {
            if (parameter.scope == ParameterScope.Global || emitter == null)
                return _parameters;
            if (create)
                return Emitter(emitter).Parameters;
            return _emitters.TryGetValue(emitter, out var local) ? local.Parameters : null;
        }

        private ParameterValue Slot(Dictionary<SoundParameter, ParameterValue> map, SoundParameter parameter)
        {
            if (map.TryGetValue(parameter, out var slot))
                return slot;

            var start = _parameters.TryGetValue(parameter, out var global) ? global.Current : parameter.Clamp(parameter.defaultValue);
            slot = new ParameterValue { Current = start, Target = start };
            map[parameter] = slot;
            return slot;
        }

        private EmitterSyncs Emitter(GameObject emitter)
        {
            if (_emitters.TryGetValue(emitter, out var local))
                return local;
            local = new EmitterSyncs { Store = this };
            _emitters[emitter] = local;
            return local;
        }

        private static void Seek(Dictionary<SoundParameter, ParameterValue> map, float deltaTime)
        {
            foreach (var pair in map)
            {
                var value = pair.Value;
                if (value.Current == value.Target || pair.Key == null)
                    continue;
                var speed = pair.Key.seekSpeed;
                value.Current = speed <= 0f ? value.Target : Mathf.MoveTowards(value.Current, value.Target, speed * deltaTime);
            }
        }

        #endregion
    }
}

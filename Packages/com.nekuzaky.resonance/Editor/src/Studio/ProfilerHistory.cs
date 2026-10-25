using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nekuzaky.Resonance.Editor
{
    internal sealed class ProfilerSample
    {
        public double Time;
        public int Instances;
        public int Real;
        public int Virtual;
        public double Cost;
        public readonly List<RemoteSound> Sounds = new();
    }

    internal sealed class ProfilerHistory
    {
        #region Public

        public const int Capacity = 600;

        public int Count => _count;

        public ProfilerSample this[int oldestFirst] => _samples[(_head - _count + oldestFirst + Capacity * 2) % Capacity];

        public void Record(ResonanceEngine engine, double time)
        {
            var stats = engine.Stats;
            var sample = Next(time, stats.Instances, stats.RealVoices, stats.VirtualVoices, stats.TickMilliseconds);
            if (++_recorded % Capacity == 0)
                _names.Clear();
            foreach (var instance in engine.Instances)
                sample.Sounds.Add(Snapshot(instance, engine.Preview));
        }

        public void RecordRemote(RemoteFrame frame, double time)
        {
            var sample = Next(time, frame.Instances, frame.Real, frame.Virtual, frame.Cost);
            sample.Sounds.AddRange(frame.Sounds);
        }

        public void Clear()
        {
            _count = 0;
            _head = 0;
            _names.Clear();
        }

        #endregion


        #region Private and Protected

        private readonly ProfilerSample[] _samples = new ProfilerSample[Capacity];
        private int _head;
        private int _count;

        private ProfilerSample Next(double time, int instances, int real, int virtualVoices, double cost)
        {
            var sample = _samples[_head] ??= new ProfilerSample();
            _head = (_head + 1) % Capacity;
            _count = Mathf.Min(_count + 1, Capacity);
            sample.Time = time;
            sample.Instances = instances;
            sample.Real = real;
            sample.Virtual = virtualVoices;
            sample.Cost = cost;
            sample.Sounds.Clear();
            return sample;
        }

        private readonly NameCache _names = new();
        private int _recorded;

        private RemoteSound Snapshot(SoundInstance instance, bool preview)
        {
            var sound = instance.Event;
            return new RemoteSound
            {
                Event = _names.Of(sound) ?? "(deleted)",
                Emitter = _names.Of(instance.Emitter) ?? (instance.HadEmitter ? "(destroyed)" : "(world)"),
                Bus = (sound != null ? _names.Of(sound.bus) : null) ?? "-",
                Voices = instance.Voices.Count,
                Virtual = instance.IsVirtual,
                Spatial = sound != null && sound.spatialize && !preview,
                Distance = instance.Distance,
                Level = instance.Level,
                Occlusion = instance.Occlusion
            };
        }

        #endregion
    }

    internal sealed class ProfilerTimeline : VisualElement
    {
        #region Public

        public event System.Action<int> Picked;

        public int Cursor { get; set; } = -1;

        public ProfilerTimeline(ProfilerHistory history)
        {
            _history = history;
            AddToClassList("rs-history");
            generateVisualContent += Draw;
            RegisterCallback<PointerDownEvent>(evt => Pick(evt.localPosition.x));
            RegisterCallback<PointerMoveEvent>(evt =>
            {
                if (evt.pressedButtons == 1)
                    Pick(evt.localPosition.x);
            });
        }

        #endregion


        #region Private and Protected

        private static readonly Color RealColor = new(47f / 255f, 169f / 255f, 184f / 255f);
        private static readonly Color VirtualColor = new(222f / 255f, 170f / 255f, 72f / 255f, 0.8f);
        private static readonly Color CostColor = new(0.87f, 0.87f, 0.9f, 0.7f);
        private readonly ProfilerHistory _history;

        private void Pick(float x)
        {
            if (_history.Count == 0)
                return;
            var index = Mathf.Clamp(Mathf.FloorToInt(x / contentRect.width * ProfilerHistory.Capacity) - (ProfilerHistory.Capacity - _history.Count), 0, _history.Count - 1);
            Cursor = index;
            Picked?.Invoke(index);
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext context)
        {
            var rect = contentRect;
            var painter = context.painter2D;
            var step = rect.width / ProfilerHistory.Capacity;
            var offset = ProfilerHistory.Capacity - _history.Count;
            var peak = 4;
            var cost = 0.05;
            for (var i = 0; i < _history.Count; i++)
            {
                peak = Mathf.Max(peak, _history[i].Real + _history[i].Virtual);
                cost = System.Math.Max(cost, _history[i].Cost);
            }
            for (var i = 0; i < _history.Count; i++)
            {
                var sample = _history[i];
                var x = (offset + i) * step;
                Bar(painter, x, step, rect.height, 0f, sample.Real / (float)peak, RealColor);
                Bar(painter, x, step, rect.height, sample.Real / (float)peak, sample.Virtual / (float)peak, VirtualColor);
            }
            DrawCost(painter, rect, step, offset, cost);
            if (Cursor >= 0 && Cursor < _history.Count)
                Bar(painter, (offset + Cursor) * step - 1f, 2f, rect.height, 0f, 1f, Color.white);
        }

        private void DrawCost(Painter2D painter, Rect rect, float step, int offset, double cost)
        {
            if (_history.Count < 2)
                return;
            painter.strokeColor = CostColor;
            painter.lineWidth = 1f;
            painter.BeginPath();
            for (var i = 0; i < _history.Count; i++)
            {
                var point = new Vector2((offset + i + 0.5f) * step, rect.height * (1f - (float)(_history[i].Cost / cost)));
                if (i == 0)
                    painter.MoveTo(point);
                else
                    painter.LineTo(point);
            }
            painter.Stroke();
        }

        private static void Bar(Painter2D painter, float x, float width, float height, float from, float amount, Color color)
        {
            if (amount <= 0f)
                return;
            painter.fillColor = color;
            painter.BeginPath();
            painter.MoveTo(new Vector2(x, height * (1f - from)));
            painter.LineTo(new Vector2(x + Mathf.Max(1f, width), height * (1f - from)));
            painter.LineTo(new Vector2(x + Mathf.Max(1f, width), height * (1f - from - amount)));
            painter.LineTo(new Vector2(x, height * (1f - from - amount)));
            painter.ClosePath();
            painter.Fill();
        }

        #endregion
    }
}

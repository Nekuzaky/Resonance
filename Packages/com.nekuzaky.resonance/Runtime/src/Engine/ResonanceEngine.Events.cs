using System.Collections.Generic;
using UnityEngine;

namespace Nekuzaky.Resonance
{
    public sealed partial class ResonanceEngine
    {
        #region Private and Protected

        private enum PendingKind : byte
        {
            SubtitleStarted,
            SubtitleEnded,
            Marker,
            Started,
            Ended
        }

        private struct PendingEvent
        {
            public PendingKind Kind;
            public Subtitle Subtitle;
            public SoundMarker Marker;
            public SoundHandle Handle;
            public GameObject Emitter;
            public SoundStartInfo Start;
        }

        private const int MaxDispatched = 4096;

        private readonly List<PendingEvent> _pendingEvents = new();
        private bool _dispatching;

        private void QueueSubtitle(PendingKind kind, Subtitle subtitle) =>
            _pendingEvents.Add(new PendingEvent { Kind = kind, Subtitle = subtitle });

        private void QueueMarker(SoundMarker marker) =>
            _pendingEvents.Add(new PendingEvent { Kind = PendingKind.Marker, Marker = marker });

        private void QueueEnded(SoundInstance instance) =>
            _pendingEvents.Add(new PendingEvent { Kind = PendingKind.Ended, Handle = new SoundHandle(instance.Id), Emitter = instance.Emitter });

        private void QueueStarted(SoundInstance instance)
        {
            if (SoundStarted == null)
                return;
            var info = new SoundStartInfo(new SoundHandle(instance.Id), instance.Event, instance.Emitter, instance.Position, StartVolume(instance), RangeOf(instance.Event));
            _pendingEvents.Add(new PendingEvent { Kind = PendingKind.Started, Start = info });
        }

        private static float StartVolume(SoundInstance instance)
        {
            var loudest = 0f;
            foreach (var voice in instance.Voices)
            {
                if (voice.HasClip)
                    loudest = Mathf.Max(loudest, voice.Clip.Gain * voice.BlendWeight);
            }
            return instance.Gain * loudest;
        }

        private static float RangeOf(SoundEvent sound)
        {
            var spatial = sound.Spatial;
            if (!sound.spatialize || spatial.blend <= 0f)
                return 0f;
            return Mathf.Max(spatial.minDistance + 0.01f, spatial.maxDistance);
        }

        private void Dispatch()
        {
            if (_dispatching || _pendingEvents.Count == 0)
                return;
            _dispatching = true;
            try
            {
                for (var i = 0; i < _pendingEvents.Count && i < MaxDispatched; i++)
                    Raise(_pendingEvents[i]);
            }
            finally
            {
                _pendingEvents.Clear();
                _dispatching = false;
            }
        }

        private void Raise(PendingEvent pending)
        {
            switch (pending.Kind)
            {
                case PendingKind.SubtitleStarted:
                    SubtitleStarted?.Invoke(pending.Subtitle);
                    break;
                case PendingKind.SubtitleEnded:
                    SubtitleEnded?.Invoke(pending.Subtitle);
                    break;
                case PendingKind.Marker:
                    MarkerReached?.Invoke(pending.Marker);
                    break;
                case PendingKind.Started:
                    SoundStarted?.Invoke(pending.Start);
                    break;
                default:
                    SoundEnded?.Invoke(pending.Handle, pending.Emitter);
                    break;
            }
        }

        #endregion
    }
}

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
            Ended
        }

        private struct PendingEvent
        {
            public PendingKind Kind;
            public Subtitle Subtitle;
            public SoundMarker Marker;
            public SoundHandle Handle;
            public GameObject Emitter;
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
                default:
                    SoundEnded?.Invoke(pending.Handle, pending.Emitter);
                    break;
            }
        }

        #endregion
    }
}

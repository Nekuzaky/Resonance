namespace Nekuzaky.Resonance
{
    public enum PlayResult
    {
        Played,
        Stolen,
        RejectedLimit,
        RejectedVoices,
        Cooldown,
        Empty
    }

    public readonly struct EventLogEntry
    {
        public readonly float Time;
        public readonly string Event;
        public readonly string Emitter;
        public readonly PlayResult Result;

        public EventLogEntry(float time, string eventName, string emitter, PlayResult result)
        {
            Time = time;
            Event = eventName;
            Emitter = emitter;
            Result = result;
        }
    }

    public sealed class ResonanceStats
    {
        #region Public

        public const int LogCapacity = 256;

        public int Instances { get; internal set; }
        public int RealVoices { get; internal set; }
        public int VirtualVoices { get; internal set; }
        public int PooledVoices { get; internal set; }
        public double TickMilliseconds { get; internal set; }
        public double PeakTickMilliseconds { get; internal set; }
        public int TotalPlays { get; internal set; }
        public int TotalRejected { get; internal set; }
        public int LogVersion { get; private set; }
        public int LogCount => _count;

        public EventLogEntry LogAt(int newestFirst)
        {
            var index = (_head - 1 - newestFirst + LogCapacity * 2) % LogCapacity;
            return _log[index];
        }

        #endregion


        #region Private and Protected

        private readonly EventLogEntry[] _log = new EventLogEntry[LogCapacity];
        private int _head;
        private int _count;

        #endregion


        #region Main API

        internal void Record(EventLogEntry entry)
        {
            _log[_head] = entry;
            _head = (_head + 1) % LogCapacity;
            if (_count < LogCapacity)
                _count++;
            LogVersion++;
            if (entry.Result == PlayResult.Played || entry.Result == PlayResult.Stolen)
                TotalPlays++;
            else
                TotalRejected++;
        }

        internal void ClearLog()
        {
            _head = 0;
            _count = 0;
            LogVersion++;
        }

        #endregion
    }
}

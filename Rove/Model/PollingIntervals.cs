using System;
using System.Linq;

namespace Rove.Model
{
    public sealed class LinearPollingInterval
    {
        private readonly object _lock = new object();

        private DateTime lastUpdate = DateTime.Now;

        public void Reset()
        {
            lock (_lock)
            {
                lastUpdate = DateTime.Now;
            }
        }

        public bool IsTimeForPolling(TimeSpan duration)
        {
            if (duration == TimeSpan.Zero)
            {
                return false;
            }

            lock (_lock)
            {
                return (DateTime.Now - lastUpdate) > duration;
            }
        }
    }

    public sealed class BoundedExponentialPollingInterval
    {
        private static readonly TimeSpan[] intervals = new[]
        {
            TimeSpan.FromSeconds(0),
            TimeSpan.FromSeconds(0),
            TimeSpan.FromSeconds(0),
            TimeSpan.FromSeconds(0),
            TimeSpan.FromSeconds(0),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(20),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(60),
            TimeSpan.FromSeconds(120)
        };

        private readonly object _lock = new object();

        private DateTime lastCheck = DateTime.Now;

        private int intervalPtr = 0;

        public void Reset()
        {
            lock (_lock)
            {
                lastCheck = DateTime.Now;
                intervalPtr = 0;
            }
        }

        public bool IsTimeForPolling()
        {
            lock (_lock)
            {
                TimeSpan duration;
                if (intervalPtr < intervals.Length)
                {
                    duration = intervals[intervalPtr];
                    intervalPtr++;
                }
                else
                {
                    duration = intervals.Last();
                }

                bool shouldPoll = (DateTime.Now - lastCheck) > duration;
                if (shouldPoll)
                {
                    lastCheck = DateTime.Now;
                }

                return shouldPoll;
            }
        }
    }
}

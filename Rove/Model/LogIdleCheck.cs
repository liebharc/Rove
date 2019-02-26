using System;

namespace Rove.Model
{
    public sealed class LogIdleCheck
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

        public bool IsIdle(TimeSpan duration)
        {
            lock (_lock)
            {
                return (DateTime.Now - lastUpdate) > duration;
            }
        }
    }
}

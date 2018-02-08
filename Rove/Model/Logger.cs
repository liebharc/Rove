using System;
using System.Collections.Generic;

namespace Rove.Model
{
    public static class Logger
    {
        private static Queue<string> Messages = new Queue<string>();
        private static readonly int MessageHistoryLimit = 1000;

        public static IEnumerable<string> RecentMessages => Messages;

        public static event Action<string> NewMessage;

        public static void WriteInfo(string message)
        {
            lock (Messages)
            {
                Messages.Enqueue(message);
                while (Messages.Count > MessageHistoryLimit)
                {
                    Messages.Dequeue();
                }

                NewMessage?.Invoke(message);
            }
        }
    }
}

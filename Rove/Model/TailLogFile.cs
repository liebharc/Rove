using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Rove.Model
{
    public sealed class TailLogFile : IDisposable
    {
        private LogIdleCheck IdleCheck { get; } = new LogIdleCheck();

        private Thread Reader { get; set; }

        public FileInfo File { get; }

        private bool IsNewTailSessionInit { get; }

        public volatile bool _isActive = true;

        public event Action<bool, int, List<string>> NewMessagesArrived;

        public TailLogFile(FileInfo file, bool isNewTailSession)
        {
            if (file == null || !file.Exists)
            {
                throw new ArgumentException(nameof(file));
            }

            File = file;
            IsNewTailSessionInit = isNewTailSession;
        }

        public bool IsIdle(TimeSpan duration)
        {
            return IdleCheck.IsIdle(duration);
        }

        public void RememberIdleCheck()
        {
            IdleCheck.Reset();
        }

        public void Start()
        {
            if (Reader != null)
            {
                return;
            }

            Reader = new Thread(Read) { IsBackground = true };
            Reader.Start();
        }

        private void Read()
        {
            using (StreamReader reader = new StreamReader(new FileStream(File.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                long lastMaxOffset = reader.BaseStream.Length;
                bool isNewTailSession = IsNewTailSessionInit;
                while (_isActive)
                {
                    if (reader.BaseStream.Length != lastMaxOffset)
                    {
                        reader.BaseStream.Seek(lastMaxOffset, SeekOrigin.Begin);
                        string line;
                        int charCount = 0;
                        List<string> lines = new List<string>();
                        while ((line = reader.ReadLine()) != null)
                        {
                            lines.Add(line);
                            charCount += line.Length;
                        }

                        lastMaxOffset = reader.BaseStream.Position;
                        NewMessagesArrived?.Invoke(isNewTailSession, charCount, lines);
                        isNewTailSession = false;
                        IdleCheck.Reset();
                    }

                    Thread.Sleep(TimeSpan.FromMilliseconds(100));
                }
            }
        }

        public void Dispose()
        {
            _isActive = false;
            if (Reader != null)
            {
                Reader.Join();
                Reader = null;
            }
        }
    }
}

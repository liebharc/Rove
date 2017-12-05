using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Rove.Model
{
    public sealed class TailLogFile : IDisposable
    {
        private Thread Reader { get; set; }

        public FileInfo File { get; }

        public volatile bool _isActive = true;

        public event Action<bool, List<string>> NewMessagesArrived;

        public TailLogFile(FileInfo file)
        {
            if (file == null || !file.Exists)
            {
                throw new ArgumentException(nameof(file));
            }

            File = file;
        }

        public void Start()
        {
            if (Reader != null)
            {
                return;
            }

            Reader = new Thread(Read);
            Reader.Start();
        }

        private void Read()
        {
            using (StreamReader reader = new StreamReader(new FileStream(File.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                long lastMaxOffset = reader.BaseStream.Length;
                bool isNewTailSession = true;
                while (_isActive)
                {
                    if (reader.BaseStream.Length != lastMaxOffset)
                    {
                        reader.BaseStream.Seek(lastMaxOffset, SeekOrigin.Begin);
                        string line;
                        List<string> lines = new List<string>();
                        while ((line = reader.ReadLine()) != null)
                        {
                            lines.Add(line);
                        }

                        lastMaxOffset = reader.BaseStream.Position;
                        NewMessagesArrived?.Invoke(isNewTailSession, lines);
                        isNewTailSession = false;
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

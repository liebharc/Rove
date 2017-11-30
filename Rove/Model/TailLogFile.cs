using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Rove.Model
{
    public sealed class TailLogFile : IDisposable
    {
        private Thread Reader { get; set; }

        public FileInfo File { get; }

        public volatile bool _isActive = true;

        public event Action<IEnumerable<string>> NewMessagesArrived;

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
                        NewMessagesArrived?.Invoke(lines);
                    }

                    Thread.Sleep(100);
                }
            }
        }

        public void Dispose()
        {
            if (Reader != null)
            {
                _isActive = false;
                Reader.Join();
                Reader = null;
            }
        }
    }
}

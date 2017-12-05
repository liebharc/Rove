using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Rove.Model
{
    public sealed class SeenProcess
    {
        public SeenProcess()
        {
        }

        public SeenProcess(int id, DateTime startTime)
        {
            Id = id;
            StartTime = startTime;
        }

        public int Id { get; }
        public DateTime StartTime { get; }

        public override bool Equals(object obj)
        {
            return Equals(obj as SeenProcess);
        }

        public bool Equals(SeenProcess other)
        {
            if (other == null)
            {
                return false;
            }

            return Id == other.Id && StartTime == other.StartTime;
        }

        public override int GetHashCode()
        {
            return 31 * Id.GetHashCode() + StartTime.GetHashCode();
        }
    }

    public class SeenProcessList
    {
        private List<SeenProcess> Processes { get; set; } = new List<SeenProcess>();

        public List<Process> GetNewProcesses(List<Process> processes)
        {
            var newProcesses = 
                processes
                .Where(p => !Processes.Any((s) => s.Id == p.Id && s.StartTime == p.StartTime))
                .ToList();

            Processes =
                processes
                .Select(p => new SeenProcess(p.Id, p.StartTime))
                .ToList();

            return newProcesses;
        }
    }

    public static class ProcessUtils
    {
        public static Result Run(FileInfo path, string arguments = null)
        {
            return Run(path.FullName, arguments);
        }

        public static Result Run(string command, string arguments = null)
        {
            try
            {
                if (arguments == null)
                {
                    arguments = string.Empty;
                }

                Process.Start(command, arguments);
                return Result.Success;
            }
            catch (Win32Exception ex)
            {
                return Result.Error(ex.Message);
            }
            catch (FileNotFoundException ex)
            {
                return Result.Error(ex.Message);
            }
            catch (ObjectDisposedException ex)
            {
                return Result.Error(ex.Message);
            }
        }
    }
}

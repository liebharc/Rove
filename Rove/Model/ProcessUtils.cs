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
                .SelectMany(SafeConvert)
                .ToList();

            return newProcesses;
        }

        private static List<SeenProcess> SafeConvert(Process process)
        {
            try
            {
                return new List<SeenProcess> { new SeenProcess(process.Id, process.StartTime) };
            }
            catch (InvalidOperationException)
            {
                return new List<SeenProcess>();
            }
            catch (PlatformNotSupportedException)
            {
                return new List<SeenProcess>();
            }
            catch (NotSupportedException)
            {
                return new List<SeenProcess>();
            }
            catch (Win32Exception)
            {
                return new List<SeenProcess>();
            }
        }
    }

    public static class ProcessUtils
    {
        public const DirectoryInfo DefaultWorkingDir = null;

        public static Result Run(ScriptPath path, CurrentRoveEnvironment environment, string arguments = null)
        {
            try
            {
                return Run(
                    path.ResolvePath(environment).FullName,
                    path.ResolveWorkingDir(environment),
                    arguments ?? path.ResolveArguments(environment));
            } catch (FileNotFoundException ex)
            {
                return Result.Error(ex.Message);
            }
        }

        public static Result Run(FileInfo path, DirectoryInfo workingDir, string arguments = null)
        {
            return Run(path.FullName, workingDir, arguments);
        }

        public static Result Run(string command, DirectoryInfo workingDir, string arguments = null)
        {
            try
            {
                if (arguments == null)
                {
                    arguments = string.Empty;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments
                };

                if (workingDir != null)
                {
                    startInfo.WorkingDirectory = workingDir.FullName;
                }

                Process.Start(startInfo);
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

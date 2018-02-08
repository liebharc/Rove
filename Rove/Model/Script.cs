using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Rove.Model
{
    public static class Script
    {
        public class ScriptResult
        {
            public ScriptResult(int exitCode, StreamReader stdOut, StreamReader stdErr)
            {
                ExitCode = exitCode;
                StdOutLinesLazy = new Lazy<List<string>>(() =>
                 {
                     List<string> result = new List<string>();
                     while (!stdOut.EndOfStream)
                     {
                         result.Add(stdOut.ReadLine());
                     }
                     return result;
                 });

                StdErrLinesLazy = new Lazy<List<string>>(() =>
                {
                    List<string> result = new List<string>();
                    while (!stdErr.EndOfStream)
                    {
                        result.Add(stdErr.ReadLine());
                    }
                    return result;
                });
            }

            public ScriptResult(int exitCode, string stdOut, string stdErr)
            {
                ExitCode = exitCode;
                StdOutLinesLazy = new Lazy<List<string>>(() =>
                {
                    List<string> result = new List<string>();
                    result.Add(stdOut);
                    return result;
                });

                StdErrLinesLazy = new Lazy<List<string>>(() =>
                {
                    List<string> result = new List<string>();
                    result.Add(stdErr);
                    return result;
                });
            }

            public int ExitCode { get; }
            private Lazy<List<string>> StdOutLinesLazy { get; }
            private Lazy<List<string>> StdErrLinesLazy { get; }
            public List<string> StdOut => StdOutLinesLazy.Value;
            public List<string> StdErr => StdErrLinesLazy.Value;

            public Result Check()
            {
                if (StdErr.Count != 0)
                {
                    return Result.Error("Process has written errors:\n" + string.Join("\n", StdErr));
                }

                if (ExitCode != 0)
                {
                    return Result.Error("Process exited with " + ExitCode);
                }

                return Result.Success;
            }
        }

        public static ScriptResult Run(ScriptPath script, CurrentRoveEnvironment roveEnvironment, IEnumerable<string> arguments = null, IDictionary<string, string> environment = null)
        {
            try
            {
                return Run(script.ResolvePath(roveEnvironment), arguments, environment);
            }
            catch (FileNotFoundException ex)
            {
                return new ScriptResult(999, string.Empty, ex.Message);
            }
        }

        public static ScriptResult Run(FileInfo script, IEnumerable<string> arguments = null, IDictionary<string, string> environment = null)
        {
            if (script.Extension == ".ps1")
            {
                return RunPowerShell(script, arguments, environment);
            }

            return RunCmdOrExe(script, arguments, environment);
        }

        private static ScriptResult RunCmdOrExe(FileInfo script, IEnumerable<string> arguments, IDictionary<string, string> environment)
        {
            var ps = new ProcessStartInfo();
            ps.FileName = script.FullName;
            if (arguments != null)
            {
                ps.Arguments = string.Join(" ", arguments);
            }

            SetEnvironment(environment, ps);
            SetProcessDefaultValues(ps);

            var process = Process.Start(ps);

            return new ScriptResult(process.ExitCode, process.StandardOutput, process.StandardError);
        }

        private static void SetProcessDefaultValues(ProcessStartInfo ps)
        {
            ps.UseShellExecute = false;
            ps.RedirectStandardOutput = true;
            ps.RedirectStandardError = true;
            ps.CreateNoWindow = true;
        }

        private static void SetEnvironment(IDictionary<string, string> environment, ProcessStartInfo ps)
        {
            if (environment != null)
            {
                foreach (var pair in environment)
                {
                    ps.Environment.Add(pair.Key, pair.Value);
                }
            }
        }

        private static ScriptResult RunPowerShell(FileInfo script, IEnumerable<string> arguments, IDictionary<string, string> environment)
        {
            var ps = new ProcessStartInfo();
            ps.FileName = "powershell";
            if (arguments != null)
            {
                ps.Arguments = script + " " + string.Join(" ", arguments);
            }

            SetEnvironment(environment, ps);
            SetProcessDefaultValues(ps);

            var process = Process.Start(ps);
            process.WaitForExit();

            return new ScriptResult(process.ExitCode, process.StandardOutput, process.StandardError);
        }
    }
}

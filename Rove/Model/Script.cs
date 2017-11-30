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
                    while (!stdOut.EndOfStream)
                    {
                        result.Add(stdErr.ReadLine());
                    }
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
                if (ExitCode != 0)
                {
                    return Result.Error("Process exited with " + ExitCode);
                }

                if (StdOut.Count != 0)
                {
                    return Result.Error("Process has written errors:\n" + string.Join("\n", StdErr));
                }

                return Result.Success;
            }
        }

        public static ScriptResult Run(FileInfo script, IEnumerable<string> arguments = null, IDictionary<string, string> environment = null)
        {
            var ps = new ProcessStartInfo();
            ps.FileName = script.FullName;
            if (arguments != null)
            {
                ps.Arguments = string.Join(" ", arguments);
            }

            if (environment != null)
            {
                foreach (var pair in environment)
                {
                    ps.Environment.Add(pair.Key, pair.Value);
                }
            }

            ps.UseShellExecute = false;
            ps.RedirectStandardOutput = true;
            ps.RedirectStandardError = true;

            var process = Process.Start(ps);

            return new ScriptResult(process.ExitCode, process.StandardOutput, process.StandardError);
        }
    }
}

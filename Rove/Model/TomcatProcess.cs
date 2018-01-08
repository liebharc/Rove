using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.IO;
using System.Management;

namespace Rove.Model
{
    public sealed class TomcatProcessInfo
    {
        public static IEnumerable<TomcatProcessInfo> NewRunningTomcatProcesses(SeenProcessList seen)
        {
            return seen.GetNewProcesses(Process.GetProcessesByName("java").ToList())
                .Select(p => new TomcatProcessInfo(p))
                .ToList();
        }

        private Process Process { get; set; }

        public int Id => Process.Id;

        private Lazy<string> CommandLineInternal { get; }

        public string CommandLine => CommandLineInternal.Value;

        public TomcatProcessInfo(Process process)
        {
            if (process == null)
            {
                throw new ArgumentNullException(nameof(process));
            }

            Process = process;
            CommandLineInternal = new Lazy<string>(() => {
                string wmiQuery = string.Format("select CommandLine from Win32_Process where ProcessID ='{0}'", Process.Id);
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(wmiQuery);
                ManagementObjectCollection retObjectCollection = searcher.Get();
                foreach (ManagementObject retObject in retObjectCollection)
                    return "" + retObject["CommandLine"];
                return process.ProcessName;
            });
        }

        public TomcatProcessControl Control()
        {
            return new TomcatProcessControl(Process, CommandLine);
        }
    }

    public sealed class TomcatProcessControl : IDisposable
    {
        private Process WorkerProcess { get; set; }

        public string CommandLine { get; }

        private Process GuiProcess { get; set; }

        private IntPtr MainWindowHandle { get; set; }

        private bool IsDisposedInternal { get; set; } = false;

        public int Id { get; }

        public bool IsDisposed {
            get
            {
                try
                {
                    return IsDisposedInternal || WorkerProcess.HasExited;
                }
                catch (InvalidOperationException)
                {
                    return true;
                }
                catch (NotSupportedException)
                {
                    return true;
                }
                catch (Win32Exception)
                {
                    return true;
                }
            }
        }

        public TomcatProcessControl(Process process, string commandLine)
        {
            if (process == null)
            {
                throw new ArgumentNullException(nameof(process));
            }

            Logger.WriteInfo("Taking control over: " + process.ToDebugString());

            var parent = process.Parent();
            if (parent != null)
            {
                Logger.WriteInfo("Found parent process: " + parent.ToDebugString());

                var siblings = 
                    parent.FindChildren()
                    .Where(s => s.Id != process.Id)
                    .ToList();
                foreach (var sibling in siblings)
                {
                    Logger.WriteInfo("Found sibling process: " + sibling.ToDebugString());
                }

                var children = process.FindChildren();
                foreach (var child in children)
                {
                    Logger.WriteInfo("Found child process: " + child.ToDebugString());
                }

                const string CONHOST = "conhost";
                const string CMD = "cmd";
                if (children.Any(s => s.ProcessName == CONHOST))
                {
                    Logger.WriteInfo("This process has conhost process as child, using process directly as GUI process");
                    GuiProcess = parent;
                }
                else if (siblings.Any(s => s.ProcessName == CONHOST))
                {
                    Logger.WriteInfo("Parent has conhost process as child, using parent as GUI process");
                    GuiProcess = parent;
                }
                else if (parent.ProcessName == CMD)
                {
                    Logger.WriteInfo("Parent process is a cmd process, using parent as GUI process");
                    GuiProcess = parent;
                }
                else
                {
                    Logger.WriteInfo("Failed to identify a console host, using process directly as GUI process");
                    GuiProcess = process;
                }
            }
            else
            {
                Logger.WriteInfo("Found no parent process, using process directly as GUI process");
                GuiProcess = process;
            }

            MainWindowHandle = WaitForMainWindowHandleToBecomeAvailable(GuiProcess);

            WorkerProcess = process;
            CommandLine = commandLine;
            Id = WorkerProcess.Id;
            Hide();

            Logger.WriteInfo("Taking control over: " + process.ToDebugString() + " - done");
        }

        private IntPtr WaitForMainWindowHandleToBecomeAvailable(Process process)
        {
            IntPtr mainWindowHandle = process.MainWindowHandle;
            DateTime start = DateTime.Now;
            while (MainWindowHandle == IntPtr.Zero && DateTime.Now - start < TimeSpan.FromMilliseconds(200))
            {
                mainWindowHandle = process.MainWindowHandle;
            }

            return mainWindowHandle;
        }

        public void Kill()
        {
            if (!IsDisposed)
            {
                WorkerProcess.Kill();
                IsDisposedInternal = true;
            }
        }

        public void Show()
        {
            User32.ShowWindow(MainWindowHandle, User32.SW_SHOW);
        }

        public void Hide()
        {
            if (MainWindowHandle != IntPtr.Zero)
            {
                User32.ShowWindow(MainWindowHandle, User32.SW_HIDE);
            } else
            {
                Logger.WriteInfo("Process " + GuiProcess.Id + " has no main window to hide");
            }
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                Show();
                IsDisposedInternal = true;
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as TomcatProcessControl);
        }
        
        public bool Equals(TomcatProcessControl other)
        {
            if (other == null)
            {
                return false;
            }

            return WorkerProcess.Id == other.WorkerProcess.Id;
        }

        public override int GetHashCode()
        {
            return WorkerProcess.Id;
        }

        internal void Update()
        {
            if (!IsDisposed && GuiProcess.HasExited)
            {
                Logger.WriteInfo("GUI process " + GuiProcess.Id + " has exited, but worker is still available. Hiding worker " + WorkerProcess.Id);
                GuiProcess = WorkerProcess;
                MainWindowHandle = WaitForMainWindowHandleToBecomeAvailable(GuiProcess);
                Hide();
            }
        }
    }

    internal static class User32
    {
        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        internal const int SW_HIDE = 0;

        internal const int SW_SHOW = 5;
    }
}

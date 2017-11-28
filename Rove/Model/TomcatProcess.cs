using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Rove.Model
{
    public sealed class TomcatProcessInfo
    {
        public static IEnumerable<TomcatProcessInfo> RunningTomcatProcesses
        {
            get
            {
                return Process.GetProcessesByName("notepad")
                    .Select(p => new TomcatProcessInfo(p))
                    .ToList();
            }
        }

        private Process Process { get; set; }

        public int Id => Process.Id;

        private Lazy<string> CommandLine { get; }

        public TomcatProcessInfo(Process process)
        {
            if (process == null)
            {
                throw new ArgumentNullException(nameof(process));
            }

            Process = process;
            CommandLine = new Lazy<string>(() => string.Format("select CommandLine from Win32_Process where ProcessID ='{0}'", Process.Id));
        }

        public TomcatProcessControl Control()
        {
            return new TomcatProcessControl(Process);
        }
    }

    public sealed class TomcatProcessControl : IDisposable
    {
        private Process Process { get; set; }

        private IntPtr MainWindowHandle { get; }

        private bool IsDisposedInternal { get; set; } = false;

        public bool IsDisposed => IsDisposedInternal || Process.HasExited;

        public int Id { get; }

        public TomcatProcessControl(Process process)
        {
            if (process == null)
            {
                throw new ArgumentNullException(nameof(process));
            }

            Process = process;
            MainWindowHandle = WaitForMainWindowHandleToBecomeAvailable(process);
            Id = process.Id;
            Hide();
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
                Process.Kill();
                IsDisposedInternal = true;
            }
        }

        public void Show()
        {
            User32.ShowWindow(MainWindowHandle, User32.SW_SHOW);
        }

        public void Hide()
        {
            User32.ShowWindow(MainWindowHandle, User32.SW_HIDE);
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

            return Process.Id == other.Process.Id;
        }

        public override int GetHashCode()
        {
            return Process.Id;
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

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
                return Process.GetProcessesByName("java.exe")
                    .Select(p => new TomcatProcessInfo(p))
                    .ToList();
            }
        }

        private Process Process { get; set; }

        private Lazy<string> CommandLine { get; }

        public TomcatProcessInfo(Process process)
        {
            Process = process;
            CommandLine = new Lazy<string>(() => string.Format("select CommandLine from Win32_Process where ProcessID ='{0}'", Process.Id));
        }
    }

    public sealed class TomcatProcessControl : IDisposable
    {
        private Process Process { get; set; }

        private IntPtr MainWindowHandle { get; }

        private bool IsDisposed { get; set; } = false;

        public TomcatProcessControl(Process process)
        {
            if (process == null)
            {
                throw new ArgumentNullException(nameof(process));
            }

            Process = process;
            MainWindowHandle = process.MainWindowHandle;
            User32.ShowWindow(MainWindowHandle, User32.SW_HIDE);
        }

        public void Dispose()
        {
            if (!IsDisposed)
            {
                User32.ShowWindow(MainWindowHandle, User32.SW_SHOW);
                IsDisposed = true;
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

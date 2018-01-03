using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Management;

namespace Rove.Model
{
    internal static class ProcessExtensions
    {
        public static Process Parent(this Process process)
        {
            if (process == null)
            {
                throw new ArgumentNullException(nameof(process));
            }

            return FindParentPidFromIndexedProcessName(FindIndexedProcessName(process));
        }

        private static Process FindParentPidFromIndexedProcessName(string indexedProcessName)
        {
            var parentId = new PerformanceCounter("Process", "Creating Process ID", indexedProcessName);
            try
            {
                return Process.GetProcessById((int)parentId.NextValue());
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private static string FindIndexedProcessName(Process process)
        {
            var processName = process.ProcessName;
            var processesByName = Process.GetProcessesByName(processName);
            string processIndexedName = null;
            for (int index = 0; index < processesByName.Length; index++)
            {
                processIndexedName = index == 0 ? processName : processName + "#" + index;
                var processId = new PerformanceCounter("Process", "ID Process", processIndexedName);
                if ((int)processId.NextValue() == process.Id)
                {
                    return processIndexedName;
                }
            }

            return processIndexedName;
        }

        public static List<Process> FindChildren(this Process parent)
        {
            var ids = FindChildProcessIds(parent);
            var children = new List<Process>();
            foreach (var id in ids)
            {
                children.Add(Process.GetProcessById((int)id));
            }

            return children;
        }

        private static List<uint> FindChildProcessIds(Process parent)
        {
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(
            "SELECT * " +
            "FROM Win32_Process " +
            "WHERE ParentProcessId=" + parent.Id);
            ManagementObjectCollection collection = searcher.Get();
            var children = new List<uint>();
            foreach (var item in collection)
            {
                children.Add((uint)item["ProcessId"]);
            }

            return children;
        }

        public static string ToDebugString(this Process process)
        {
            return process.Id + "/" + process.ProcessName;
        }
    }
}

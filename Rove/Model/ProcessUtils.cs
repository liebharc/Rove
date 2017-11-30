using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace Rove.Model
{
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

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;

namespace Rove.Model
{
    public static class ProcessUtils
    {
        public static Result Run(FileInfo path)
        {
            try
            {
                Process.Start(path.FullName);
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

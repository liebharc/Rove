using System.IO;
using System.Text.RegularExpressions;

namespace Rove.Model
{
    public class OverallConfig
    {
        public FileInfo OnNewProcessScript { get; set; }
    }

    public class ProcessConfig
    {
        string ProcessName { get; set; }

        public Regex WarningMessage { get; set; }

        public Regex ErrorMessage { get; set; }

        public FileInfo OnProcessStartedScript { get; set; }

        public FileInfo FindLogFileScript { get; set; }

        public FileInfo IsKnownProcessScript { get; set; }
    }
}

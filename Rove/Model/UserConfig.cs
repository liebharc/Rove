using System.Collections.Generic;

namespace Rove.Model
{
    public class UserConfig
    {
        public static UserConfig DefaultConfig
        {
            get
            {
                return new UserConfig();
            }
        }

        public string DisplayLayout { get; set; } = string.Empty;

        public string CurrentRoveEnvironment { get; set; } = string.Empty;

        public List<ProcessUserConfig> ProcessConfigs { get; set; } = new List<ProcessUserConfig>();
    }

    public class ProcessUserConfig
    {
        public string ProcessName { get; set; } = string.Empty;
        public bool AutoScroll { get; set; } = true;
    }
}

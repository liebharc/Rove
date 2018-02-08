using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Xml.Serialization;

namespace Rove.Model
{
    public class OverallConfig
    {
        public static OverallConfig DefaultConfig { 
            get
            {
                var config = new OverallConfig
                {
                    OnAnyProcessStartedScript = "StartupScript.ps1",
                    LogHistory = 10000,
                    UpdateLimit = 50000
                };

                var process = new ProcessConfig
                {
                    ProcessName = "ProcessName",
                    ErrorMessage = ".*ERROR.*",
                    WarningMessage = ".*WARN.*",
                    StartupMessage = ".*started.*",
                    OnProcessStartedScript = "OnProcessStartedScript.ps1",
                    FindLogFileScript = "FindLogFileScript.ps1",
                    IsKnownProcess= ".*",
                    StartProcessScript ="StartProcessScript.ps1"
                };

                config.ProcessConfigs.Add(process);
                return config;
            }
        }

        public string OnAnyProcessStartedScript { get; set; } = string.Empty;

        public List<ProcessConfig> ProcessConfigs { get; } = new List<ProcessConfig>();

        public int LogHistory { get; set; } = 1000;

        public int UpdateLimit { get; set; } = 50000;

        public OverallConfigChecked ToOverallConfig(UserConfig userSerialized)
        {
            return new OverallConfigChecked(this, userSerialized);
        }
    }

    public class OverallConfigChecked
    {
        public OverallConfigChecked(OverallConfig serialized, UserConfig userSerialized)
        {
            OnNewProcessScript = Converstions.GetOptionalPath(nameof(OverallConfig), nameof(serialized.OnAnyProcessStartedScript), serialized.OnAnyProcessStartedScript);
            LogHistory = serialized.LogHistory;
            if (LogHistory < 0)
            {
                throw new ConfigException(nameof(OverallConfig), nameof(LogHistory));
            }
            UpdateLimit = serialized.UpdateLimit;
            if (UpdateLimit < 0)
            {
                throw new ConfigException(nameof(OverallConfig), nameof(UpdateLimit));
            }

            foreach (var s in serialized.ProcessConfigs)
            {
                var userConfig = userSerialized.ProcessConfigs.FirstOrDefault(p => p.ProcessName == s.ProcessName);
                ProcessConfigs.Add(s.ToProcessConfig(userConfig));
            }
        }

        public string DisplayLayout { get; }
        public FileInfo OnNewProcessScript { get; }
        public int LogHistory { get; }
        public int UpdateLimit { get; }
        public List<ProcessConfigChecked> ProcessConfigs { get; } = new List<ProcessConfigChecked>();
    }

    public class ProcessConfig
    {
        public string ProcessName { get; set; } = string.Empty;

        public string WarningMessage { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;

        public string OnProcessStartedScript { get; set; } = string.Empty;

        public string FindLogFileScript { get; set; } = string.Empty;

        public string IsKnownProcess { get; set; } = string.Empty;

        public string StartProcessScript { get; set; } = string.Empty;

        public string StartupMessage { get; set; } = string.Empty;

        public string Color { get; set; } = string.Empty;

        public ProcessConfigChecked ToProcessConfig(ProcessUserConfig userSerialized)
        {
            return new ProcessConfigChecked(this, userSerialized);
        }
    }

    public class ProcessConfigChecked
    {
        public ProcessConfigChecked(ProcessConfig serialized, ProcessUserConfig userSerialized)
        {
            if (string.IsNullOrEmpty(serialized.ProcessName))
            {
                throw new ArgumentException("Unnamed section", nameof(serialized.ProcessName));
            }

            ProcessName = serialized.ProcessName;
            WarningMessage = Converstions.CompileRegex(serialized.ProcessName, nameof(serialized.WarningMessage), serialized.WarningMessage);
            ErrorMessage = Converstions.CompileRegex(serialized.ProcessName, nameof(serialized.ErrorMessage), serialized.ErrorMessage);
            StartupMessage = Converstions.CompileRegex(serialized.ProcessName, nameof(serialized.StartupMessage), serialized.StartupMessage);
            OnProcessStartedScript = Converstions.GetOptionalPath(serialized.ProcessName, nameof(serialized.OnProcessStartedScript), serialized.OnProcessStartedScript);
            FindLogFileScript = Converstions.GetMandatoryPath(serialized.ProcessName, nameof(serialized.FindLogFileScript), serialized.FindLogFileScript);
            IsKnownProcess = Converstions.CompileRegex(serialized.ProcessName, nameof(serialized.IsKnownProcess), serialized.IsKnownProcess);
            StartProcessScript = Converstions.GetMandatoryPath(serialized.ProcessName, nameof(serialized.StartProcessScript), serialized.StartProcessScript);
            Color = Converstions.GetColor(serialized.ProcessName, nameof(serialized.Color), serialized.Color);
            AutoScroll = userSerialized != null ? userSerialized.AutoScroll : true;
        }

        public string ProcessName { get; } 

        public Regex WarningMessage { get; }

        public Regex ErrorMessage { get; }

        public Regex StartupMessage { get; }

        public FileInfo OnProcessStartedScript { get; }

        public FileInfo FindLogFileScript { get; }

        public Regex IsKnownProcess { get; }

        public FileInfo StartProcessScript { get; }

        public Color Color { get; }

        public bool AutoScroll { get; set; }
    }

    public static class Converstions
    {
        public static Regex CompileRegex(string section, string argName, string regex)
        {
            try
            {
                // Remove trailing .* since they aren't required and cost a lot of performance
                if (regex.StartsWith(".*"))
                {
                    regex = regex.Substring(2);
                }

                if (regex.EndsWith(".*"))
                {
                    regex = regex.Substring(0, regex.Length - 2);
                }

                return new Regex(regex, RegexOptions.Compiled);
            }
            catch (ArgumentException ex)
            {
                throw new ConfigException(section, argName, ex);
            }
        }

        public static FileInfo GetOptionalPath(string section, string argName, string path)
        {
            path = path.Trim(new[] { '"' });
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            return GetMandatoryPath(section, argName, path);
        }

        public static FileInfo GetMandatoryPath(string section, string argName, string path)
        {
            path = path.Trim(new[] { '"' });
            if (string.IsNullOrEmpty(path))
            {
                throw new ConfigException(section, argName);
            }

            if (!File.Exists(path))
            {
                throw new ConfigException(section, argName);
            }

            return new FileInfo(path);
        }

        internal static Color GetColor(string section, string argName, string color)
        {
            try
            {
                return (Color)ColorConverter.ConvertFromString(color);
            }
            catch (NotSupportedException ex)
            {
                throw new ConfigException(section, argName, ex);
            }
        }
    }

    public static class ConfigSerializer
    {
        public static string ConfigToText<T>(T config)
        {
            XmlSerializer xml = new XmlSerializer(typeof(T));
            using (StringWriter stringWriter = new StringWriter())
            {
                xml.Serialize(stringWriter, config);
                return stringWriter.ToString();
            }
        }

        public static T TextToConfig<T>(string config)
        {
            XmlSerializer xml = new XmlSerializer(typeof(T));
            using (StringReader stringReader = new StringReader(config))
            {
                return (T)xml.Deserialize(stringReader);
            }
        }
    }
}

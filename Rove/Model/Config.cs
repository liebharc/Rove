using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Xml.Serialization;

namespace Rove.Model
{
    public class ConfigException : Exception
    {
        public ConfigException(string section, string value, Exception innerException = null) : base("Configuration failure in section " + section + " for value " + value, innerException)
        {
            Section = section;
            Value = value;
        }

        public string Section { get; }
        public string Value { get; }
    }

    public class OverallConfig
    {
        public static OverallConfig DefaultConfig { 
            get
            {
                var config = new OverallConfig
                {
                    OnAnyProcessStartedScript = "StartupScript.ps1",
                    LogHistory = 10000,
                    UpdateLimit = 50000,
                    DisplayLayout = string.Empty
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
                    StartProcessScript ="StartProcessScript.ps1",
                    AutoScroll = true
                };

                config.ProcessConfigs.Add(process);
                return config;
            }
        }

        public string DisplayLayout { get; set; } = string.Empty;

        public string OnAnyProcessStartedScript { get; set; } = string.Empty;

        public List<ProcessConfig> ProcessConfigs { get; } = new List<ProcessConfig>();

        public int LogHistory { get; set; } = 1000;

        public int UpdateLimit { get; set; } = 50000;

        public OverallConfigChecked ToOverallConfig()
        {
            return new OverallConfigChecked(this);
        }
    }

    public class OverallConfigChecked
    {
        public OverallConfigChecked(OverallConfig ser)
        {
            OnNewProcessScript = Converstions.GetOptionalPath(nameof(OverallConfig), nameof(ser.OnAnyProcessStartedScript), ser.OnAnyProcessStartedScript);
            DisplayLayout = ser.DisplayLayout;
            LogHistory = ser.LogHistory;
            if (LogHistory < 0)
            {
                throw new ConfigException(nameof(OverallConfig), nameof(LogHistory));
            }
            UpdateLimit = ser.UpdateLimit;
            if (UpdateLimit < 0)
            {
                throw new ConfigException(nameof(OverallConfig), nameof(UpdateLimit));
            }

            foreach (var s in ser.ProcessConfigs)
            {
                ProcessConfigs.Add(s.ToProcessConfig());
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

        public bool AutoScroll { get; set; } = true;

        public ProcessConfigChecked ToProcessConfig()
        {
            return new ProcessConfigChecked(this);
        }
    }

    public class ProcessConfigChecked
    {
        public ProcessConfigChecked(ProcessConfig ser)
        {
            if (string.IsNullOrEmpty(ser.ProcessName))
            {
                throw new ArgumentException("Unnamed section", nameof(ser.ProcessName));
            }

            ProcessName = ser.ProcessName;
            WarningMessage = Converstions.CompileRegex(ser.ProcessName, nameof(ser.WarningMessage), ser.WarningMessage);
            ErrorMessage = Converstions.CompileRegex(ser.ProcessName, nameof(ser.ErrorMessage), ser.ErrorMessage);
            StartupMessage = Converstions.CompileRegex(ser.ProcessName, nameof(ser.StartupMessage), ser.StartupMessage);
            OnProcessStartedScript = Converstions.GetOptionalPath(ser.ProcessName, nameof(ser.OnProcessStartedScript), ser.OnProcessStartedScript);
            FindLogFileScript = Converstions.GetMandatoryPath(ser.ProcessName, nameof(ser.FindLogFileScript), ser.FindLogFileScript);
            IsKnownProcess = Converstions.CompileRegex(ser.ProcessName, nameof(ser.IsKnownProcess), ser.IsKnownProcess);
            StartProcessScript = Converstions.GetMandatoryPath(ser.ProcessName, nameof(ser.StartProcessScript), ser.StartProcessScript);
            Color = Converstions.GetColor(ser.ProcessName, nameof(ser.Color), ser.Color);
            AutoScroll = ser.AutoScroll;
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

        public bool AutoScroll { get; }
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
        public static string ConfigToText(OverallConfig config)
        {
            XmlSerializer xml = new XmlSerializer(typeof(OverallConfig));
            using (StringWriter stringWriter = new StringWriter())
            {
                xml.Serialize(stringWriter, config);
                return stringWriter.ToString();
            }
        }

        public static OverallConfig TextToConfig(string config)
        {
            XmlSerializer xml = new XmlSerializer(typeof(OverallConfig));
            using (StringReader stringReader = new StringReader(config))
            {
                return (OverallConfig)xml.Deserialize(stringReader);
            }
        }
    }
}

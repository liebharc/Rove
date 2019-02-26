using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Media;
using System.Xml.Serialization;

namespace Rove.Model
{
    public class EnvironmentEntry
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class Executable
    {
        public Executable() { }

        public Executable(string path) { Path = path; }

        public string Path { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public string WorkingDir { get; set; } = string.Empty;

        public void Trim()
        {
            Path = Path.Trim(new[] { '"' });
            WorkingDir = WorkingDir.Trim(new[] { '"' });
        }

        internal void ReplaceVariables()
        {
            Path = Path.Replace("$cwd", AppDomain.CurrentDomain.BaseDirectory);
            WorkingDir = WorkingDir.Replace("$cwd", AppDomain.CurrentDomain.BaseDirectory);
        }
    }

    public class OverallConfig
    {
        public static OverallConfig DefaultConfig { 
            get
            {
                var config = new OverallConfig
                {
                    OnAnyProcessStartedScript = new Executable("StartupScript.ps1"),
                    SetRoveEnvScript = new Executable("SetRoveEnvScript.ps1"),
                    LogHistory = 10000,
                    UpdateLimit = 50000
                };

                config.RoveEnvironment.Add(new EnvironmentEntry{ Key = "Dev", Value = "c:\\dev"});

                var process = new ProcessConfig
                {
                    ProcessName = "ProcessName",
                    ErrorMessage = ".*ERROR.*",
                    WarningMessage = ".*WARN.*",
                    StartupMessage = ".*started.*",
                    OnProcessStartedScript = new Executable("OnProcessStartedScript.ps1"),
                    FindLogFileScript = new Executable("FindLogFileScript.ps1"),
                    IsKnownProcess= ".*",
                    StartProcessScript = new Executable("StartProcessScript.ps1")
                };

                config.ProcessConfigs.Add(process);
                return config;
            }
        }

        public Executable OnAnyProcessStartedScript { get; set; } = new Executable();

        public Executable SetRoveEnvScript { get; set; } = new Executable();

        public List<ProcessConfig> ProcessConfigs { get; } = new List<ProcessConfig>();

        public List<EnvironmentEntry> RoveEnvironment { get; } = new List<EnvironmentEntry>();

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
            RoveEnvironments = new RoveEnvironments(serialized.RoveEnvironment);
            OnNewProcessScript = Converstions.GetOptionalPath(nameof(OverallConfig), nameof(serialized.OnAnyProcessStartedScript), serialized.OnAnyProcessStartedScript, RoveEnvironments);
            SetRoveEnvScript = Converstions.GetOptionalPath(nameof(OverallConfig), nameof(serialized.SetRoveEnvScript), serialized.SetRoveEnvScript, RoveEnvironments);
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
                ProcessConfigs.Add(s.ToProcessConfig(userConfig, RoveEnvironments));
            }
        }

        public string DisplayLayout { get; }
        public ScriptPath OnNewProcessScript { get; }
        public ScriptPath SetRoveEnvScript { get; }
        public RoveEnvironments RoveEnvironments { get; }
        public int LogHistory { get; }
        public int UpdateLimit { get; }
        public List<ProcessConfigChecked> ProcessConfigs { get; } = new List<ProcessConfigChecked>();
    }

    public class ProcessConfig
    {
        public string ProcessName { get; set; } = string.Empty;

        public string WarningMessage { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;

        public Executable OnProcessStartedScript { get; set; } = new Executable();

        public Executable FindLogFileScript { get; set; } = new Executable();

        public string IsKnownProcess { get; set; } = string.Empty;

        public Executable StartProcessScript { get; set; } = new Executable();

        public string StartupMessage { get; set; } = string.Empty;

        public string Color { get; set; } = string.Empty;

        public ProcessConfigChecked ToProcessConfig(ProcessUserConfig userSerialized, RoveEnvironments environments)
        {
            return new ProcessConfigChecked(this, userSerialized, environments);
        }
    }

    public class ProcessConfigChecked
    {
        public ProcessConfigChecked(ProcessConfig serialized, ProcessUserConfig userSerialized, RoveEnvironments environments)
        {
            if (string.IsNullOrEmpty(serialized.ProcessName))
            {
                throw new ArgumentException("Unnamed section", nameof(serialized.ProcessName));
            }

            ProcessName = serialized.ProcessName;
            WarningMessage = Converstions.CompileRegex(serialized.ProcessName, nameof(serialized.WarningMessage), serialized.WarningMessage);
            ErrorMessage = Converstions.CompileRegex(serialized.ProcessName, nameof(serialized.ErrorMessage), serialized.ErrorMessage);
            StartupMessage = Converstions.CompileRegex(serialized.ProcessName, nameof(serialized.StartupMessage), serialized.StartupMessage);
            OnProcessStartedScript = Converstions.GetOptionalPath(serialized.ProcessName, nameof(serialized.OnProcessStartedScript), serialized.OnProcessStartedScript, environments);
            FindLogFileScript = Converstions.GetMandatoryPath(serialized.ProcessName, nameof(serialized.FindLogFileScript), serialized.FindLogFileScript, environments);
            IsKnownProcess = Converstions.CompileRegex(serialized.ProcessName, nameof(serialized.IsKnownProcess), serialized.IsKnownProcess);
            StartProcessScript = Converstions.GetMandatoryPath(serialized.ProcessName, nameof(serialized.StartProcessScript), serialized.StartProcessScript, environments);
            Color = Converstions.GetColor(serialized.ProcessName, nameof(serialized.Color), serialized.Color);
            AutoScroll = userSerialized != null ? userSerialized.AutoScroll : true;
        }

        public string ProcessName { get; } 

        public Regex WarningMessage { get; }

        public Regex ErrorMessage { get; }

        public Regex StartupMessage { get; }

        public ScriptPath OnProcessStartedScript { get; }

        public ScriptPath FindLogFileScript { get; }

        public Regex IsKnownProcess { get; }

        public ScriptPath StartProcessScript { get; }

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

        public static ScriptPath GetOptionalPath(string section, string argName, Executable executable, RoveEnvironments environments)
        {
            if (executable == null)
            {
                return null;
            }

            executable.Trim();
            if (string.IsNullOrEmpty(executable.Path))
            {
                return null;
            }

            return GetMandatoryPath(section, argName, executable, environments);
        }

        public static ScriptPath GetMandatoryPath(string section, string argName, Executable executable, RoveEnvironments environments)
        {
            executable.Trim();
            executable.ReplaceVariables();
            if (string.IsNullOrEmpty(executable.Path))
            {
                throw new ConfigException(section, argName);
            }

            var scriptPath = new ScriptPath(executable, environments);
            if (!scriptPath.Exists)
            {
                throw new ConfigException(section, argName);
            }

            return scriptPath;
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

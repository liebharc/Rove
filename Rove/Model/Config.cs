using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace Rove.Model
{
    public class OverallConfigSerialize
    {
        public static OverallConfigSerialize DefaultConfig { 
            get
            {
                var config = new OverallConfigSerialize
                {
                    OnNewProcessScript = "StartupScript.ps1"
                };

                var process = new ProcessConfigSerialize
                {
                    ProcessName = "ProcessName",
                    ErrorMessage = ".*ERROR.*",
                    WarningMessage = ".*WARN.*",
                    OnProcessStartedScript = "OnProcessStartedScript.ps1",
                    FindLogFileScript = "FindLogFileScript.ps1",
                    IsKnownProcessScript = "IsKnownProcessScript.ps1"
                };

                config.ProcessConfigs.Add(process);
                return config;
            }
        }

        public string OnNewProcessScript { get; set; } = string.Empty;

        public List<ProcessConfigSerialize> ProcessConfigs { get; } = new List<ProcessConfigSerialize>();

        public OverallConfig ToOverallConfig()
        {
            return new OverallConfig(this);
        }
    }

    public class OverallConfig
    {
        public OverallConfig(OverallConfigSerialize ser)
        {
            OnNewProcessScript = ProcessConfig.GetPath(nameof(ser.OnNewProcessScript), ser.OnNewProcessScript);
            foreach (var s in ser.ProcessConfigs)
            {
                ProcessConfigs.Add(s.ToProcessConfig());
            }
        }

        public FileInfo OnNewProcessScript { get; }

        public List<ProcessConfig> ProcessConfigs { get; } = new List<ProcessConfig>();
    }

    public class ProcessConfigSerialize
    {
        public string ProcessName { get; set; } = string.Empty;

        public string WarningMessage { get; set; } = string.Empty;

        public string ErrorMessage { get; set; } = string.Empty;

        public string OnProcessStartedScript { get; set; } = string.Empty;

        public string FindLogFileScript { get; set; } = string.Empty;

        public string IsKnownProcessScript { get; set; } = string.Empty;

        public ProcessConfig ToProcessConfig()
        {
            return new ProcessConfig(this);
        }
    }

    public class ProcessConfig
    {
        public ProcessConfig(ProcessConfigSerialize ser)
        {
            if (string.IsNullOrEmpty(ser.ProcessName))
            {
                throw new ArgumentException(nameof(ser.ProcessName));
            }

            ProcessName = ser.ProcessName;
            WarningMessage = CompileRegex(nameof(ser.WarningMessage), ser.WarningMessage);
            ErrorMessage = CompileRegex(nameof(ser.ErrorMessage), ser.ErrorMessage);
            OnProcessStartedScript = GetPath(nameof(ser.OnProcessStartedScript), ser.OnProcessStartedScript);
            FindLogFileScript = GetPath(nameof(ser.FindLogFileScript), ser.FindLogFileScript);
            IsKnownProcessScript = GetPath(nameof(ser.IsKnownProcessScript), ser.IsKnownProcessScript);
        }

        private static Regex CompileRegex(string argName, string regex)
        {
            try
            {
                return new Regex(regex);
            }
            catch (Exception)
            {
                throw new ArgumentException(argName);
            }
        }

        public static FileInfo GetPath(string argName, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            if (!File.Exists(path))
            {
                throw new ArgumentException(argName);
            }

            return new FileInfo(path);
        }


        public string ProcessName { get;} 

        public Regex WarningMessage { get; }

        public Regex ErrorMessage { get; }

        public FileInfo OnProcessStartedScript { get; }

        public FileInfo FindLogFileScript { get; }

        public FileInfo IsKnownProcessScript { get; }

    }

    public static class ConfigSerializer
    {
        public static string ConfigToText(OverallConfigSerialize config)
        {
            XmlSerializer xml = new XmlSerializer(typeof(OverallConfigSerialize));
            using (StringWriter stringWriter = new StringWriter())
            {
                xml.Serialize(stringWriter, config);
                return stringWriter.ToString();
            }
        }

        public static OverallConfigSerialize TextToConfig(string config)
        {
            XmlSerializer xml = new XmlSerializer(typeof(OverallConfigSerialize));
            using (StringReader stringReader = new StringReader(config))
            {
                return (OverallConfigSerialize)xml.Deserialize(stringReader);
            }
        }
    }
}

using System;

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
}

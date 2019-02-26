using System.Collections.Generic;
using System.Linq;

namespace Rove.Model
{
    public sealed class RoveEnvironments
    {
        private static readonly string RoveEnvMarker = "$RoveEnv";

        public RoveEnvironments(IEnumerable<EnvironmentEntry> mapping)
        {
            foreach (var map in mapping)
            {
                Mapping.Add(map.Key, map.Value);
            }
        }

        private IDictionary<string, string> Mapping { get; } = new Dictionary<string, string>();

        public IEnumerable<string> AvailableEnvironments => Mapping.Select(m => m.Key);

        public bool HasMappings => Mapping.Any();

        public IEnumerable<string> MapAllPath(string path)
        {
            if (!Mapping.Any() || !path.Contains(RoveEnvMarker))
            {
                yield return path;
            }
            else
            {
                foreach (var map in Mapping)
                {
                    yield return path.Replace(RoveEnvMarker, map.Value);
                }
            }
        }

        public string MapSelected(string path, CurrentRoveEnvironment current)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            var selection = current.Selection;
            if (!Mapping.ContainsKey(selection))
            {
                return path;
            }
            else
            {
                return path.Replace(RoveEnvMarker, Mapping[selection]);
            }
        }
    }
}

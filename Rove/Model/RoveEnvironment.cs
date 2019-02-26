using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Rove.Model
{
    public sealed class RoveEnvironments
    {
        private static readonly string RoveEnvMarker = "$RoveEnv";

        public RoveEnvironments(EnvironmentEntrySearch baseDirectory, IEnumerable<EnvironmentEntry> mapping)
        {
            foreach (var map in mapping.Concat(Search(baseDirectory)))
            {
                Mapping.Add(map.Key, map.Value);
            }
        }

        public static IEnumerable<EnvironmentEntry> Search(EnvironmentEntrySearch baseDirectory)
        {
            if (string.IsNullOrEmpty(baseDirectory.GitRepo))
            {
                return new List<EnvironmentEntry>();
            }

            if (string.IsNullOrEmpty(baseDirectory.BaseFolder))
            {
                throw new ArgumentException("BaseFolder can't be empty if GitRepo is used");
            }

            if (!Directory.Exists(baseDirectory.BaseFolder))
            {
                throw new FileNotFoundException(baseDirectory.BaseFolder + " is not a valid path");
            }

            var gitFiles = Directory.GetDirectories(baseDirectory.BaseFolder)
                .Where(d => DoesGitConfigMatch(Path.Combine(d, ".git", "config"), baseDirectory.GitRepo))
                .OrderBy(d => d)
                .ToList();

            return gitFiles.Select(d => new EnvironmentEntry { Key = Path.GetFileName(d), Value = d });
        }

        public static bool DoesGitConfigMatch(string file, string repoName)
        {
            if (!File.Exists(file))
            {
                return false;
            }

            var regex = new Regex("url\\s*=.*" + repoName);
            return File.ReadAllLines(file).Any(l => regex.IsMatch(l));
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

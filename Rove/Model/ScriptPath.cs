using System.IO;
using System.Linq;

namespace Rove.Model
{
    public sealed class ScriptPath
    {
        public ScriptPath(Executable executable, RoveEnvironments environments)
        {
            Path = executable.Path;
            Arguments = executable.Arguments;
            WorkingDir = executable.WorkingDir;
            Environments = environments;
        }

        public bool Exists
        {
            get
            {
                return Environments.MapAllPath(Path).Any(p => File.Exists(p));
            }
        }

        private string Path { get; }
        private string Arguments { get; }
        private string WorkingDir { get; }
        private RoveEnvironments Environments { get; }

        public FileInfo ResolvePath(CurrentRoveEnvironment currentEnvironment)
        {
            var path = Environments.MapSelected(Path, currentEnvironment);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(Path + " was resolved to " + path + " but it doesn't exist");
            }
            return new FileInfo(path);
        }

        public DirectoryInfo ResolveWorkingDir(CurrentRoveEnvironment currentEnvironment)
        {
            var workingDir = Environments.MapSelected(WorkingDir, currentEnvironment);
            if (!Directory.Exists(workingDir))
            {
                return null;
            }
            return new DirectoryInfo(workingDir);
        }

        public string ResolveArguments(CurrentRoveEnvironment currentEnvironment)
        {
            return Environments.MapSelected(Arguments, currentEnvironment);
        }
    }
}

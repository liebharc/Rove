using System.IO;
using System.Linq;

namespace Rove.Model
{
    public sealed class ScriptPath
    {
        public ScriptPath(Executable executable)
        {
            Path = executable.Path;
            Arguments = executable.Arguments;
            WorkingDir = executable.WorkingDir;
        }

        public bool Exists
        {
            get
            {
                return true;
            }
        }

        private string Path { get; }
        private string Arguments { get; }
        private string WorkingDir { get; }

        public FileInfo ResolvePath(CurrentRoveEnvironment currentEnvironment)
        {
            var path = currentEnvironment.MapSelected(Path);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException(Path + " was resolved to " + path + " but it doesn't exist");
            }
            return new FileInfo(path);
        }

        public DirectoryInfo ResolveWorkingDir(CurrentRoveEnvironment currentEnvironment)
        {
            var workingDir = currentEnvironment.MapSelected(WorkingDir);
            if (!Directory.Exists(workingDir))
            {
                return null;
            }
            return new DirectoryInfo(workingDir);
        }

        public string ResolveArguments(CurrentRoveEnvironment currentEnvironment)
        {
            return currentEnvironment.MapSelected(Arguments);
        }
    }
}

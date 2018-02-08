using System.IO;
using System.Linq;

namespace Rove.Model
{
    public sealed class ScriptPath
    {
        public ScriptPath(string path, RoveEnvironments environments)
        {
            Path = path;
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
    }
}

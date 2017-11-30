using System.Collections.Generic;
using System.IO;

namespace Rove.Model
{
    public static class Script
    {
        public class Result
        {
            public Result(FileInfo script, IEnumerable<string> arguments, IDictionary<string, string> environment)
            {

            }
        }

        public static Result Run(FileInfo script, IEnumerable<string> arguments = null, IDictionary<string, string> environment = null)
        {
            return new Result(script, arguments, environment);
        }

    }
}

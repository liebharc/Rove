namespace Rove.Model
{
    public static class Logger
    {
        public static void WriteInfo(string message)
        {
            System.Diagnostics.Trace.WriteLine(message);
        }
    }
}

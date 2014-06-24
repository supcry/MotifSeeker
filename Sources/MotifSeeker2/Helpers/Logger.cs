using NLog;

namespace MotifSeeker2.Helpers
{
    public static class Logs
    {
        public static readonly Logger Instance = LogManager.GetCurrentClassLogger();
    }
}

using System;

namespace kOS.Safe
{
    public interface ILogger
    {
        void Log(string text);
        void Log(Exception e);
        void SuperVerbose(string s);
        void LogWarning(string s);
        void LogWarningAndScreen(string s);
        void LogException(Exception exception);
        void LogError(string s);
    }
    public static class LoggerExtensions
    {
        public static void Log(this ILogger logger, string text, params object[] args)
        {
            if (args?.Length > 0) logger.Log(string.Format(text, args));
            else logger.Log(text);
        }
        public static void SuperVerbose(this ILogger logger, string text, params object[] args)
        {
            if (args?.Length > 0) logger.SuperVerbose(string.Format(text, args));
            else logger.SuperVerbose(text);
        }
        public static void LogWarning(this ILogger logger, string text, params object[] args)
        {
            if (args?.Length > 0) logger.LogWarning(string.Format(text, args));
            else logger.LogWarning(text);
        }
        public static void LogWarningAndScreen(this ILogger logger, string text, params object[] args)
        {
            if (args?.Length > 0) logger.LogWarningAndScreen(string.Format(text, args));
            else logger.LogWarningAndScreen(text);
        }
        public static void LogError(this ILogger logger, string text, params object[] args)
        {
            if (args?.Length > 0) logger.LogError(string.Format(text, args));
            else logger.LogError(text);
        }
    }
}

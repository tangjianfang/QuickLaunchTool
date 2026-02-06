using System;
using System.IO;

namespace QuickLaunchTool.Utils
{
    /// <summary>
    /// Logger
    /// </summary>
    public static class Logger
    {
        private static readonly string _logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "QuickLaunchTool",
            "logs.txt");

        static Logger()
        {
            var directory = Path.GetDirectoryName(_logPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory!);
            }
        }

        /// <summary>
        /// Log information
        /// </summary>
        public static void Info(string message)
        {
            Log("INFO", message);
        }

        /// <summary>
        /// Log warning
        /// </summary>
        public static void Warning(string message)
        {
            Log("WARNING", message);
        }

        /// <summary>
        /// Log error
        /// </summary>
        public static void Error(string message)
        {
            Log("ERROR", message);
        }

        /// <summary>
        /// Log exception
        /// </summary>
        public static void Exception(Exception ex)
        {
            Log("EXCEPTION", $"{ex.Message}\n{ex.StackTrace}");
        }

        private static void Log(string level, string message)
        {
            try
            {
                var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
                File.AppendAllText(_logPath, logMessage + Environment.NewLine);
                System.Diagnostics.Debug.WriteLine(logMessage);
            }
            catch
            {
                // Log write failed, ignore
            }
        }
    }
}

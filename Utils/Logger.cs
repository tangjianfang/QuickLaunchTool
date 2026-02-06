using System;
using System.IO;

namespace QuickLaunchTool.Utils
{
    /// <summary>
    /// 日志记录器
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
        /// 记录信息
        /// </summary>
        public static void Info(string message)
        {
            Log("INFO", message);
        }

        /// <summary>
        /// 记录警告
        /// </summary>
        public static void Warning(string message)
        {
            Log("WARNING", message);
        }

        /// <summary>
        /// 记录错误
        /// </summary>
        public static void Error(string message)
        {
            Log("ERROR", message);
        }

        /// <summary>
        /// 记录异常
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
                // 日志写入失败，忽略
            }
        }
    }
}

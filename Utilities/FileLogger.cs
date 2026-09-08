using System;
using System.IO;

namespace BracketLibraryInventorPlugin.Utilities
{
    /// <summary>
    /// Logger ghi ra file tại %APPDATA%\MCG_BracketLibrary\plugin.log.
    /// Thread-safe, không cần quyền Administrator.
    /// </summary>
    public static class FileLogger
    {
        private static readonly object _lock = new object();
        private static readonly string _logPath;
        private const long MAX_LOG_SIZE = 5 * 1024 * 1024; // 5MB

        public static string LogPath => _logPath;

        static FileLogger()
        {
            try
            {
                string logDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MCG_BracketLibrary");
                if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
                _logPath = Path.Combine(logDir, "plugin.log");
            }
            catch
            {
                _logPath = Path.Combine(Path.GetTempPath(), "MCG_BracketLibrary_plugin.log");
            }
        }

        public static void Log(string prefix, string message)
        {
            try
            {
                lock (_lock)
                {
                    RotateLogIfNeeded();
                    File.AppendAllText(_logPath,
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {prefix} {message}{Environment.NewLine}");
                }
            }
            catch { }
        }

        public static void LogException(string prefix, string context, Exception ex)
        {
            if (ex == null) return;
            Log(prefix, $"LOI {context}: {ex.GetType().Name}: {ex.Message}");
            if (!string.IsNullOrEmpty(ex.StackTrace))
                Log(prefix, $"Stack:\n{ex.StackTrace}");
            if (ex.InnerException != null)
                LogException(prefix, $"{context} (inner)", ex.InnerException);
        }

        public static void LogSessionStart(string sessionName)
        {
            try
            {
                lock (_lock)
                {
                    RotateLogIfNeeded();
                    string header = $"{Environment.NewLine}{"=".PadRight(60, '=')}{Environment.NewLine}" +
                                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] SESSION START: {sessionName}{Environment.NewLine}" +
                                    $"{"=".PadRight(60, '=')}{Environment.NewLine}";
                    File.AppendAllText(_logPath, header);
                }
            }
            catch { }
        }

        private static void RotateLogIfNeeded()
        {
            try
            {
                if (!File.Exists(_logPath)) return;
                if (new FileInfo(_logPath).Length < MAX_LOG_SIZE) return;
                string old = _logPath + ".old";
                if (File.Exists(old)) File.Delete(old);
                File.Move(_logPath, old);
            }
            catch { }
        }
    }
}

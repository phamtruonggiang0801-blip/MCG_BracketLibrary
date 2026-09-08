using System;
using System.IO;

namespace BracketLibraryInventorPlugin.Helpers
{
    /// <summary>
    /// Logger ghi lỗi Activate/Deactivate add-in ra C:\CustomTools\Inventor\logs\.
    /// Chỉ ghi khi có Exception — tránh verbose trace.
    /// </summary>
    internal static class AddinLogger
    {
        private const string LOG_DIR  = @"C:\CustomTools\Inventor\logs";
        private const string LOG_FILE = @"C:\CustomTools\Inventor\logs\MCG_BracketLibrary.log";

        public static void Log(string phase, Exception ex)
        {
            try
            {
                if (!Directory.Exists(LOG_DIR)) Directory.CreateDirectory(LOG_DIR);
                File.AppendAllText(LOG_FILE,
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {phase} ERROR: {ex.Message}\n{ex.StackTrace}\n\n");
            }
            catch { }
        }

        public static void DeleteLogFile()
        {
            try { if (File.Exists(LOG_FILE)) File.Delete(LOG_FILE); }
            catch { }
        }
    }
}

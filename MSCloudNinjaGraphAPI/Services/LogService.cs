using System;
using System.IO;
using System.Threading.Tasks;

namespace MSCloudNinjaGraphAPI.Services
{
    public class LogService
    {
        private static readonly string LogDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MSCloudNinja", "Logs");
        private static readonly string LogFile = Path.Combine(LogDirectory, $"app_{DateTime.Now:yyyyMMdd}.log");
        private static readonly object LockObject = new object();

        public LogService()
        {
            EnsureLogDirectoryExists();
        }

        private void EnsureLogDirectoryExists()
        {
            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }
        }

        public async Task LogAsync(string message, bool isError = false)
        {
            var logMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} - {(isError ? "ERROR: " : "")}{message}";
            
            try
            {
                // Use lock to prevent multiple threads from writing simultaneously
                lock (LockObject)
                {
                    File.AppendAllText(LogFile, logMessage + Environment.NewLine);
                }

                // Also write to debug output for development
                System.Diagnostics.Debug.WriteLine(logMessage);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error writing to log file: {ex.Message}");
            }
        }

        public string GetCurrentLogFilePath()
        {
            return LogFile;
        }

        public async Task ClearLogsAsync()
        {
            try
            {
                if (File.Exists(LogFile))
                {
                    File.WriteAllText(LogFile, string.Empty);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing log file: {ex.Message}");
            }
        }
    }
}

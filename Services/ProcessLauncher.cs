using System;
using System.Diagnostics;
using System.IO;

namespace QuickLaunchTool.Services
{
    /// <summary>
    /// Process launch service
    /// </summary>
    public static class ProcessLauncher
    {
        /// <summary>
        /// Launch application
        /// </summary>
        public static bool Launch(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                var psi = new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                };

                Process.Start(psi);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to launch: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Run application as administrator
        /// </summary>
        public static bool LaunchAsAdmin(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                var psi = new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true,
                    Verb = "runas"  // Request administrator privilege
                };

                Process.Start(psi);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to launch as administrator: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Check if application is running
        /// </summary>
        public static bool IsRunning(string filePath)
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(filePath);
                var processes = Process.GetProcessesByName(fileName);
                return processes.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Open file location
        /// </summary>
        public static bool OpenFileLocation(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                var directory = Path.GetDirectoryName(filePath);
                if (directory == null)
                    return false;

                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{filePath}\"",
                    UseShellExecute = true
                });

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to open file location: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get application property information
        /// </summary>
        public static bool ShowProperties(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                // Use explorer with verb to show properties dialog
                var psi = new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"/select,\"{filePath}\"",
                    UseShellExecute = true
                };

                Process.Start(psi);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to show properties: {ex.Message}");
                return false;
            }
        }
    }
}

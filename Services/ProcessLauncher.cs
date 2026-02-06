using System;
using System.Diagnostics;
using System.IO;

namespace QuickLaunchTool.Services
{
    /// <summary>
    /// 进程启动服务
    /// </summary>
    public static class ProcessLauncher
    {
        /// <summary>
        /// 启动应用程序
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
                System.Diagnostics.Debug.WriteLine($"启动失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 以管理员身份运行应用程序
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
                    Verb = "runas"  // 请求管理员权限
                };

                Process.Start(psi);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"以管理员身份启动失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检查应用程序是否正在运行
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
        /// 打开文件所在位置
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
                System.Diagnostics.Debug.WriteLine($"打开文件位置失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取应用程序属性信息
        /// </summary>
        public static bool ShowProperties(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return false;

                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c properties \"{filePath}\"",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                Process.Start(psi);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"显示属性失败: {ex.Message}");
                return false;
            }
        }
    }
}

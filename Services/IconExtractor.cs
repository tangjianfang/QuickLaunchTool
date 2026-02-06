using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace QuickLaunchTool.Services
{
    /// <summary>
    /// Icon extraction service
    /// </summary>
    public static class IconExtractor
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern uint ExtractIconEx(string lpszFile, int nIconIndex,
            IntPtr[] phiconLarge, IntPtr[] phiconSmall, uint nIcons);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        /// <summary>
        /// 从exe文件提取图标
        /// </summary>
        public static Icon? ExtractIcon(string filePath, int iconIndex = 0)
        {
            try
            {
                if (!File.Exists(filePath))
                    return GetDefaultIcon();

                // Method 1: Use built-in Icon.ExtractAssociatedIcon
                var icon = Icon.ExtractAssociatedIcon(filePath);
                if (icon != null)
                    return icon;
            }
            catch
            {
                // Built-in method failed, try P/Invoke method
            }

            // Method 2: Use P/Invoke
            try
            {
                IntPtr[] largeIcons = new IntPtr[1];
                IntPtr[] smallIcons = new IntPtr[1];

                uint iconCount = ExtractIconEx(filePath, iconIndex, largeIcons, smallIcons, 1);

                if (iconCount > 0 && largeIcons[0] != IntPtr.Zero)
                {
                    var icon = Icon.FromHandle(largeIcons[0]);
                    // Note: Do not destroy icon pointer here, as Icon object needs it
                    return icon;
                }
            }
            catch
            {
                // P/Invoke method also failed
            }

            return GetDefaultIcon();
        }

        /// <summary>
        /// Get default icon
        /// </summary>
        public static Icon GetDefaultIcon()
        {
            try
            {
                return SystemIcons.Application;
            }
            catch
            {
                // If unable to get system icon, return a simple icon
                return new Icon(SystemIcons.Exclamation, new Size(32, 32));
            }
        }

        /// <summary>
        /// Extract icon asynchronously
        /// </summary>
        public static async Task<Icon?> ExtractIconAsync(string filePath, int iconIndex = 0)
        {
            return await Task.Run(() => ExtractIcon(filePath, iconIndex));
        }
    }
}

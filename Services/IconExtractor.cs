using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace QuickLaunchTool.Services
{
    /// <summary>
    /// 图标提取服务
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

                // 方法1：使用内置Icon.ExtractAssociatedIcon
                var icon = Icon.ExtractAssociatedIcon(filePath);
                if (icon != null)
                    return icon;
            }
            catch
            {
                // 内置方法失败，尝试P/Invoke方法
            }

            // 方法2：使用P/Invoke
            try
            {
                IntPtr[] largeIcons = new IntPtr[1];
                IntPtr[] smallIcons = new IntPtr[1];

                uint iconCount = ExtractIconEx(filePath, iconIndex, largeIcons, smallIcons, 1);

                if (iconCount > 0 && largeIcons[0] != IntPtr.Zero)
                {
                    var icon = Icon.FromHandle(largeIcons[0]);
                    // 注意：这里不销毁图标指针，因为Icon对象需要它
                    return icon;
                }
            }
            catch
            {
                // P/Invoke方法也失败
            }

            return GetDefaultIcon();
        }

        /// <summary>
        /// 获取默认图标
        /// </summary>
        public static Icon GetDefaultIcon()
        {
            try
            {
                return SystemIcons.Application;
            }
            catch
            {
                // 如果无法获取系统图标，返回一个简单的图标
                return new Icon(SystemIcons.Exclamation, new Size(32, 32));
            }
        }

        /// <summary>
        /// 异步提取图标
        /// </summary>
        public static async Task<Icon?> ExtractIconAsync(string filePath, int iconIndex = 0)
        {
            return await Task.Run(() => ExtractIcon(filePath, iconIndex));
        }
    }
}

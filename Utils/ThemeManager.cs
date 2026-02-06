using System;
using System.Drawing;
using System.Windows.Forms;

namespace QuickLaunchTool.Utils
{
    /// <summary>
    /// 主题管理器
    /// </summary>
    public static class ThemeManager
    {
        // 浅色主题颜色
        private static readonly Color LightBackground = Color.White;
        private static readonly Color LightForeground = Color.Black;
        private static readonly Color LightHover = Color.LightGray;
        private static readonly Color LightBorder = Color.DarkGray;

        // 深色主题颜色
        private static readonly Color DarkBackground = Color.FromArgb(45, 45, 48);
        private static readonly Color DarkForeground = Color.White;
        private static readonly Color DarkHover = Color.FromArgb(60, 60, 65);
        private static readonly Color DarkBorder = Color.FromArgb(100, 100, 100);

        /// <summary>
        /// 获取背景色
        /// </summary>
        public static Color GetBackgroundColor(bool darkTheme)
        {
            return darkTheme ? DarkBackground : LightBackground;
        }

        /// <summary>
        /// 获取前景色
        /// </summary>
        public static Color GetForegroundColor(bool darkTheme)
        {
            return darkTheme ? DarkForeground : LightForeground;
        }

        /// <summary>
        /// 获取悬停色
        /// </summary>
        public static Color GetHoverColor(bool darkTheme)
        {
            return darkTheme ? DarkHover : LightHover;
        }

        /// <summary>
        /// 获取边框色
        /// </summary>
        public static Color GetBorderColor(bool darkTheme)
        {
            return darkTheme ? DarkBorder : LightBorder;
        }

        /// <summary>
        /// 应用主题到控件
        /// </summary>
        public static void ApplyTheme(Control control, bool darkTheme)
        {
            control.BackColor = GetBackgroundColor(darkTheme);
            control.ForeColor = GetForegroundColor(darkTheme);

            foreach (Control child in control.Controls)
            {
                ApplyTheme(child, darkTheme);
            }
        }
    }
}
